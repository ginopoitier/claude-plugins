---
name: api-client
description: >
  Scaffold a typed API client module at features/{name}/api.ts with request/response interfaces.
  Load this skill when: "api client", "api layer", "api.ts", "typed api", "feature api",
  "fetch wrapper", "axios", "http client", "endpoint", "/api-client".
user-invocable: true
argument-hint: "<FeatureName> [endpoints...]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# API Client — Typed Feature API Layer

## Core Principles

1. **Every function is fully typed** — No `Promise<any>`, no `unknown` that isn't narrowed, no untyped `response.json()`. Request parameters and response payloads have named TypeScript interfaces.
2. **API functions live in `features/{name}/api.ts`** — They are called exclusively from store actions, never from components or composables directly.
3. **HTTP concerns stay in the API layer** — Status code checking, error mapping, and header management live here. The store only sees a `Promise<T>` that either resolves or throws a typed `ApiError`.
4. **One shared axios instance** — Create a single `apiClient` instance in `lib/apiClient.ts` with base URL, default headers, and request/response interceptors. Feature `api.ts` files import from it — they never create `axios.create()` themselves.
5. **Error responses are typed** — Catch HTTP errors and throw a consistent `ApiError` so callers can pattern-match on `error.statusCode`.

## Patterns

### Shared Axios Instance

```typescript
// lib/apiClient.ts
import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'

export interface ApiError {
  message: string
  statusCode: number
  errors?: Record<string, string[]>  // validation errors from .NET ProblemDetails
}

function isApiError(value: unknown): value is ApiError {
  return (
    typeof value === 'object' &&
    value !== null &&
    'message' in value &&
    'statusCode' in value
  )
}

export const apiClient = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
  timeout: 15_000,
})

// Attach auth token from Pinia auth store
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  // Avoid circular import — read directly from localStorage or pass token separately
  const token = localStorage.getItem('auth_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Map HTTP errors to ApiError
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    const data = error.response?.data
    const apiError: ApiError = isApiError(data)
      ? data
      : {
          message: error.message,
          statusCode: error.response?.status ?? 0,
        }
    return Promise.reject(apiError)
  },
)
```

### Feature API Module

```typescript
// features/products/api.ts
import { apiClient } from '@/lib/apiClient'

// ── Types ────────────────────────────────────────────────────────────────
export interface Product {
  id: string
  name: string
  description: string
  price: number
  stock: number
  categoryId: string
  imageUrl: string | null
  createdAt: string
  updatedAt: string
}

export interface CreateProductRequest {
  name: string
  description: string
  price: number
  stock: number
  categoryId: string
  imageUrl?: string
}

export interface UpdateProductRequest {
  name?: string
  description?: string
  price?: number
  stock?: number
  imageUrl?: string | null
}

export interface ProductListResponse {
  items: Product[]
  totalCount: number
  page: number
  pageSize: number
}

export interface ProductListParams {
  page?: number
  pageSize?: number
  categoryId?: string
  search?: string
  inStock?: boolean
}

// ── API Functions ────────────────────────────────────────────────────────
export async function fetchProducts(params?: ProductListParams): Promise<ProductListResponse> {
  const { data } = await apiClient.get<ProductListResponse>('/products', { params })
  return data
}

export async function fetchProductById(id: string): Promise<Product> {
  const { data } = await apiClient.get<Product>(`/products/${id}`)
  return data
}

export async function createProduct(request: CreateProductRequest): Promise<Product> {
  const { data } = await apiClient.post<Product>('/products', request)
  return data
}

export async function updateProduct(id: string, request: UpdateProductRequest): Promise<Product> {
  const { data } = await apiClient.put<Product>(`/products/${id}`, request)
  return data
}

export async function deleteProduct(id: string): Promise<void> {
  await apiClient.delete(`/products/${id}`)
}

export async function uploadProductImage(id: string, file: File): Promise<{ imageUrl: string }> {
  const form = new FormData()
  form.append('file', file)
  const { data } = await apiClient.post<{ imageUrl: string }>(
    `/products/${id}/image`,
    form,
    { headers: { 'Content-Type': 'multipart/form-data' } },
  )
  return data
}
```

### Fetch-based API (no axios)

```typescript
// features/orders/api.ts
// Use when the project doesn't have axios as a dependency

import type { Order, CreateOrderRequest, OrderStatus } from './types'

const BASE = '/api/orders'

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  const token = localStorage.getItem('auth_token')
  const response = await fetch(url, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  })

  if (!response.ok) {
    let body: unknown
    try { body = await response.json() } catch { body = null }
    const message =
      typeof body === 'object' && body !== null && 'title' in body
        ? (body as { title: string }).title
        : response.statusText
    throw { message, statusCode: response.status }
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export async function fetchOrders(params?: { status?: OrderStatus; page?: number }): Promise<Order[]> {
  const qs = params ? '?' + new URLSearchParams(params as Record<string, string>).toString() : ''
  return request<Order[]>(`${BASE}${qs}`)
}

export async function fetchOrderById(id: string): Promise<Order> {
  return request<Order>(`${BASE}/${id}`)
}

export async function createOrder(payload: CreateOrderRequest): Promise<Order> {
  return request<Order>(BASE, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export async function cancelOrder(id: string): Promise<void> {
  return request<void>(`${BASE}/${id}/cancel`, { method: 'POST' })
}
```

