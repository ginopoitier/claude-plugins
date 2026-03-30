# SignalR — Strongly-Typed Hubs & Real-Time Integration Reference

## Overview

SignalR provides bidirectional real-time communication over WebSockets (with SSE and long-polling fallbacks). In Clean Architecture, the hub lives in the Presentation layer and receives domain events dispatched by MediatR notification handlers — domain logic never directly references SignalR. This document covers strongly-typed hubs, domain event integration, group management, and client reconnect patterns.

## Setup: Registration

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration config)
{
    services.AddSignalR(options =>
    {
        // Keep-alive: server pings client every 15 s to detect dropped connections
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);

        // Client must respond within 30 s or the server closes the connection
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);

        // Increase max message size for payload-heavy notifications (default: 32 KB)
        options.MaximumReceiveMessageSize = 64 * 1024;  // 64 KB

        // Enable detailed errors in dev only — stack traces visible to the client otherwise
        options.EnableDetailedErrors = false;
    })
    // Sticky sessions are required when using multiple server instances
    // Replace with .AddAzureSignalR() or .AddStackExchangeRedis() for scale-out
    .AddMessagePackProtocol();  // binary protocol — smaller payload than JSON

    return services;
}

// Program.cs
app.UseAuthentication();
app.UseAuthorization();

// Map the hub endpoint — must come after auth middleware
app.MapHub<OrderHub>("/hubs/orders")
   .RequireAuthorization();  // all connections must be authenticated
```

## Pattern: Strongly-Typed Hub

A strongly-typed hub uses an interface for client methods, giving compile-time safety when calling methods on connected clients.

```csharp
// Presentation/Hubs/Orders/IOrderHubClient.cs
// Contract: defines the methods the server can invoke on connected clients
public interface IOrderHubClient
{
    // Each method corresponds to an event the client listens for
    Task OrderStatusChanged(OrderStatusChangedNotification notification);
    Task OrderCreated(OrderCreatedNotification notification);
    Task PaymentConfirmed(PaymentConfirmedNotification notification);
}

// Notification DTOs — simple records, no domain types
// Presentation/Hubs/Orders/Notifications.cs
public sealed record OrderStatusChangedNotification(
    Guid   OrderId,
    string NewStatus,
    string PreviousStatus,
    DateTimeOffset ChangedAt);

public sealed record OrderCreatedNotification(
    Guid   OrderId,
    Guid   CustomerId,
    decimal TotalAmount,
    string  Currency);

public sealed record PaymentConfirmedNotification(
    Guid OrderId,
    Guid PaymentId,
    DateTimeOffset ConfirmedAt);

