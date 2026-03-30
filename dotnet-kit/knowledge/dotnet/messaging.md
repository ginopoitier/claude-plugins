# Messaging — Wolverine & MassTransit Reference

## Overview

Messaging decouples services by exchanging commands and events through a broker (RabbitMQ, Azure Service Bus, Amazon SQS) rather than direct HTTP calls. Wolverine is the preferred choice for in-process and out-of-process messaging in .NET 9 — it integrates directly with EF Core for a first-class outbox pattern and its convention-based handler discovery eliminates boilerplate. MassTransit is the right choice when you need deep broker portability or an established saga DSL. This doc covers both.

## Setup: Wolverine with EF Core Outbox

The outbox pattern ensures a message is stored atomically with your database write, then delivered to the broker asynchronously. Without it, a crash between `SaveChanges` and broker publish leaves your system in an inconsistent state.

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration config,
    IHostEnvironment env)
{
    // 1. EF Core — Wolverine needs access to the DbContext for outbox storage
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

    return services;
}

// Program.cs — Wolverine is configured on the host builder, not in DI
builder.Host.UseWolverine(opts =>
{
    // Store outbox messages in the same SQL Server database as the app
    // This gives you a single transaction for both business data and outbox
    opts.PersistMessagesWithSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        schema: "wolverine");   // wolverine manages its own schema

    // Discover message handlers by convention (classes ending in "Handler")
    opts.Discovery.IncludeAssembly(typeof(InfrastructureAssemblyMarker).Assembly);

    // RabbitMQ transport — swap for Azure Service Bus or SQS with different extension
    opts.UseRabbitMq(rabbit =>
    {
        rabbit.ConnectionFactory.HostName = builder.Configuration["RabbitMq:Host"];
        rabbit.ConnectionFactory.UserName = builder.Configuration["RabbitMq:Username"];
        rabbit.ConnectionFactory.Password = builder.Configuration["RabbitMq:Password"];
    })
    .AutoProvision()           // create exchanges/queues on startup if missing
    .AutoPurgeOnStartup(builder.Environment.IsDevelopment()); // clean slate in dev

    // Dead-letter queue — failed messages go here after all retries are exhausted
    opts.Policies.UseDurableLocalQueue("errors");

    // Retry policy: 3 attempts with exponential back-off
    opts.Policies.OnException<Exception>()
        .RetryWithCooldown(100.Milliseconds(), 250.Milliseconds(), 500.Milliseconds());

    if (env.IsProduction())
    {
        // Run message processing on a fixed thread pool to avoid overwhelming the broker
        opts.Policies.UseDurableInboxOnAllListeners();
        opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
    }
});
```

## Pattern: Publishing with the Outbox (Wolverine)

The handler receives a `IMessageBus` (or uses the `IMessageContext` injected into Wolverine handlers). When called from inside a Wolverine handler, outbox semantics are automatic. When called from MediatR handlers, inject `IMessageBus` directly.

```csharp
// Application/Orders/Commands/CreateOrderHandler.cs
internal sealed class CreateOrderHandler(
    AppDbContext db,
    IMessageBus bus)   // Wolverine's bus — outbox-aware when EF Core outbox is configured
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = Order.Create(new CustomerId(cmd.CustomerId), new Money(cmd.Amount, "GBP"));
        db.Orders.Add(order);

        // Enqueue the event in the same transaction as the EF Core SaveChanges.
        // Wolverine stores the message in the outbox table; a background worker
        // delivers it to RabbitMQ after the transaction commits.
        await bus.PublishAsync(new OrderCreatedEvent(order.Id.Value, cmd.CustomerId));

        await db.SaveChangesAsync(ct);  // outbox record + business record committed atomically
        return order.Id.Value;
    }
}
```

## Pattern: Message Handler (Wolverine)

Wolverine discovers handlers by convention: any public method named `Handle` or `HandleAsync` on a class that ends in `Handler`. No interface required.

```csharp
// Infrastructure/Messaging/Handlers/OrderCreatedHandler.cs
public sealed class OrderCreatedHandler(
    IEmailService email,
    ILogger<OrderCreatedHandler> logger)
{
    // Wolverine injects dependencies via the method signature or constructor
    public async Task HandleAsync(
        OrderCreatedEvent evt,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Processing OrderCreated for order {OrderId}", evt.OrderId);

        await email.SendOrderConfirmationAsync(evt.CustomerId, evt.OrderId, ct);
    }
}

