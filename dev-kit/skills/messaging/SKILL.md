---
name: messaging
description: >
  Asynchronous messaging patterns for .NET applications. Covers Wolverine and
  MassTransit, outbox pattern, saga and choreography, and broker configuration
  for RabbitMQ and Azure Service Bus.
  Load this skill when implementing event-driven communication, background
  processing, module-to-module messaging, or when the user mentions "Wolverine",
  "MassTransit", "message bus", "RabbitMQ", "Azure Service Bus", "event",
  "publish", "consumer", "outbox", "saga", "integration event", "queue",
  or "pub/sub".
user-invocable: true
argument-hint: "[message/event to implement]"
allowed-tools: Read, Write, Edit, Bash, Grep, Glob
---

# Messaging

## Core Principles

1. **Wolverine is the recommended default** — MIT licensed, combines mediator + messaging in one library with built-in outbox, saga support, and convention-based handlers. MassTransit requires a commercial license from v9.
2. **Outbox pattern for reliability** — Always use the transactional outbox to ensure messages are published only when the database transaction succeeds.
3. **Choreography for simple flows, saga for complex** — If a workflow has 2-3 steps, use event choreography. If it has compensating actions or complex state, use a saga.
4. **Messages are contracts** — Put message types in a shared contracts project. Keep them as simple records with primitive types.

## Patterns

### Wolverine Setup

```csharp
// Program.cs
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);

    opts.UseRabbitMq(rabbit =>
    {
        rabbit.HostName = "localhost";
    })
    .AutoProvision()
    .AutoPurgeOnStartup(); // Dev only

    opts.Services.AddDbContextWithWolverineIntegration<AppDbContext>(x =>
        x.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

    opts.Policies.AutoApplyTransactions();
});
```

### Publishing Events

```csharp
// Message contract (in shared Contracts project)
public record OrderCreated(Guid OrderId, string CustomerId, decimal Total, DateTimeOffset CreatedAt);

// Style 1: Cascading messages — return the event from the handler
public static class CreateOrder
{
    public record Command(string CustomerId, List<OrderItem> Items);
    public record Response(Guid OrderId, decimal Total);

    public static async Task<(Response, OrderCreated)> HandleAsync(
        Command command, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var order = Order.Create(command.CustomerId, command.Items, clock.GetUtcNow());
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return (new Response(order.Id, order.Total),
                new OrderCreated(order.Id, order.CustomerId, order.Total, order.CreatedAt));
    }
}
```

### Consuming Events

```csharp
// Convention-based handler — no interface, no base class
public static class OrderCreatedHandler
{
    public static async Task HandleAsync(
        OrderCreated message, NotificationsDbContext db, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("Processing OrderCreated: {OrderId}", message.OrderId);

        var notification = new OrderNotification(message.OrderId, message.CustomerId);
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);
    }
}
```

### Transactional Outbox

```csharp
// DbContext — add Wolverine outbox tables
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddIncomingWolverineMessageTable();
        modelBuilder.AddOutgoingWolverineMessageTable();
    }
}
```

### Saga (Stateful Orchestration)

```csharp
public class OrderSaga : Saga<OrderSagaState>
{
    public Guid Id { get; set; }

    public static (OrderSagaState, ProcessPayment) Start(OrderCreated message)
    {
        var state = new OrderSagaState(message.OrderId) { CustomerId = message.CustomerId };
        return (state, new ProcessPayment(message.OrderId, message.Total));
    }

    public CompleteOrder Handle(PaymentCompleted message)
    {
        MarkCompleted();
        return new CompleteOrder(Id);
    }

    public CancelOrder Handle(PaymentFailed message)
    {
        MarkCompleted();
        return new CancelOrder(Id);
    }
}
```

### Alternative: MassTransit

```csharp
// Setup
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumers(typeof(Program).Assembly);
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        cfg.ConfigureEndpoints(context);
    });
});

// Consumer — requires IConsumer<T> interface
public class OrderCreatedConsumer(AppDbContext db) : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        // Handle event...
    }
}
```

> **License note**: MassTransit v9+ requires a commercial license for production use. Wolverine (MIT) is recommended for new projects.

## Anti-patterns

### Don't Publish Events Without Outbox

```csharp
// BAD — if SaveChanges succeeds but Publish fails, data is inconsistent
await db.SaveChangesAsync(ct);
await bus.PublishAsync(new OrderCreated(...));

// GOOD — use transactional outbox
// Configure AddDbContextWithWolverineIntegration() + AutoApplyTransactions()
```

### Don't Use Fire-and-Forget for Important Events

```csharp
// BAD — no guarantee of delivery
_ = Task.Run(() => bus.PublishAsync(new OrderCreated(...)));

// GOOD — await the publish (with outbox, this is transactional)
await bus.PublishAsync(new OrderCreated(...));
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Module-to-module communication (new project) | Wolverine with events (MIT, free) |
| Module-to-module communication (existing MassTransit) | MassTransit |
| Reliable event publishing | Transactional outbox |
| Simple 2-3 step workflow | Event choreography |
| Complex workflow with compensation | Wolverine saga or MassTransit saga |
| Local development broker | RabbitMQ (via Docker or Aspire) |
| Production cloud broker | Azure Service Bus or RabbitMQ |
| Want single lib for mediator + messaging | Wolverine (replaces both MediatR and MassTransit) |
