---
name: ddd
description: >
  Scaffold Domain-Driven Design building blocks — aggregates, value objects, domain events,
  domain errors, and strongly-typed IDs for a given domain concept.
  Load this skill when: "ddd", "aggregate", "domain event", "value object",
  "aggregate root", "/ddd", "domain model", "bounded context".
user-invocable: true
argument-hint: "<AggregateRootName> [<namespace>]"
allowed-tools: Read, Write, Edit, Glob, Grep, Bash
---

# DDD — Domain-Driven Design Building Blocks

## Core Principles

1. **Aggregates own their consistency boundary** — All state changes go through the aggregate's public methods. External code never sets properties directly. The aggregate enforces its own invariants.
2. **Business logic lives in the domain, not handlers** — Handlers orchestrate; aggregates decide. A handler that says `order.Status = OrderStatus.Cancelled` instead of `order.Cancel()` is a violation.
3. **Domain events record what happened** — When significant state changes occur, the aggregate raises a domain event (past tense, e.g., `OrderCancelledDomainEvent`). These are dispatched after `SaveChanges`.
4. **Value objects express domain language** — `Money`, `Address`, `Email` are not just strings or decimals. They validate, enforce format, and carry business meaning. Make them records.
5. **Errors are domain concerns** — `OrderErrors.NotFound` and `OrderErrors.AlreadyCancelled` belong in the Domain layer, not in handlers. They express business rules, not infrastructure failures.

## Patterns

### Aggregate Root

```csharp
// Domain/Orders/Order.cs
// GOOD — private setters, factory method, business methods return Result
public sealed class Order : Entity
{
    private Order() { } // Required by EF Core

    public OrderId Id { get; private set; } = null!;
    public CustomerId CustomerId { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = null!;

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

    /// <summary>Cancels the order. Returns Conflict if already cancelled.</summary>
    public Result Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            return OrderErrors.AlreadyCancelled;

        Status = OrderStatus.Cancelled;
        RaiseDomainEvent(new OrderCancelledDomainEvent(Id));
        return Result.Success();
    }
}

// BAD — public setters, logic in handlers instead of aggregate
public class Order
{
    public OrderStatus Status { get; set; }  // handler can set this directly
}
// handler: order.Status = OrderStatus.Cancelled; db.SaveChanges(); — no validation!
```

### Value Object

```csharp
// Domain/Orders/ValueObjects/Money.cs
// GOOD — immutable record, factory with validation, equality by value
public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return MoneyErrors.NegativeAmount;
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return MoneyErrors.InvalidCurrency;

        return new Money(amount, currency.ToUpperInvariant());
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies.");
        return new Money(Amount + other.Amount, Currency);
    }
}
```

### Domain Events

```csharp
// Domain/Orders/Events/OrderCreatedDomainEvent.cs
// GOOD — past tense, immutable record, minimal data
public record OrderCreatedDomainEvent(OrderId OrderId) : IDomainEvent;

public record OrderCancelledDomainEvent(OrderId OrderId) : IDomainEvent;

public record OrderShippedDomainEvent(OrderId OrderId, TrackingNumber TrackingNumber) : IDomainEvent;

// BAD — present tense, mutable, carries full entity
public class OrderCancelEvent  // wrong tense, wrong suffix
{
    public Order Order { get; set; }  // don't pass the full entity
}
```

### Domain Errors

```csharp
// Domain/Orders/OrderErrors.cs
// GOOD — static class, namespaced error codes, ErrorType classification
public static class OrderErrors
{
    public static readonly Error NotFound =
        new("Order.NotFound", "The specified order was not found.", ErrorType.NotFound);

    public static readonly Error AlreadyCancelled =
        new("Order.AlreadyCancelled", "The order is already cancelled.", ErrorType.Conflict);

    public static readonly Error CannotShipUnpaid =
        new("Order.CannotShipUnpaid", "Cannot ship an order that has not been paid.", ErrorType.Validation);

    // Factory method for parameterized errors
    public static Error InsufficientStock(Guid productId) =>
        new("Order.InsufficientStock", $"Product {productId} has insufficient stock.", ErrorType.Validation);
}
```

### Strongly-Typed ID

```csharp
// Domain/Orders/OrderId.cs
public record OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
```

## Anti-patterns

### Anemic Domain Model