// Message contract — record in a shared Contracts project so both publisher and consumer agree
// Contracts/Events/OrderCreatedEvent.cs
public sealed record OrderCreatedEvent(Guid OrderId, Guid CustomerId);
```

## Pattern: Saga with Wolverine

Wolverine sagas track long-running workflows. The saga state is persisted automatically; correlation happens via the message's identity property.

```csharp
// Infrastructure/Messaging/Sagas/OrderFulfillmentSaga.cs
public sealed class OrderFulfillmentSaga : Saga
{
    public Guid OrderId { get; set; }           // Wolverine uses this as the correlation key
    public bool PaymentConfirmed { get; set; }
    public bool WarehousePicked   { get; set; }

    // Handles the first message that starts the saga
    public static OrderFulfillmentSaga Start(OrderCreatedEvent evt)
    {
        return new OrderFulfillmentSaga
        {
            OrderId = evt.OrderId   // correlate all future messages by this ID
        };
    }

    public IEnumerable<object> Handle(PaymentConfirmedEvent evt)
    {
        PaymentConfirmed = true;

        if (WarehousePicked)
        {
            // Both steps done — emit the next command and mark saga complete
            yield return new ShipOrderCommand(OrderId);
            MarkCompleted();    // Wolverine removes the saga from storage
        }
    }

    public IEnumerable<object> Handle(WarehousePickedEvent evt)
    {
        WarehousePicked = true;

        if (PaymentConfirmed)
        {
            yield return new ShipOrderCommand(OrderId);
            MarkCompleted();
        }
    }

    // Timeout: if fulfilment doesn't complete in 48 h, raise a compensating event
    public ScheduledTimeout Timeout(TimeSpan elapsed)
        => elapsed > TimeSpan.FromHours(48)
            ? ScheduledTimeout.Expire()
            : ScheduledTimeout.Continue();

    public void Handle(TimeoutExpired _)
    {
        // Compensate: cancel the order
        MarkCompleted();
    }
}
```

## Setup: MassTransit with RabbitMQ

```csharp
// Infrastructure/DependencyInjection.cs (when MassTransit is the chosen library)
services.AddMassTransit(x =>
{
    // Outbox: store messages in EF Core before delivery — same atomic-guarantee benefit
    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();       // replace in-memory bus with outbox-backed delivery
    });

    // Consumer discovery — scans the assembly for IConsumer<T> implementations
    x.AddConsumers(typeof(InfrastructureAssemblyMarker).Assembly);

    // Saga state machine storage
    x.AddSagaStateMachine<OrderSagaStateMachine, OrderSagaState>()
     .EntityFrameworkRepository(r =>
     {
         r.ConcurrencyMode = ConcurrencyMode.Optimistic;
         r.ExistingDbContext<AppDbContext>();
         r.UseSqlServer();
     });

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(config["RabbitMq:Host"], "/", h =>
        {
            h.Username(config["RabbitMq:Username"]!);
            h.Password(config["RabbitMq:Password"]!);
        });

        // Retry: 5 attempts, 2-second intervals — prevents transient failures becoming DLQ
        cfg.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(2)));

        // Configure endpoints from registered consumers/sagas
        cfg.ConfigureEndpoints(ctx);
    });
});
```

## Pattern: Consumer (MassTransit)

```csharp
// Infrastructure/Messaging/Consumers/OrderCreatedConsumer.cs
public sealed class OrderCreatedConsumer(
    IEmailService email,
    ILogger<OrderCreatedConsumer> logger)
    : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        logger.LogInformation(
            "Received OrderCreated {OrderId}", context.Message.OrderId);

        await email.SendOrderConfirmationAsync(
            context.Message.CustomerId,
            context.Message.OrderId,
            context.CancellationToken);
    }
}

// Consumer definition — controls queue name, retry, concurrency
public sealed class OrderCreatedConsumerDefinition : ConsumerDefinition<OrderCreatedConsumer>
{
    public OrderCreatedConsumerDefinition()
    {
        // Explicit queue name avoids the auto-generated GUID-suffixed name
        EndpointName = "order-created";
        ConcurrentMessageLimit = 10;   // parallel consumer instances per host
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OrderCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // Dead-letter after 3 failures — keeps the queue moving
        endpointConfigurator.UseMessageRetry(r => r.Immediate(3));
    }
}
```

## Pattern: Saga State Machine (MassTransit)

```csharp
// Infrastructure/Messaging/Sagas/OrderSagaState.cs
public sealed class OrderSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }    // MassTransit correlation key
    public string CurrentState { get; set; } = null!;
    public bool PaymentConfirmed { get; set; }
    public bool WarehousePicked  { get; set; }
}

