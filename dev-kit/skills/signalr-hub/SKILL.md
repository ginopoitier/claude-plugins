---
name: signalr-hub
description: >
  Scaffold a complete SignalR real-time feature — strongly-typed hub interface, hub class,
  domain event notifier handlers, and a Vue composable with cleanup.
  Load this skill when: "signalr", "signalr hub", "real-time", "hub", "/signalr-hub",
  "websocket", "push notification", "IHubContext", "hub composable".
user-invocable: true
argument-hint: "<HubName> [event1 event2 ...]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# SignalR Hub — Real-Time Feature Scaffold

## Core Principles

1. **Always use strongly-typed hubs** — `Hub<IClientInterface>` prevents typos in method names and gives compile-time safety. Never use `Hub` with string-based `Clients.All.SendAsync("MethodName")`.
2. **The interface lives in Application, the hub in Infrastructure** — The `IOrderHubClient` interface is a contract defined in the Application layer. The hub implementation belongs in Infrastructure (it depends on SignalR, an external concern).
3. **Domain event handlers publish to the hub** — Don't call `IHubContext` from inside aggregates or command handlers. Create a dedicated `{Event}HubNotifier` handler (implements `INotificationHandler<TEvent>`) that bridges domain events to SignalR.
4. **Vue composables must clean up on unmount** — Every `conn.on("EventName", handler)` in `onMounted` must have a matching `conn.off("EventName")` in `onUnmounted`. Forgotten listeners cause memory leaks and duplicate event handling.
5. **Group-based targeting over broadcast** — Prefer sending notifications to SignalR groups (e.g., a user's group, a tenant group) rather than broadcasting to all connections. `IHubContext.Clients.Group(userId)` is almost always better than `Clients.All`.

## Patterns

### Strongly-Typed Hub Interface (Application Layer)

```csharp
// Application/Hubs/IOrderHubClient.cs
// GOOD — strongly typed interface; hub and composable must match this contract
public interface IOrderHubClient
{
    Task OrderStatusChanged(OrderStatusChangedDto notification);
    Task OrderCreated(OrderSummaryDto order);
    Task OrderShipped(OrderShippedDto notification);
}

// BAD — untyped hub; typos only fail at runtime
public class OrderHub : Hub
{
    // caller: Clients.All.SendAsync("OrderStatusChangd", dto)  // typo, no compile error
}
```

### Hub Class (Infrastructure Layer)

```csharp
// Infrastructure/Hubs/OrderHub.cs
[Authorize]
public sealed class OrderHub(ILogger<OrderHub> logger) : Hub<IOrderHubClient>
{
    public override async Task OnConnectedAsync()
    {
        // Auto-join the authenticated user to their personal group
        var userId = Context.UserIdentifier!;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        logger.LogDebug("User {UserId} connected to OrderHub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier!;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnDisconnectedAsync(exception);
    }

    // Optional: client can join/leave specific order groups
    public async Task JoinOrderGroup(string orderId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order:{orderId}");

    public async Task LeaveOrderGroup(string orderId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order:{orderId}");
}
```

### Domain Event Notifier (Application Layer)

```csharp
// Application/Hubs/Notifiers/OrderStatusChangedNotifier.cs
// GOOD — dedicated handler bridges domain event to SignalR; command handler stays clean
internal sealed class OrderStatusChangedNotifier(
    IHubContext<OrderHub, IOrderHubClient> hubContext,
    ILogger<OrderStatusChangedNotifier> logger)
    : INotificationHandler<OrderStatusChangedDomainEvent>
{
    public async Task Handle(OrderStatusChangedDomainEvent notification, CancellationToken ct)
    {
        var dto = new OrderStatusChangedDto(
            notification.OrderId.Value,
            notification.NewStatus.ToString());

        // Target only the specific user — not broadcast to all
        await hubContext.Clients
            .Group($"user:{notification.UserId.Value}")
            .OrderStatusChanged(dto);

        logger.LogDebug("Sent OrderStatusChanged to user {UserId}", notification.UserId.Value);
    }
}
```

### Vue Composable with Cleanup

```typescript
// features/orders/composables/useOrderHub.ts
// GOOD — registers and deregisters handlers; won't leak on component unmount
import { onMounted, onUnmounted } from 'vue'
import { useSignalRStore } from '@/stores/signalrStore'
import { useOrderStore } from '../stores/orderStore'
import type { OrderStatusChangedDto, OrderSummaryDto } from '../types'

const HUB_URL = `${import.meta.env.VITE_API_URL}/hubs/orders`

export function useOrderHub() {
  const signalRStore = useSignalRStore()
  const orderStore = useOrderStore()

  onMounted(async () => {
    const conn = await signalRStore.connect(HUB_URL)

    conn.on('OrderStatusChanged', (dto: OrderStatusChangedDto) => {
      orderStore.updateStatus(dto.orderId, dto.newStatus)
    })

    conn.on('OrderCreated', (dto: OrderSummaryDto) => {
      orderStore.orders.push(dto)
    })
  })

  // CRITICAL — deregister every handler registered in onMounted
  onUnmounted(() => {
    const conn = signalRStore.getOrCreate(HUB_URL)
    conn.off('OrderStatusChanged')
    conn.off('OrderCreated')
  })
}

// BAD — no cleanup; duplicate handlers accumulate when component remounts
onMounted(async () => {
  const conn = await signalRStore.connect(HUB_URL)
  conn.on('OrderStatusChanged', handler)
  // no onUnmounted → handler registered again on remount → called twice
})
```

## Anti-patterns

### Using IHubContext Inside a Command Handler

```csharp
// BAD — command handler directly pushes to hub; couples use case to transport
internal sealed class CancelOrderHandler(
    AppDbContext db,
    IHubContext<OrderHub, IOrderHubClient> hubContext)  // wrong dependency
    : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var result = order.Cancel();
        await hubContext.Clients.All.OrderStatusChanged(...);  // transport in use case layer
        await db.SaveChangesAsync(ct);
        return result;
    }
}

// GOOD — handler raises domain event; notifier handles hub push
// CancelOrderHandler: just calls order.Cancel() and saves
// OrderCancelledNotifier: INotificationHandler<OrderCancelledDomainEvent> → pushes to hub
```

### Broadcasting to All Connections

```csharp
// BAD — sends order updates to all connected users
await hubContext.Clients.All.OrderStatusChanged(dto);

// GOOD — target only the user who owns this order
await hubContext.Clients.Group($"user:{order.OwnerId}").OrderStatusChanged(dto);
```

### Missing Hub Registration in Program.cs

```csharp
// BAD — hub class exists but is never mapped; client gets 404
var app = builder.Build();
app.MapAllEndpoints();
// hub not mapped → SignalR connections fail silently

// GOOD — map hub routes alongside API routes
var app = builder.Build();
app.MapAllEndpoints();
app.MapHub<OrderHub>("/hubs/orders");  // must match VITE_API_URL + /hubs/orders in Vue
```

## Decision Guide

| Scenario | Recommendation |
|----------|----------------|
| New real-time feature needed | `/signalr-hub <Name> <Event1> <Event2>` |
| Existing hub needs a new event | Add to `IClientInterface`, implement in notifier, add to composable |
| Send to one user only | `Clients.Group($"user:{userId}")` |
| Send to all viewers of an entity | `Clients.Group($"order:{orderId}")` with client join/leave |
| Broadcast to everyone | `Clients.All` — only for truly global notifications |
| Vue component loses connection on remount | Check `onUnmounted` — missing `conn.off()` calls |
| Hub returns 401 | Verify `[Authorize]` + `AddSignalR()` + `UseAuthentication()` order in Program.cs |
| Messages delivered but wrong component updates | Check DTO property names match TypeScript interface exactly |

## Execution

Read `~/.claude/kit.config.md` for `DEFAULT_NAMESPACE`. Inspect existing hubs for project conventions.

### `/signalr-hub <HubName> [event1 event2 ...]`

Generates:

1. **Hub Interface** (`Application/Hubs/I{Name}HubClient.cs`) — one method per event, typed DTOs
2. **Hub Class** (`Infrastructure/Hubs/{Name}Hub.cs`) — extends `Hub<IClientInterface>`, `[Authorize]`, group join/leave in `OnConnectedAsync`/`OnDisconnectedAsync`
3. **DTO Records** (`Application/Hubs/Dtos/`) — one record per event, minimal fields
4. **Domain Event Notifier(s)** (`Application/Hubs/Notifiers/`) — `INotificationHandler<TDomainEvent>` stubs that call `IHubContext`
5. **Vue Composable** (`features/{name}/composables/use{Name}Hub.ts`) — `onMounted` connect + register, `onUnmounted` deregister, TypeScript types
6. **Registration reminder** — show where to add `MapHub<{Name}Hub>("/hubs/{name}")` in `Program.cs` and confirm `AddSignalR()` is present

### After Generation
Run `/verify` to ensure the solution builds cleanly.

$ARGUMENTS
