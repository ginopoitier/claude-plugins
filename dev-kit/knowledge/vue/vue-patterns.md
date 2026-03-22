# Vue 3 + TypeScript + Pinia + SignalR Patterns

## Project Structure

```
src/
  assets/
  components/         # shared reusable components
  composables/        # useXxx composables for shared logic
  features/           # feature-based folders (mirror backend structure)
    orders/
      components/     # feature-specific components
      composables/    # feature-specific composables
      stores/         # Pinia store(s) for this feature
      types.ts        # feature-specific types/interfaces
      api.ts          # API client functions for this feature
  layouts/
  pages/              # route-level components (one per route)
  router/
  stores/             # global stores (auth, app state, signalr)
  lib/
    api.ts            # base axios/fetch client
    signalr.ts        # SignalR connection factory
  types/              # global shared TypeScript types
  App.vue
  main.ts
```

## Component Conventions

```vue
<!-- Always use <script setup lang="ts"> -->
<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useOrderStore } from '@/features/orders/stores/orderStore'

// Props always typed with defineProps<T>()
const props = defineProps<{
  orderId: string
  readonly?: boolean
}>()

// Emits always typed
const emit = defineEmits<{
  cancelled: [orderId: string]
  updated: [order: Order]
}>()

const orderStore = useOrderStore()
const isLoading = ref(false)

const order = computed(() => orderStore.getById(props.orderId))

async function handleCancel() {
  isLoading.value = true
  try {
    await orderStore.cancelOrder(props.orderId)
    emit('cancelled', props.orderId)
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <!-- always use Tailwind classes, no inline styles -->
  <div class="flex items-center gap-4 p-4 rounded-lg border border-gray-200">
    <slot />
  </div>
</template>
```

## Pinia Store Pattern

```typescript
// features/orders/stores/orderStore.ts
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { fetchOrders, cancelOrder } from '../api'
import type { Order } from '../types'

export const useOrderStore = defineStore('orders', () => {
  // State
  const orders = ref<Order[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Getters
  const getById = computed(() => (id: string) =>
    orders.value.find(o => o.id === id)
  )

  const pendingOrders = computed(() =>
    orders.value.filter(o => o.status === 'Pending')
  )

  // Actions
  async function loadOrders() {
    isLoading.value = true
    error.value = null
    try {
      orders.value = await fetchOrders()
    } catch (e) {
      error.value = 'Failed to load orders'
      throw e
    } finally {
      isLoading.value = false
    }
  }

  async function cancelOrder(orderId: string) {
    await cancelOrder(orderId)
    const order = orders.value.find(o => o.id === orderId)
    if (order) order.status = 'Cancelled'
  }

  return { orders, isLoading, error, getById, pendingOrders, loadOrders, cancelOrder }
})
```

## API Client Layer

```typescript
// lib/api.ts — base client
import axios from 'axios'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  res => res,
  err => {
    // Handle ProblemDetails from .NET API
    const problem = err.response?.data
    if (problem?.title) {
      throw new ApiError(problem.title, problem.detail, err.response.status, problem)
    }
    throw err
  }
)

// features/orders/api.ts — typed feature API
import { api } from '@/lib/api'
import type { Order, CreateOrderRequest } from './types'

export const fetchOrders = () =>
  api.get<Order[]>('/api/orders').then(r => r.data)

export const createOrder = (data: CreateOrderRequest) =>
  api.post<{ id: string }>('/api/orders', data).then(r => r.data)

export const cancelOrder = (id: string) =>
  api.delete(`/api/orders/${id}/cancel`)
```

## SignalR — Connection Store

```typescript
// stores/signalrStore.ts
import { defineStore } from 'pinia'
import { ref } from 'vue'
import * as signalR from '@microsoft/signalr'

export const useSignalRStore = defineStore('signalr', () => {
  const connections = ref<Map<string, signalR.HubConnection>>(new Map())

  function getOrCreate(hubUrl: string): signalR.HubConnection {
    if (connections.value.has(hubUrl)) return connections.value.get(hubUrl)!

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => localStorage.getItem('token') ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connections.value.set(hubUrl, connection)
    return connection
  }

  async function connect(hubUrl: string) {
    const conn = getOrCreate(hubUrl)
    if (conn.state === signalR.HubConnectionState.Disconnected) {
      await conn.start()
    }
    return conn
  }

  async function disconnect(hubUrl: string) {
    const conn = connections.value.get(hubUrl)
    if (conn) {
      await conn.stop()
      connections.value.delete(hubUrl)
    }
  }

  return { connect, disconnect, getOrCreate }
})
```

## SignalR — Feature Composable

```typescript
// features/orders/composables/useOrderUpdates.ts
import { onMounted, onUnmounted } from 'vue'
import { useSignalRStore } from '@/stores/signalrStore'
import { useOrderStore } from '../stores/orderStore'

const HUB_URL = `${import.meta.env.VITE_API_URL}/hubs/orders`

export function useOrderUpdates() {
  const signalRStore = useSignalRStore()
  const orderStore = useOrderStore()

  onMounted(async () => {
    const conn = await signalRStore.connect(HUB_URL)
    conn.on('OrderStatusChanged', (orderId: string, status: string) => {
      orderStore.updateStatus(orderId, status)
    })
    conn.on('OrderCreated', (order: Order) => {
      orderStore.orders.push(order)
    })
  })

  onUnmounted(() => {
    const conn = signalRStore.getOrCreate(HUB_URL)
    conn.off('OrderStatusChanged')
    conn.off('OrderCreated')
  })
}
```

## TypeScript Rules

```typescript
// Never use 'any' — use 'unknown' if truly unknown and narrow it
function handleError(e: unknown) {
  if (e instanceof ApiError) { ... }
}

// Define explicit types for API responses
interface Order {
  id: string
  status: 'Pending' | 'Confirmed' | 'Shipped' | 'Cancelled'
  totalAmount: number
  createdAt: string // ISO string from API
}

// Use type-safe route params
import { useRoute } from 'vue-router'
const route = useRoute()
const orderId = route.params.id as string // assert if router guarantees it
```

## Vite Config Conventions

```typescript
// vite.config.ts
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
      '/hubs': { target: 'http://localhost:5000', ws: true },
    },
  },
})
```

## TailwindCSS Rules

- Use Tailwind utility classes only — no custom CSS files unless for third-party overrides
- Use `@apply` in component `<style>` blocks sparingly, only for repeated patterns
- Use `tailwind.config.ts` to extend theme (colors, fonts) with design tokens
- Prefer responsive variants (`sm:`, `md:`, `lg:`) over JS-driven layout changes
