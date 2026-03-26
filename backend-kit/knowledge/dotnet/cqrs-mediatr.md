# CQRS with MediatR — .NET Reference

## Registration

```csharp
// Application/DependencyInjection.cs
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    var assembly = typeof(ApplicationAssemblyMarker).Assembly;

    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(assembly);
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
    });

    services.AddValidatorsFromAssembly(assembly);

    return services;
}
```

## Command Pattern

```csharp
// Command — mutates state
public record CreateProductCommand(string Name, decimal Price, int Stock) : IRequest<Result<Guid>>;

internal sealed class CreateProductHandler(AppDbContext db, ILogger<CreateProductHandler> logger)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = Product.Create(request.Name, new Money(request.Price, "EUR"), request.Stock);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Product {ProductId} created with name {Name}", product.Id.Value, request.Name);
        return product.Id.Value;
    }
}
```

## Query Pattern

```csharp
// Query — reads state, never modifies
public record GetProductQuery(Guid ProductId) : IRequest<Result<ProductResponse>>;

internal sealed class GetProductHandler(AppDbContext db)
    : IRequestHandler<GetProductQuery, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(GetProductQuery request, CancellationToken ct)
    {
        var product = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == new ProductId(request.ProductId))
            .Select(p => new ProductResponse(p.Id.Value, p.Name, p.Price.Amount, p.Stock))
            .FirstOrDefaultAsync(ct);

        return product is null ? ProductErrors.NotFound : product;
    }
}
```

## Pipeline Behaviors

### Logging Behavior
```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName} {@Request}", name, request);
        var response = await next();
        logger.LogInformation("Handled {RequestName}", name);
        return response;
    }
}
```

### Validation Behavior
```csharp
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var failures = validators
            .Select(v => v.Validate(new ValidationContext<TRequest>(request)))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Transaction Behavior (Commands only)
```csharp
public sealed class TransactionBehavior<TRequest, TResponse>(AppDbContext db)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse> // marker interface for commands only
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var response = await next();
            await transaction.CommitAsync(ct);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
```

## Marker Interfaces (optional but useful)

```csharp
// Separate commands from queries for behaviors that should only apply to one type
public interface ICommand<TResponse> : IRequest<TResponse> { }
public interface IQuery<TResponse> : IRequest<TResponse> { }
```

## Domain Events with MediatR

```csharp
// Publish domain events after SaveChanges using a dispatcher
public sealed class DomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        foreach (var domainEvent in events)
            await publisher.Publish(domainEvent, ct);
    }
}

// Hook into EF Core SaveChanges override or use Interceptors
```
