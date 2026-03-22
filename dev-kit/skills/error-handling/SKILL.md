---
name: error-handling
description: >
  Error handling strategy for .NET applications. Covers the Result pattern,
  ProblemDetails (RFC 9457), global exception handling, FluentValidation, and
  structured error responses.
  Load this skill when implementing error handling, validation, or designing
  API error contracts, or when the user mentions "error handling", "Result pattern",
  "ProblemDetails", "exception", "validation", "FluentValidation", "error response",
  "global exception handler", or "RFC 9457".
user-invocable: true
argument-hint: "[error scenario or domain to handle]"
allowed-tools: Read, Write, Edit, Bash, Grep, Glob
---

# Error Handling

## Core Principles

1. **Use the Result pattern for expected failures** — Don't throw exceptions for things like "order not found" or "validation failed". These are expected outcomes, not exceptional conditions.
2. **Reserve exceptions for unexpected failures** — Database connection lost, null reference bugs, network timeouts — these are truly exceptional and should propagate to the global handler.
3. **Every API error returns ProblemDetails** — RFC 9457 is the standard. Every error response has `type`, `title`, `status`, `detail`, and optionally `errors`.
4. **Validate at the boundary** — Validate incoming requests at the API layer, not deep inside business logic.

## Patterns

### Result Pattern

See `~/.claude/knowledge/dotnet/result-pattern.md` for the full Result<T> implementation.

Key usage pattern:
```csharp
// In handler — return errors, don't throw
if (order is null)
    return OrderErrors.NotFound;

// At endpoint — map to ProblemDetails
var result = await sender.Send(command, ct);
return result.IsFailure ? result.Error!.ToProblemDetails() : TypedResults.Ok(result.Value);
```

### Global Exception Handler

Catches unexpected exceptions and converts them to ProblemDetails.

```csharp
// Program.cs
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            context.Request.Method, context.Request.Path);

        var problem = new ProblemDetails
        {
            Title = "An unexpected error occurred",
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        };

        if (context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            problem.Detail = exception?.Message;

        context.Response.StatusCode = problem.Status.Value;
        await context.Response.WriteAsJsonAsync(problem);
    });
});
```

### FluentValidation with Endpoint Filters

```csharp
// Validator
public class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Customer ID is required");
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

// Generic validation filter
public class ValidationFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
        if (validator is null) return await next(context);

        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null) return await next(context);

        var result = await validator.ValidateAsync(request);
        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary());

        return await next(context);
    }
}

// Registration
group.MapPost("/", CreateOrder)
    .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();
```

## Anti-patterns

### Don't Throw Exceptions for Flow Control

```csharp
// BAD — exceptions for expected outcomes
public Order GetOrder(Guid id)
{
    var order = db.Orders.Find(id)
        ?? throw new NotFoundException($"Order {id} not found");
    return order;
}

// GOOD — Result pattern
public Result<Order> GetOrder(Guid id)
{
    var order = db.Orders.Find(id);
    return order is not null ? Result.Success(order) : OrderErrors.NotFound;
}
```

### Don't Return Raw Error Strings from APIs

```csharp
// BAD — inconsistent error format
return Results.BadRequest("Something went wrong");

// GOOD — always ProblemDetails
return TypedResults.Problem(title: "Invalid input", statusCode: 400);
return TypedResults.ValidationProblem(validationResult.ToDictionary());
```

### Don't Catch and Swallow Exceptions

```csharp
// BAD — silently swallowing
try { await ProcessOrder(order); }
catch (Exception) { /* ignore */ }

// GOOD — log and handle appropriately
try { await ProcessOrder(order); }
catch (PaymentException ex)
{
    logger.LogWarning(ex, "Payment failed for order {OrderId}", order.Id);
    return Result.Failure("Payment processing failed");
}
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Expected business failure | Result pattern |
| Input validation | FluentValidation with endpoint filter |
| Unexpected crash | Global exception handler → ProblemDetails |
| API error format | RFC 9457 ProblemDetails — always |
| Validation in handler | Return Result.Failure, don't throw |
| External service failure | Catch specific exception, return Result.Failure |
