---
name: signalr-hub
description: >
  Scaffold Vue.js TypeScript integration for consuming a .NET-hosted SignalR hub.
  Creates a strongly-typed composable with proper cleanup patterns for real-time features.
  Load this skill when: "signalr", "signalr hub", "real-time", "hub", "/signalr-hub",
  "websocket", "push notification", "hub composable", "vue signalr".
user-invocable: true
argument-hint: "<HubName> [event1 event2 ...]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# SignalR Hub — Vue.js Integration Scaffold

## Core Principles

1. **Strongly-typed client interfaces** — Define TypeScript interfaces that match the .NET hub's `IClientInterface` for compile-time safety.
2. **Vue composables must clean up on unmount** — Every `conn.on("EventName", handler)` in `onMounted` must have a matching `conn.off("EventName")` in `onUnmounted`. Forgotten listeners cause memory leaks and duplicate event handling.
3. **Group-based targeting over broadcast** — Prefer joining SignalR groups (user groups, tenant groups) rather than relying on broadcasts.
4. **Connection management** — Use a centralized SignalR store for connection lifecycle and error handling.
5. **Type safety** — DTOs and event payloads should be strongly typed to match the .NET backend.

## Patterns

### TypeScript Client Interface

```typescript
// types/signalr.ts
// Match the .NET IOrderHubClient interface exactly
export interface IOrderHubClient {
  orderStatusChanged(notification: OrderStatusChangedDto): void
  orderCreated(order: OrderSummaryDto): void
  orderShipped(notification: OrderShippedDto): void
}

export interface OrderStatusChangedDto {
  orderId: string
  newStatus: string
}

export interface OrderSummaryDto {
  id: string
  customerName: string
  total: number
  status: string
}

export interface OrderShippedDto {
  orderId: string
  trackingNumber: string
  estimatedDelivery: string
}
```

### Vue Composable with Cleanup

```typescript
// composables/useOrderHub.ts
import { onMounted, onUnmounted } from 'vue'
import { useSignalRStore } from '@/stores/signalrStore'
import { useOrderStore } from '@/stores/orderStore'
import type { IOrderHubClient, OrderStatusChangedDto, OrderSummaryDto, OrderShippedDto } from '@/types/signalr'

// Use a relative path so Vite can proxy `/hubs` to the .NET backend in development
// and the same path works when the SPA is hosted by the .NET BFF in production.
const HUB_URL = '/hubs/orders'

export function useOrderHub() {
  const signalRStore = useSignalRStore()
  const orderStore = useOrderStore()

  onMounted(async () => {
    const conn = await signalRStore.connect(HUB_URL)

    // Register event handlers that match the .NET hub interface
    conn.on('OrderStatusChanged', (dto: OrderStatusChangedDto) => {
      orderStore.updateOrderStatus(dto.orderId, dto.newStatus)
    })

    conn.on('OrderCreated', (dto: OrderSummaryDto) => {
      orderStore.addOrder(dto)
    })

    conn.on('OrderShipped', (dto: OrderShippedDto) => {
      orderStore.updateShippingInfo(dto.orderId, dto.trackingNumber, dto.estimatedDelivery)
    })

    // Join user-specific group for targeted notifications
    const userId = orderStore.currentUserId
    if (userId) {
      await conn.invoke('JoinUserGroup', userId)
    }
  })

  // CRITICAL — deregister every handler registered in onMounted
  onUnmounted(async () => {
    const conn = signalRStore.getConnection(HUB_URL)
    if (conn) {
      conn.off('OrderStatusChanged')
      conn.off('OrderCreated')
      conn.off('OrderShipped')

      // Leave user group
      const userId = orderStore.currentUserId
      if (userId) {
        await conn.invoke('LeaveUserGroup', userId)
      }
    }
  })

  return {
    // Expose methods to join/leave specific groups
    async joinOrderGroup(orderId: string) {
      const conn = signalRStore.getConnection(HUB_URL)
      if (conn) {
        await conn.invoke('JoinOrderGroup', orderId)
      }
    },

    async leaveOrderGroup(orderId: string) {
      const conn = signalRStore.getConnection(HUB_URL)
      if (conn) {
        await conn.invoke('LeaveOrderGroup', orderId)
      }
    }
  }
}
```

### SignalR Store for Connection Management