```csharp
// BAD — aggregate has no behavior, all logic is in the handler
public class Order
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "Pending";
}

// Handler doing what the aggregate should do
public async Task<Result> Handle(CancelOrderCommand request, CancellationToken ct)
{
    var order = await db.Orders.FindAsync(request.OrderId, ct);
    if (order.Status == "Cancelled") return Result.Failure(...);  // business rule in handler!
    order.Status = "Cancelled";  // direct mutation, no domain event
    await db.SaveChangesAsync(ct);
    return Result.Success();
}

// GOOD — aggregate owns the logic
var result = order.Cancel();  // all business rules inside Order.Cancel()
if (result.IsFailure) return result.Error!;
await db.SaveChangesAsync(ct);
```

### Domain Events in Wrong Layer

```csharp
// BAD — raising domain events from a handler
public async Task<Result> Handle(CancelOrderCommand request, CancellationToken ct)
{
    order.Status = OrderStatus.Cancelled;
    await publisher.Publish(new OrderCancelledDomainEvent(order.Id), ct);  // wrong layer!
    await db.SaveChangesAsync(ct);
    return Result.Success();
}

// GOOD — aggregate raises events internally
// In Order.Cancel():
RaiseDomainEvent(new OrderCancelledDomainEvent(Id));  // inside the aggregate
// DbContext.SaveChangesAsync() dispatches them after save
```

### Primitive Obsession

```csharp
// BAD — using primitives where value objects belong
public record CreateOrderCommand(Guid CustomerId, decimal Amount, string Currency);

internal sealed class CreateOrderHandler(AppDbContext db)
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // Handler validates what a value object should validate
        if (request.Amount < 0) return Result.Failure(...);
        if (request.Currency.Length != 3) return Result.Failure(...);
        var order = new Order { Amount = request.Amount, Currency = request.Currency };
    }
}

// GOOD — value objects carry validation and meaning
var money = Money.Create(request.Amount, request.Currency);
if (money.IsFailure) return money.Error!;
var order = Order.Create(new CustomerId(request.CustomerId), money.Value!);
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| New domain concept needs a persistent entity | Generate aggregate root with `/ddd` |
| Field is a domain concept (email, money, address) | Create a value object record |
| State change should notify other parts of the system | Add a domain event to the aggregate method |
| Business rule violation should return an error | Add to `{Entity}Errors` as static readonly |
| ID needs to be type-safe across the codebase | Create a strongly-typed ID record |
| Handler contains an `if` checking business rules | Move that logic into an aggregate method |
| Two aggregates need to communicate | Use domain events, not direct references |
| Value object needs validation | Factory method returning `Result<ValueObject>` |

## Execution

Read `~/.claude/kit.config.md` for `DEFAULT_NAMESPACE` and `NEW_PROJECT_BASE_PATH`. If missing, ask the user to run `/kit-setup`.

### `/ddd <Name>` — generates:

**1. Aggregate Root** (`Domain/<Name>s/<Name>.cs`)
- Inherit from `Entity` base class
- Private parameterless constructor (EF Core)
- Static `Create(...)` factory method
- Business methods with `Result` return types
- Raise domain events on state changes
- Private setters on all properties

**2. Value Objects** (as needed for the domain, `Domain/<Name>s/ValueObjects/`)
- Records implementing value equality
- Static `Create(...)` factory with validation returning `Result<ValueObject>`

**3. Domain Events** (`Domain/<Name>s/Events/`)
- Records implementing `IDomainEvent`
- Named in past tense: `OrderCreatedDomainEvent`, `OrderCancelledDomainEvent`

**4. Domain Errors** (`Domain/<Name>s/<Name>Errors.cs`)
- Static class with static readonly `Error` fields
- Namespaced error codes: `"Order.NotFound"`, `"Order.AlreadyCancelled"`
- Factory methods for parameterized errors

**5. Strongly-Typed ID** (`Domain/<Name>s/<Name>Id.cs`)
- Record: `public record OrderId(Guid Value) { public static OrderId New() => new(Guid.NewGuid()); }`

### Code Style
- File-scoped namespaces
- Primary constructors where applicable
- `IReadOnlyList<T>` not `List<T>` for return types
- XML doc comments on public factory methods and domain methods

### After Generation
1. Remind the user to add EF Core configuration in Infrastructure (`IEntityTypeConfiguration<TEntity>`)
2. Remind to configure the strongly-typed ID value converter
3. Remind to register domain events if using the Outbox pattern