// Infrastructure/Messaging/Sagas/OrderSagaStateMachine.cs
public sealed class OrderSagaStateMachine : MassTransitStateMachine<OrderSagaState>
{
    public State Pending    { get; private set; } = null!;
    public State Fulfilling { get; private set; } = null!;

    public Event<OrderCreatedEvent>       OrderCreated       { get; private set; } = null!;
    public Event<PaymentConfirmedEvent>   PaymentConfirmed   { get; private set; } = null!;
    public Event<WarehousePickedEvent>    WarehousePicked    { get; private set; } = null!;

    public OrderSagaStateMachine()
    {
        // Correlate all messages by OrderId
        CorrelateBy<OrderCreatedEvent>(s => s.CorrelationId,
            m => m.Message.OrderId);
        CorrelateBy<PaymentConfirmedEvent>(s => s.CorrelationId,
            m => m.Message.OrderId);
        CorrelateBy<WarehousePickedEvent>(s => s.CorrelationId,
            m => m.Message.OrderId);

        Initially(
            When(OrderCreated)
                .TransitionTo(Pending));

        During(Pending,
            When(PaymentConfirmed)
                .Then(ctx => ctx.Saga.PaymentConfirmed = true)
                .If(ctx => ctx.Saga.WarehousePicked,
                    then => then.Publish(ctx =>
                        new ShipOrderCommand(ctx.Saga.CorrelationId))
                    .Finalize()),

            When(WarehousePicked)
                .Then(ctx => ctx.Saga.WarehousePicked = true)
                .If(ctx => ctx.Saga.PaymentConfirmed,
                    then => then.Publish(ctx =>
                        new ShipOrderCommand(ctx.Saga.CorrelationId))
                    .Finalize()));

        // Remove completed sagas from storage to prevent unbounded table growth
        SetCompletedWhenFinalized();
    }
}
```

## Anti-patterns

### Don't publish events without an outbox

```csharp
// BAD — if the process crashes between SaveChanges and PublishAsync, the event is lost;
//       the database record exists but no downstream consumer ever processes it
await db.SaveChangesAsync(ct);
await bus.PublishAsync(new OrderCreatedEvent(order.Id.Value));  // fire-and-forget = data loss

// GOOD — enqueue the message before SaveChanges; Wolverine/MassTransit outbox delivers it
//         after the transaction commits, guaranteeing at-least-once delivery
await bus.PublishAsync(new OrderCreatedEvent(order.Id.Value));  // goes to outbox table
await db.SaveChangesAsync(ct);                                   // outbox + business data in one transaction
```

### Don't put domain logic in consumers

```csharp
// BAD — consumer duplicates Order domain logic; two sources of truth for order state
public async Task Consume(ConsumeContext<PaymentConfirmedEvent> ctx)
{
    var order = await db.Orders.FindAsync(ctx.Message.OrderId);
    order!.Status = OrderStatus.Paid;        // direct property mutation bypasses invariants
    order.PaidAt  = DateTime.UtcNow;
    await db.SaveChangesAsync();
}

// GOOD — consumer dispatches a MediatR command; domain logic stays in the handler
public async Task Consume(ConsumeContext<PaymentConfirmedEvent> ctx)
{
    await sender.Send(
        new MarkOrderPaidCommand(ctx.Message.OrderId),
        ctx.CancellationToken);
}
```

### Don't swallow consumer exceptions

```csharp
// BAD — catching all exceptions prevents MassTransit/Wolverine from retrying or dead-lettering;
//       errors disappear silently and messages are acknowledged as if succeeded
public async Task Consume(ConsumeContext<OrderCreatedEvent> ctx)
{
    try { await DoWork(ctx.Message); }
    catch (Exception) { /* swallowed */ }
}

// GOOD — let exceptions propagate so the framework's retry/DLQ policy takes effect
public async Task Consume(ConsumeContext<OrderCreatedEvent> ctx)
{
    // Catch only expected transient exceptions if you need custom back-off; rethrow the rest
    await DoWork(ctx.Message);  // unhandled exception triggers retry, then DLQ
}
```

## Reference

**NuGet Packages:**
```
WolverineFx                                 3.*
WolverineFx.SqlServer                       3.*
WolverineFx.RabbitMQ                        3.*
MassTransit                                 8.*
MassTransit.RabbitMQ                        8.*
MassTransit.EntityFrameworkCore             8.*
```

**Configuration (appsettings.json):**
```json
{
  "RabbitMq": {
    "Host":     "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**EF Core: add Wolverine outbox tables via migration**
```bash
dotnet ef migrations add AddWolverineOutbox \
  --project src/MyApp.Infrastructure \
  --startup-project src/MyApp.Api
```
