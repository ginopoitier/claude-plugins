# Result Pattern

## Core Types

```csharp
// Domain/Shared/Result.cs
public class Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;
    public bool IsFailure => !IsSuccess;

    private Result(T value) => Value = value;
    private Result(Error error) => Error = error;

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

public class Result
{
    public Error? Error { get; }
    public bool IsSuccess => Error is null;
    public bool IsFailure => !IsSuccess;

    private Result() { }
    private Result(Error error) => Error = error;

    public static Result Success() => new();
    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

public record Error(string Code, string Description, ErrorType Type = ErrorType.Failure);

public enum ErrorType
{
    Failure,
    NotFound,
    Validation,
    Conflict,
    Unauthorized
}
```

## Defining Errors (per domain)

```csharp
// Domain/Orders/OrderErrors.cs
public static class OrderErrors
{
    public static readonly Error NotFound = new("Order.NotFound", "Order was not found.", ErrorType.NotFound);
    public static readonly Error AlreadyCancelled = new("Order.AlreadyCancelled", "Order is already cancelled.", ErrorType.Conflict);
    public static Error InsufficientStock(int productId) =>
        new("Order.InsufficientStock", $"Product {productId} has insufficient stock.", ErrorType.Validation);
}
```

## Handler Usage

```csharp
// Application/Orders/Commands/CancelOrder/CancelOrderHandler.cs
internal sealed class CancelOrderHandler(AppDbContext db) : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FindAsync([request.OrderId], cancellationToken);

        if (order is null)
            return OrderErrors.NotFound;

        if (order.Status == OrderStatus.Cancelled)
            return OrderErrors.AlreadyCancelled;

        order.Cancel();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

## Mapping to ProblemDetails (Presentation layer)

```csharp
// Presentation/Extensions/ResultExtensions.cs
public static IResult ToProblemDetails(this Error error) =>
    error.Type switch
    {
        ErrorType.NotFound     => Results.NotFound(new ProblemDetails { Title = error.Code, Detail = error.Description }),
        ErrorType.Validation   => Results.UnprocessableEntity(new ProblemDetails { Title = error.Code, Detail = error.Description }),
        ErrorType.Conflict     => Results.Conflict(new ProblemDetails { Title = error.Code, Detail = error.Description }),
        ErrorType.Unauthorized => Results.Unauthorized(),
        _                      => Results.Problem(detail: error.Description, title: error.Code)
    };
```

## Endpoint Usage

```csharp
app.MapPost("/orders/{id}/cancel", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new CancelOrderCommand(id), ct);
    return result.IsFailure ? result.Error!.ToProblemDetails() : Results.NoContent();
});
```

## Validation Pipeline Behavior

```csharp
// Application/Behaviors/ValidationBehavior.cs
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0) return await next();

        // Return a Result<T> failure — requires TResponse to be Result or Result<T>
        // Use a source generator or reflection approach if needed for generic mapping
        throw new ValidationException(failures); // caught by global exception handler → 422
    }
}
```