### Handling .NET ProblemDetails validation errors

```typescript
// lib/apiErrors.ts
import type { ApiError } from './apiClient'

export function isApiError(err: unknown): err is ApiError {
  return typeof err === 'object' && err !== null && 'statusCode' in err
}

export function getValidationErrors(err: unknown): Record<string, string[]> {
  if (isApiError(err) && err.errors) return err.errors
  return {}
}

export function getErrorMessage(err: unknown, fallback = 'An unexpected error occurred'): string {
  if (isApiError(err)) return err.message
  if (err instanceof Error) return err.message
  return fallback
}
```

Store usage:

```typescript
// in productStore.ts action
import { getErrorMessage } from '@/lib/apiErrors'

async function create(request: CreateProductRequest) {
  isSaving.value = true
  try {
    const created = await createProduct(request)
    products.value.push(created)
    return created
  } catch (err) {
    error.value = getErrorMessage(err)
    throw err
  } finally {
    isSaving.value = false
  }
}
```

### Types-only barrel (when API types are shared with the store)

```typescript
// features/products/types.ts
// Re-export API types that the store and components also need
export type {
  Product,
  CreateProductRequest,
  UpdateProductRequest,
  ProductListParams,
} from './api'
```

## Anti-patterns

### `Promise<any>` return type

```typescript
// BAD — no compile-time safety, no IDE completion
export async function fetchProducts(): Promise<any> {
  const res = await fetch('/api/products')
  return res.json()
}

// GOOD
export async function fetchProducts(): Promise<Product[]> {
  const { data } = await apiClient.get<Product[]>('/products')
  return data
}
```

### API calls directly in components

```vue
<!-- BAD — duplicated logic, no shared state, no centralized error handling -->
<script setup lang="ts">
const products = ref([])
onMounted(async () => {
  const res = await fetch('/api/products')
  products.value = await res.json()
})
</script>

<!-- GOOD — component calls store action; store calls api.ts -->
<script setup lang="ts">
const store = useProductStore()
onMounted(() => store.fetchAll())
</script>
```

### Untyped `response.json()`

```typescript
// BAD
const data = await response.json()         // type is `any`
const products = data.items                // no type checking

// GOOD
const data = await response.json() as ProductListResponse
const products = data.items                // typed as Product[]
```

### Leaking HTTP status codes into the store

```typescript
// BAD — store now knows about HTTP
async function fetchAll() {
  const res = await fetch('/api/products')
  if (res.status === 404) {
    products.value = []
  } else if (res.status === 401) {
    router.push('/login')
  }
}

// GOOD — interceptor/wrapper handles HTTP; store sees a typed ApiError
async function fetchAll() {
  try {
    products.value = await fetchProducts()
  } catch (err) {
    if (isApiError(err) && err.statusCode === 401) {
      authStore.logout()
    }
    error.value = getErrorMessage(err)
  }
}
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Standard CRUD endpoints | `fetchAll`, `fetchById`, `create`, `update`, `delete` functions |
| Paginated list | Accept `{ page, pageSize, ...filters }` parameter, return `PagedResult<T>` |
| File upload | Use `FormData` with `multipart/form-data` content type override |
| 204 No Content response | Return `Promise<void>` |
| .NET ProblemDetails error shape | Use `ApiError` interface with `errors: Record<string, string[]>` |
| Auth header injection | Axios request interceptor in `lib/apiClient.ts` |
| Token refresh on 401 | Axios response interceptor with queue-based retry |
| Types shared between api.ts and store | Re-export from `features/{name}/types.ts` barrel |
| Mocking in tests | `vi.mock('@/features/products/api')` — mock the module, not axios |

## Execution

Run `find_missing_api_types` via vue-mcp to identify existing `api.ts` files with `Promise<any>` or missing interfaces. Check whether `lib/apiClient.ts` already exists before generating it.

### `/api-client <FeatureName> [endpoints...]`

1. If `lib/apiClient.ts` does not exist, generate it with the shared axios instance, `ApiError` interface, and request/response interceptors.
2. Generate `features/{name}/api.ts` with:
   - One TypeScript `interface` per request payload and response shape
   - One `async function` per endpoint, typed with the matching interfaces
   - No inline `axios.create()` — import the shared `apiClient`
3. If endpoints are provided as arguments, generate stubs for each named endpoint.
4. After writing, run `find_missing_api_types` to confirm no `Promise<any>` remains.

$ARGUMENTS
