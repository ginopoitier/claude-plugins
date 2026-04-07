---
name: pinia-store
description: >
  Scaffold a Pinia store using the Composition API style with typed state, getters, and async actions.
  Load this skill when: "pinia", "store", "pinia store", "create store", "state management",
  "defineStore", "pinia action", "pinia getter", "/pinia-store".
user-invocable: true
argument-hint: "<StoreName> [entity description]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# Pinia Store — Composition API Style Scaffold

## Core Principles

1. **Composition API style only** — Always `defineStore('id', () => { ... })`. Never use the Options API `state/getters/actions` object form. The setup-function form gives full TypeScript inference and supports composables inside the store.
2. **State is `ref`, derived values are `computed`** — `ref<T>` for mutable state, `computed(() => ...)` for derived values. `reactive` objects in stores are allowed but prefer `ref` for individual fields — it is more explicit and avoids accidental spread-destructuring of reactivity.
3. **Actions are plain async functions** — No return value typing needed for `void` actions; type the return for actions that fetch and return data.
4. **No side effects in computed** — Getters must be pure derivations. Never call `fetch`, mutate state, or trigger navigation inside a `computed`.
5. **API calls belong in `features/{name}/api.ts`** — Store actions call the typed API module; they never contain `fetch()` or `axios()` calls inline.
6. **The store ID matches the feature** — Use `defineStore('products', ...)` not `defineStore('productStore', ...)`. The ID is used by Vue DevTools and Pinia's `storeToRefs`.

## Patterns

### Standard Feature Store

```typescript
// features/products/stores/productStore.ts
import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { fetchProducts, fetchProductById, createProduct, deleteProduct } from '@/features/products/api'
import type { Product, CreateProductRequest } from '@/features/products/types'

export const useProductStore = defineStore('products', () => {
  // ── State ─────────────────────────────────────────────────────────────
  const products = ref<Product[]>([])
  const selectedId = ref<string | null>(null)
  const isLoading = ref(false)
  const isSaving = ref(false)
  const error = ref<string | null>(null)

  // ── Getters ───────────────────────────────────────────────────────────
  const selectedProduct = computed(() =>
    products.value.find((p) => p.id === selectedId.value) ?? null,
  )

  const inStockProducts = computed(() =>
    products.value.filter((p) => p.stock > 0),
  )

  const productCount = computed(() => products.value.length)

  // ── Actions ───────────────────────────────────────────────────────────
  async function fetchAll() {
    isLoading.value = true
    error.value = null
    try {
      products.value = await fetchProducts()
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load products'
    } finally {
      isLoading.value = false
    }
  }

  async function fetchById(id: string) {
    isLoading.value = true
    error.value = null
    try {
      const product = await fetchProductById(id)
      const index = products.value.findIndex((p) => p.id === id)
      if (index >= 0) {
        products.value[index] = product
      } else {
        products.value.push(product)
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load product'
    } finally {
      isLoading.value = false
    }
  }

  async function create(request: CreateProductRequest) {
    isSaving.value = true
    error.value = null
    try {
      const created = await createProduct(request)
      products.value.push(created)
      return created
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to create product'
      throw err  // re-throw so the caller can react (e.g. show a toast)
    } finally {
      isSaving.value = false
    }
  }

  async function remove(id: string) {
    const backup = [...products.value]
    // Optimistic removal
    products.value = products.value.filter((p) => p.id !== id)
    try {
      await deleteProduct(id)
    } catch (err) {
      // Roll back on failure
      products.value = backup
      error.value = err instanceof Error ? err.message : 'Failed to delete product'
      throw err
    }
  }

  function select(id: string | null) {
    selectedId.value = id
  }

  function clearError() {
    error.value = null
  }

  // ── Real-time updates (called from SignalR composable) ────────────────
  function applyServerUpdate(updated: Product) {
    const index = products.value.findIndex((p) => p.id === updated.id)
    if (index >= 0) {
      products.value[index] = updated
    }
  }

  return {
    // State
    products,
    selectedId,
    isLoading,
    isSaving,
    error,
    // Getters
    selectedProduct,
    inStockProducts,
    productCount,
    // Actions
    fetchAll,
    fetchById,
    create,
    remove,
    select,
    clearError,
    applyServerUpdate,
  }
})
```

### Auth Store (singleton shared state)

```typescript
// stores/authStore.ts
import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { loginUser, logoutUser, refreshToken } from '@/features/auth/api'
import type { User, LoginRequest, AuthTokens } from '@/features/auth/types'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const tokens = ref<AuthTokens | null>(null)
  const isAuthenticating = ref(false)

  const isAuthenticated = computed(() => user.value !== null && tokens.value !== null)
  const accessToken = computed(() => tokens.value?.accessToken ?? null)
  const currentUserId = computed(() => user.value?.id ?? null)

  async function login(request: LoginRequest) {
    isAuthenticating.value = true
    try {
      const result = await loginUser(request)
      user.value = result.user
      tokens.value = result.tokens
    } finally {
      isAuthenticating.value = false
    }
  }

  async function logout() {
    try {
      await logoutUser()
    } finally {
      user.value = null
      tokens.value = null
    }
  }

  async function refresh() {
    if (!tokens.value?.refreshToken) return
    try {
      const newTokens = await refreshToken(tokens.value.refreshToken)
      tokens.value = newTokens
    } catch {
      user.value = null
      tokens.value = null
    }
  }

  return {
    user,
    tokens,
    isAuthenticating,
    isAuthenticated,
    accessToken,
    currentUserId,
    login,
    logout,
    refresh,
  }
},
{
  persist: true,  // pinia-plugin-persistedstate — persists to localStorage
})
```