// Presentation/Hubs/Orders/OrderHub.cs
// Hub<T> — the generic parameter is the strongly-typed client interface
public sealed class OrderHub(
    ILogger<OrderHub> logger,
    ISender sender)            // MediatR for handling client-initiated commands
    : Hub<IOrderHubClient>
{
    // Called when a client connects — add them to their personal group
    public override async Task OnConnectedAsync()
    {
        // Groups allow targeted broadcasts without iterating all connections
        // Customer group: all connections for a given customer
        var customerId = Context.User!.FindFirst("sub")!.Value;
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName.Customer(customerId));

        // Role-based group: admins see all order events
        if (Context.User.IsInRole("admin"))
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName.Admins);

        logger.LogInformation(
            "Client connected {ConnectionId} for customer {CustomerId}",
            Context.ConnectionId, customerId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
            logger.LogWarning(exception,
                "Client disconnected with error {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // Client-invokable hub method — client calls this to subscribe to a specific order
    public async Task SubscribeToOrder(Guid orderId)
    {
        // Authorization check: customer can only subscribe to their own orders
        var result = await sender.Send(new CanAccessOrderQuery(
            orderId,
            Guid.Parse(Context.User!.FindFirst("sub")!.Value)));

        if (result.IsFailure)
        {
            // Throw HubException — client receives an error, connection stays alive
            throw new HubException("Access denied to order " + orderId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName.Order(orderId));
        logger.LogDebug("Connection {ConnectionId} subscribed to order {OrderId}",
            Context.ConnectionId, orderId);
    }

    public Task UnsubscribeFromOrder(Guid orderId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName.Order(orderId));
}

// Presentation/Hubs/Orders/GroupName.cs
// Centralize group name generation to avoid typo bugs
public static class GroupName
{
    public static string Customer(string customerId) => $"customer:{customerId}";
    public static string Order(Guid orderId)          => $"order:{orderId}";
    public const  string Admins                       = "admins";
}
```

## Pattern: Domain Event → Hub Integration

MediatR notification handlers bridge domain events to SignalR. The hub is injected via `IHubContext<THub, TClient>` — this is the DI-safe way to push from outside the hub class.

```csharp
// Application/Orders/Events/OrderStatusChangedHandler.cs
// This handler lives in Application — it knows about the domain event but NOT about SignalR
// The Hub reference is injected by the infrastructure layer's handler registration
internal sealed class OrderStatusChangedHubNotifier(
    IHubContext<OrderHub, IOrderHubClient> hubContext,
    ILogger<OrderStatusChangedHubNotifier> logger)
    : INotificationHandler<OrderStatusChangedDomainEvent>
{
    public async Task Handle(
        OrderStatusChangedDomainEvent notification,
        CancellationToken ct)
    {
        var dto = new OrderStatusChangedNotification(
            OrderId:      notification.OrderId.Value,
            NewStatus:    notification.NewStatus.ToString(),
            PreviousStatus: notification.PreviousStatus.ToString(),
            ChangedAt:    notification.OccurredAt);

        // Send to the order-specific group: all clients subscribed to this order
        await hubContext.Clients
            .Group(GroupName.Order(notification.OrderId.Value))
            .OrderStatusChanged(dto);

        // Also send to the customer's personal group (for notification centre UI)
        await hubContext.Clients
            .Group(GroupName.Customer(notification.CustomerId.Value.ToString()))
            .OrderStatusChanged(dto);

        // Admins always receive all order events
        await hubContext.Clients
            .Group(GroupName.Admins)
            .OrderStatusChanged(dto);

        logger.LogInformation(
            "Pushed OrderStatusChanged for order {OrderId} to hub groups",
            notification.OrderId.Value);
    }
}

// Register in Application/DependencyInjection.cs — alongside other notification handlers
// MediatR.RegisterServicesFromAssembly picks this up automatically by convention
```

## Pattern: Hub Scale-Out with Redis Backplane

When running multiple API instances, use the Redis backplane so messages sent from any instance reach all connected clients.

```csharp
// Infrastructure/DependencyInjection.cs
services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
})
.AddStackExchangeRedis(config.GetConnectionString("Redis")!, options =>
{
    // Prefix all backplane keys to avoid collisions with application cache keys
    options.Configuration.ChannelPrefix = RedisChannel.Literal("signalr:");
});

// Azure SignalR Service alternative — fully managed, no Redis required
// .AddAzureSignalR(config["Azure:SignalRConnectionString"]);
```

## Pattern: Client Reconnect (TypeScript / Vue)

The client must implement reconnect logic — by default, SignalR uses exponential back-off automatically but you need to handle state restoration on reconnect.

```typescript
// src/composables/useOrderHub.ts (Vue 3 + TypeScript)
import { HubConnectionBuilder, HubConnection, LogLevel, HubConnectionState }
    from '@microsoft/signalr'
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack'

export function useOrderHub(getAccessToken: () => Promise<string>) {
    let connection: HubConnection | null = null

    function buildConnection(): HubConnection {
        return new HubConnectionBuilder()
            .withUrl('/hubs/orders', {
                // Provide the JWT bearer token for hub authentication
                accessTokenFactory: getAccessToken
            })
            .withHubProtocol(new MessagePackHubProtocol())  // matches server MessagePack config
            // Automatic reconnect: tries after 0, 2, 10, 30 seconds, then stops
            .withAutomaticReconnect([0, 2000, 10000, 30000])
            .configureLogging(LogLevel.Warning)
            .build()
    }

    async function start(orderId: string): Promise<void> {
        connection = buildConnection()

        // Register handlers BEFORE starting — avoids race between start and first message
        connection.on('OrderStatusChanged', (notification: OrderStatusChangedNotification) => {
            console.log('Order status changed:', notification)
            // Update Pinia store
        })

        connection.on('PaymentConfirmed', (notification: PaymentConfirmedNotification) => {
            console.log('Payment confirmed:', notification)
        })

        // Reconnecting: the UI should show a "reconnecting..." banner
        connection.onreconnecting(error => {
            console.warn('SignalR reconnecting...', error)
        })

        // Reconnected: re-subscribe to groups — server group membership is lost on disconnect
        connection.onreconnected(async connectionId => {
            console.info('SignalR reconnected', connectionId)
            // Re-subscribe after reconnect — the server loses group membership on disconnect
            await connection!.invoke('SubscribeToOrder', orderId)
        })

        connection.onclose(error => {
            if (error) console.error('SignalR closed with error:', error)
        })

        await connection.start()

        // Subscribe to the specific order after connection is established
        await connection.invoke('SubscribeToOrder', orderId)
    }

    async function stop(): Promise<void> {
        if (connection?.state === HubConnectionState.Connected) {
            await connection.stop()
        }
        connection = null
    }

    return { start, stop }
}