> Prefer a relative hub endpoint so Vite can proxy `/hubs` to your .NET backend during development.
>
> Example `vite.config.ts` proxy config for a .NET BFF:
>
> ```ts
> server: {
>   proxy: {
>     '/api': 'http://localhost:5000',
>     '/hubs': { target: 'http://localhost:5000', ws: true }
>   }
> }
> ```
>
```typescript
// stores/signalrStore.ts
import { defineStore } from 'pinia'
import { HubConnectionBuilder, LogLevel, HubConnection } from '@microsoft/signalr'

interface SignalRState {
  connections: Record<string, HubConnection>
}

export const useSignalRStore = defineStore('signalr', {
  state: (): SignalRState => ({
    connections: {}
  }),

  actions: {
    async connect(hubUrl: string): Promise<HubConnection> {
      if (this.connections[hubUrl]) {
        return this.connections[hubUrl]
      }

      const connection = new HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => localStorage.getItem('authToken') || ''
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Information)
        .build()

      await connection.start()
      this.connections[hubUrl] = connection

      return connection
    },

    getConnection(hubUrl: string): HubConnection | null {
      return this.connections[hubUrl] || null
    },

    async disconnect(hubUrl: string) {
      const connection = this.connections[hubUrl]
      if (connection) {
        await connection.stop()
        delete this.connections[hubUrl]
      }
    }
  }
})
```

## Anti-patterns

### No Cleanup on Component Unmount

```typescript
// BAD — handlers accumulate on remount
onMounted(async () => {
  const conn = await signalRStore.connect(HUB_URL)
  conn.on('OrderStatusChanged', handler)
  // No onUnmounted — handler registered again on remount → called multiple times
})
```

### Broadcasting Instead of Groups

```typescript
// BAD — sends to all connected clients
conn.on('OrderStatusChanged', (dto) => {
  // This assumes the backend broadcasts to all clients
  // Instead, backend should target specific user groups
})
```

### Weakly-Typed Event Handlers

```typescript
// BAD — no type safety
conn.on('OrderStatusChanged', (dto: any) => {
  orderStore.updateStatus(dto.orderId, dto.newStatus) // Runtime errors possible
})
```

## Usage Examples

### Basic Real-Time Order Tracking

```typescript
// pages/OrderDetails.vue
<script setup lang="ts">
import { useOrderHub } from '@/composables/useOrderHub'

const { joinOrderGroup, leaveOrderGroup } = useOrderHub()
const route = useRoute()

onMounted(async () => {
  // Join the specific order group for targeted updates
  await joinOrderGroup(route.params.orderId)
})

onUnmounted(async () => {
  // Clean up group membership
  await leaveOrderGroup(route.params.orderId)
})
</script>
```

### Dashboard with Multiple Hub Connections

```typescript
// pages/Dashboard.vue
<script setup lang="ts">
import { useOrderHub } from '@/composables/useOrderHub'
import { useNotificationHub } from '@/composables/useNotificationHub'

// Use multiple hub composables for different real-time features
useOrderHub()
useNotificationHub()
</script>
```

## Command Reference

### `/signalr-hub <HubName> [event1 event2 ...]`

Creates a Vue.js SignalR integration for the specified hub:

1. **TypeScript interfaces** — Strongly-typed DTOs matching the .NET hub's client interface
2. **Vue composable** — `use${HubName}Hub.ts` with proper cleanup patterns
3. **Store integration** — Updates relevant Pinia stores when events are received
4. **Group management** — Methods to join/leave SignalR groups for targeted notifications

**Example:**
```
/signalr-hub Order StatusChanged Created Shipped
```

Creates:
- `types/orderSignalr.ts` — TypeScript interfaces
- `composables/useOrderHub.ts` — Vue composable with cleanup
- Integration with existing order store

**Assumptions:**
- .NET SignalR hub is already hosted and accessible via the BFF
- Vite dev server proxies `/hubs` to the .NET backend and production serves the SPA from the same host
- Hub follows strongly-typed pattern with `I${HubName}HubClient` interface
- Authentication token available in localStorage as 'authToken'
- Pinia stores exist for domain objects (orders, notifications, etc.)

| Scenario | Command |
|----------|---------|
| Order tracking | `/signalr-hub Order StatusChanged Created Shipped` |
| Chat system | `/signalr-hub Chat MessageReceived UserJoined UserLeft` |
| Notifications | `/signalr-hub Notification NewAlert AlertRead` |
| Live dashboard | `/signalr-hub Dashboard MetricUpdated UserActivity` |

Backend note: map the hub endpoint at `/hubs/{name}` on the .NET side to match the Vue client route.
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