### Paginated List Store

```typescript
// features/orders/stores/orderStore.ts
import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { fetchOrdersPage } from '@/features/orders/api'
import type { Order, OrderStatus, PagedResult } from '@/features/orders/types'

interface Filters {
  status: OrderStatus | null
  customerId: string | null
  search: string
}

export const useOrderStore = defineStore('orders', () => {
  const items = ref<Order[]>([])
  const currentPage = ref(1)
  const pageSize = ref(20)
  const totalCount = ref(0)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  const filters = ref<Filters>({
    status: null,
    customerId: null,
    search: '',
  })

  const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value))
  const hasNextPage = computed(() => currentPage.value < totalPages.value)
  const hasPrevPage = computed(() => currentPage.value > 1)

  async function loadPage(page = 1) {
    isLoading.value = true
    error.value = null
    try {
      const result: PagedResult<Order> = await fetchOrdersPage({
        page,
        pageSize: pageSize.value,
        ...filters.value,
      })
      items.value = result.items
      totalCount.value = result.totalCount
      currentPage.value = page
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load orders'
    } finally {
      isLoading.value = false
    }
  }

  function setFilter<K extends keyof Filters>(key: K, value: Filters[K]) {
    filters.value[key] = value
    loadPage(1)  // reset to page 1 when filter changes
  }

  function updateOrderStatus(orderId: string, status: OrderStatus) {
    const order = items.value.find((o) => o.id === orderId)
    if (order) {
      order.status = status
    }
  }

  return {
    items,
    currentPage,
    pageSize,
    totalCount,
    isLoading,
    error,
    filters,
    totalPages,
    hasNextPage,
    hasPrevPage,
    loadPage,
    setFilter,
    updateOrderStatus,
  }
})
```

### Using `storeToRefs` in Components

```vue
<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useProductStore } from '@/features/products/stores/productStore'

const store = useProductStore()

// Destructure reactive state without losing reactivity
const { products, isLoading, error, selectedProduct } = storeToRefs(store)

// Actions can be destructured directly (they are plain functions, not reactive)
const { fetchAll, select, clearError } = store
</script>
```

## Anti-patterns

### Options API store

```typescript
// BAD
export const useProductStore = defineStore('products', {
  state: () => ({ products: [] as Product[] }),
  getters: {
    count: (state) => state.products.length,
  },
  actions: {
    async fetch() { ... }
  }
})

// GOOD — setup function form
export const useProductStore = defineStore('products', () => {
  const products = ref<Product[]>([])
  const count = computed(() => products.value.length)
  async function fetch() { ... }
  return { products, count, fetch }
})
```

### `any`-typed state

```typescript
// BAD
const products = ref<any[]>([])
const response = await fetch('/api/products')
products.value = await response.json()  // unknown shape at runtime

// GOOD
interface Product { id: string; name: string; price: number }
const products = ref<Product[]>([])
products.value = await fetchProducts()  // typed API client
```

### Side effects inside computed

```typescript
// BAD — computed runs silently on access, this causes infinite loops and hidden API calls
const enrichedProducts = computed(async () => {
  const data = await fetchProducts()  // DON'T do this
  return data.map(enrich)
})

// GOOD — async work in actions, computed derives from state
const enrichedProducts = computed(() =>
  products.value.map(enrich)
)
```

### Calling API directly from component instead of store action

```vue
<!-- BAD — bypasses store, no shared state, no loading/error handling -->
<script setup lang="ts">
const products = ref([])
onMounted(async () => {
  const res = await fetch('/api/products')
  products.value = await res.json()
})
</script>

<!-- GOOD -->
<script setup lang="ts">
const store = useProductStore()
onMounted(() => store.fetchAll())
</script>
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| List of domain objects fetched from API | `ref<T[]>` + `fetchAll` action + `isLoading` + `error` |
| Single selected item | `ref<string \| null>` for ID, `computed` to derive the object |
| Form in progress | Local `ref` in component; move to store only if navigation must preserve it |
| Auth tokens / user session | Dedicated `authStore` with `persist: true` |
| Pagination | `currentPage`, `pageSize`, `totalCount` refs; `totalPages` computed |
| Optimistic updates | Mutate local state immediately, roll back in the `catch` block |
| SignalR server-push updates | Expose a mutation action (e.g. `applyServerUpdate`) called from a composable |
| Cross-store communication | Import the other store inside the setup function — stores compose like composables |

## Execution

Run `validate_pinia_stores` via vue-mcp to check existing stores for Options API usage and missing returns. Check `features/{name}/` for an existing `api.ts` before generating the store.

### `/pinia-store <StoreName> [entity description]`

1. Determine the file path: `features/{name}/stores/{name}Store.ts` (or `stores/{name}Store.ts` for app-level stores).
2. Generate the store with:
   - Typed `ref<T[]>` or `ref<T | null>` state
   - `isLoading`, `isSaving`, `error` refs
   - `computed` getters derived from state
   - `fetchAll` (or appropriate CRUD) async actions that call `api.ts`
   - Error handling with `try/catch/finally`
   - A `return {}` exporting everything
3. If a `types.ts` file does not exist for the feature, generate a stub `interface` for the entity.
4. After writing, run `validate_pinia_stores` to verify the generated store.

$ARGUMENTS
