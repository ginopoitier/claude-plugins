# Clean Architecture — .NET Reference

## Layer Overview

```
src/
  YourApp.Domain/           # Enterprise business rules — zero external dependencies
  YourApp.Application/      # Use cases — depends on Domain only
  YourApp.Infrastructure/   # EF Core, external services, repos — depends on Application
  YourApp.Api/              # Minimal API, DI wiring — depends on Application + Infrastructure
tests/
  YourApp.Domain.Tests/
  YourApp.Application.Tests/
  YourApp.Infrastructure.Tests/
```

## Dependency Rule
- Domain → nothing
- Application → Domain
- Infrastructure → Application + Domain
- Api (Presentation) → Application + Infrastructure (for DI only)

**Never reference Infrastructure from Application.** Define interfaces in Application, implement in Infrastructure.

## Domain Layer

Contains:
- **Entities** — with private setters, business methods, domain events
- **Value Objects** — immutable, equality by value (use records)
- **Aggregates** — entity that owns its consistency boundary
- **Domain Events** — records implementing `IDomainEvent`
- **Errors** — static error definitions per entity (see result-pattern.md)
- **Interfaces** — only interfaces needed by domain logic itself

```csharp
// Domain/Orders/Order.cs
public sealed class Order : Entity
{
    private Order() { } // EF Core

    public OrderId Id { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }

    public static Order Create(CustomerId customerId, Money totalAmount)
    {
        var order = new Order
        {
            Id = OrderId.New(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            TotalAmount = totalAmount
        };
        order.RaiseDomainEvent(new OrderCreatedDomainEvent(order.Id));
        return order;
    }

    public Result Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            return OrderErrors.AlreadyCancelled;

        Status = OrderStatus.Cancelled;
        RaiseDomainEvent(new OrderCancelledDomainEvent(Id));
        return Result.Success();
    }
}
```

## Application Layer

Contains:
- **Commands** — records implementing `IRequest<Result<T>>`
- **Queries** — records implementing `IRequest<Result<T>>`
- **Handlers** — `IRequestHandler<TRequest, TResponse>`, one per file, `internal sealed`
- **Validators** — FluentValidation `AbstractValidator<TCommand>`
- **DTOs** — response/request data shapes (records)
- **Interfaces** — `IEmailService`, `IFileStorage`, etc. (implemented in Infrastructure)
- **Pipeline Behaviors** — validation, logging, transactions

```csharp
// Application/Orders/Commands/CreateOrder/CreateOrderCommand.cs
public record CreateOrderCommand(Guid CustomerId, decimal Amount) : IRequest<Result<OrderResponse>>;

// Application/Orders/Commands/CreateOrder/CreateOrderValidator.cs
public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

// Application/Orders/Commands/CreateOrder/CreateOrderHandler.cs
internal sealed class CreateOrderHandler(AppDbContext db) : IRequestHandler<CreateOrderCommand, Result<OrderResponse>>
{
    public async Task<Result<OrderResponse>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = Order.Create(new CustomerId(request.CustomerId), new Money(request.Amount, "EUR"));
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return new OrderResponse(order.Id.Value, order.Status.ToString(), order.TotalAmount.Amount);
    }
}
```

## Infrastructure Layer

Contains:
- **DbContext** and EF Core configurations (`IEntityTypeConfiguration<T>`)
- **Migrations**
- **Interface implementations** (email, storage, external APIs)
- **Neo4j** session/driver wrappers
- **DI registration** extension methods (`AddInfrastructure(this IServiceCollection services)`)

## Presentation / API Layer

Contains:
- **Endpoint groups** (feature-organized, auto-discovered)
- **DI wiring** (calls `AddApplication()`, `AddInfrastructure()`)
- **Middleware** (exception handling, request logging)
- **Serilog** configuration

## Strongly Typed IDs

```csharp
// Domain/Shared/StronglyTypedId.cs
public record OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());
}
```

Configure EF Core value converters for strongly typed IDs in `OnModelCreating` or via `IEntityTypeConfiguration`.