interface OrderStatusChangedNotification {
    orderId:        string
    newStatus:      string
    previousStatus: string
    changedAt:      string
}

interface PaymentConfirmedNotification {
    orderId:     string
    paymentId:   string
    confirmedAt: string
}
```

## Pattern: Hub Authentication with Cookies (for SSE / WebSocket upgrade issues)

WebSocket upgrade requests from browsers cannot set the `Authorization` header. Use the `accessTokenFactory` (above) for SPA clients, or cookies for server-rendered apps.

```csharp
// Program.cs — let SignalR read the token from the query string for WebSocket connections
// (the JS client automatically appends ?access_token=... when using accessTokenFactory)
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            // WebSocket and SSE upgrade requests arrive as GET with a query string parameter
            // rather than an Authorization header — read the token from the query string
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;

                return Task.CompletedTask;
            }
        };
    });
```

## Anti-patterns

### Don't put domain logic in the hub

```csharp
// BAD — hub applies business rules and writes directly to the database;
//       this bypasses the application layer, skips validation, and makes testing impossible
public sealed class OrderHub(AppDbContext db) : Hub<IOrderHubClient>
{
    public async Task CancelOrder(Guid orderId)
    {
        var order = await db.Orders.FindAsync(orderId);
        order!.Status = OrderStatus.Cancelled;   // no validation, no domain events
        await db.SaveChangesAsync();
        await Clients.All.OrderStatusChanged(...);
    }
}

// GOOD — hub delegates to MediatR; domain logic stays in the command handler
public sealed class OrderHub(ISender sender) : Hub<IOrderHubClient>
{
    public async Task CancelOrder(Guid orderId)
    {
        var result = await sender.Send(new CancelOrderCommand(orderId));
        if (result.IsFailure)
            throw new HubException(result.Error!.Description);
        // The command handler publishes the domain event → hub notifier pushes the update
    }
}
```

### Don't broadcast to `Clients.All` when only specific users need the message

```csharp
// BAD — sends every order update to every connected client;
//       leaks other users' order data and wastes bandwidth
await hubContext.Clients.All.OrderStatusChanged(dto);

// GOOD — use groups scoped to the customer or the specific order
await hubContext.Clients
    .Group(GroupName.Order(notification.OrderId.Value))
    .OrderStatusChanged(dto);
```

### Don't forget to re-subscribe to groups after reconnect

```typescript
// BAD — after reconnect, the client's group memberships are gone on the server;
//       the client receives no further messages even though the connection is alive
connection.onreconnected(() => {
    console.log('Reconnected')
    // No re-subscription — silently stops receiving messages
})

// GOOD — invoke SubscribeToOrder again after every reconnect
connection.onreconnected(async () => {
    await connection!.invoke('SubscribeToOrder', currentOrderId)
})
```

## Reference

**NuGet Packages:**
```
Microsoft.AspNetCore.SignalR                    9.0.*   (inbox — no NuGet needed in .NET 9)
Microsoft.AspNetCore.SignalR.StackExchangeRedis 9.0.*   (Redis backplane for scale-out)
Microsoft.AspNetCore.SignalR.Protocols.MessagePack 9.0.*
```

**npm Packages:**
```
@microsoft/signalr                 8.*
@microsoft/signalr-protocol-msgpack 8.*
```

**Configuration (appsettings.json):**
```json
{
  "SignalR": {
    "EnableDetailedErrors": false
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379,password=yourpassword"
  }
}
```
