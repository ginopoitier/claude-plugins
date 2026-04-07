---
name: error-handling
description: >
  Global error handler, error boundary component, and typed API error patterns for Vue 3.
  Load this skill when: "error handling", "error boundary", "global error", "app error",
  "error display", "api error", "typed error", "errorHandler", "/error-handling".
user-invocable: true
argument-hint: "[scope: global|component|api]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# Error Handling — Global Handler, Boundaries, and Typed API Errors

## Core Principles

1. **Never swallow errors silently** — Every `catch` block either re-throws, sets a reactive error ref, or dispatches a notification. A `catch {}` with no body is always a bug.
2. **Never show raw error messages to users** — Stack traces, SQL messages, and internal exception text must never reach the UI. Map errors to user-facing messages at the API boundary.
3. **`app.config.errorHandler` is the last resort** — It catches errors that escape component boundaries (uncaught promise rejections in `<script setup>`, errors in watchers). It should log to a monitoring service and display a generic toast.
4. **The API layer owns HTTP error mapping** — The axios interceptor (or fetch wrapper) in `lib/apiClient.ts` translates HTTP error responses into a typed `ApiError`. Components and stores never inspect `error.response.status`.
5. **Error state is reactive** — Use `ref<string | null>` for error messages in stores and composables. Components bind to it and render error UI reactively.

## Error Architecture

```
Browser Error
  │
  ├─ Component render / lifecycle error
  │    └─ app.config.errorHandler (catches + logs)
  │         └─ useNotifications().error('...')  → toast
  │
  ├─ Unhandled promise (not in Vue lifecycle)
  │    └─ window.onunhandledrejection
  │         └─ app.config.errorHandler fallback
  │
  └─ API call error
       └─ axios interceptor → ApiError
            └─ store action catch → error.value = message
                 └─ component renders <AsyncBoundary> with :error
```

## Patterns

### Global Error Handler Registration

```typescript
// main.ts
import { createApp } from 'vue'
import App from './App.vue'
import { router } from './router'
import { createPinia } from 'pinia'
import { registerErrorHandlers } from './lib/errorHandlers'

const app = createApp(App)
app.use(createPinia())
app.use(router)

registerErrorHandlers(app)

app.mount('#app')
```

```typescript
// lib/errorHandlers.ts
import { type App } from 'vue'

interface ErrorWithMessage {
  message: string
}

function isErrorWithMessage(value: unknown): value is ErrorWithMessage {
  return typeof value === 'object' && value !== null && 'message' in value
}

export function toUserMessage(err: unknown): string {
  if (isErrorWithMessage(err)) return err.message
  return 'An unexpected error occurred. Please try again.'
}

export function registerErrorHandlers(app: App) {
  // Vue component tree errors (render, lifecycle, watchers)
  app.config.errorHandler = (err, instance, info) => {
    console.error('[Vue Error]', info, err)

    // Send to monitoring (e.g. Sentry)
    // captureException(err, { extra: { info, component: instance?.$options.name } })

    // Show a non-intrusive notification — import the store lazily to avoid circular deps
    const { useNotificationStore } = require('@/stores/notificationStore')
    const notifications = useNotificationStore()
    notifications.addError('Something went wrong. Our team has been notified.')
  }

  // Unhandled promise rejections outside the Vue component tree
  window.addEventListener('unhandledrejection', (event) => {
    console.error('[Unhandled Rejection]', event.reason)
    event.preventDefault()
    // Optionally show a toast here too
  })
}
```

### Typed API Error

```typescript
// lib/apiClient.ts
export interface ApiError {
  message: string
  statusCode: number
  errors?: Record<string, string[]>  // .NET ProblemDetails validation errors
  traceId?: string
}

export function isApiError(value: unknown): value is ApiError {
  return (
    typeof value === 'object' &&
    value !== null &&
    'message' in value &&
    'statusCode' in value
  )
}

export function toApiUserMessage(err: unknown): string {
  if (isApiError(err)) {
    // Show the server's message for 4xx (user fixable); generic for 5xx
    if (err.statusCode >= 400 && err.statusCode < 500) return err.message
    return 'A server error occurred. Please try again later.'
  }
  return 'An unexpected error occurred.'
}

export function getValidationErrors(err: unknown): Record<string, string[]> {
  if (isApiError(err) && err.errors) return err.errors
  return {}
}
```

### Error Boundary Component

```vue
<!-- components/ErrorBoundary.vue -->
<script setup lang="ts">
import { ref, onErrorCaptured } from 'vue'

interface Props {
  fallbackMessage?: string
}

const { fallbackMessage = 'Something went wrong.' } = defineProps<Props>()
const emit = defineEmits<{ error: [err: unknown] }>()

const hasError = ref(false)
const errorMessage = ref('')

onErrorCaptured((err, _instance, _info) => {
  hasError.value = true
  errorMessage.value = fallbackMessage
  emit('error', err)
  // Return false to stop propagation to the parent boundary / global handler
  return false
})

function reset() {
  hasError.value = false
  errorMessage.value = ''
}
</script>

<template>
  <div
    v-if="hasError"
    class="flex flex-col items-center gap-4 rounded-xl border border-red-200 bg-red-50 p-8 text-center"
  >
    <svg class="h-8 w-8 text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
        d="M12 9v4m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
    </svg>
    <p class="text-sm font-medium text-red-700">{{ errorMessage }}</p>
    <button
      type="button"
      class="rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white hover:bg-red-700"
      @click="reset"
    >
      Try again
    </button>
  </div>
  <slot v-else />
</template>
```

Usage:

```vue
<!-- features/products/pages/ProductListPage.vue -->
<template>
  <main class="p-6">
    <ErrorBoundary fallback-message="Failed to load products. Please refresh the page.">
      <ProductList />
    </ErrorBoundary>
  </main>
</template>
```

### Async State Component (loading + error + empty)

```vue
<!-- components/AsyncBoundary.vue -->
<script setup lang="ts">
import type { ApiError } from '@/lib/apiClient'
import { isApiError } from '@/lib/apiClient'

interface Props {
  loading?: boolean
  error?: ApiError | Error | null
  empty?: boolean
  emptyMessage?: string
}

const {
  loading = false,
  error = null,
  empty = false,
  emptyMessage = 'Nothing here yet.',
} = defineProps<Props>()

const emit = defineEmits<{ retry: [] }>()

function userMessage(err: ApiError | Error | null): string {
  if (!err) return ''
  if (isApiError(err) && err.statusCode >= 400 && err.statusCode < 500) return err.message
  return 'An error occurred. Please try again.'
}
</script>

<template>
  <div>
    <div v-if="loading" class="flex justify-center py-16" aria-busy="true" aria-label="Loading">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-indigo-200 border-t-indigo-600" />
    </div>

    <div
      v-else-if="error"
      role="alert"
      class="flex flex-col items-center gap-3 rounded-xl border border-red-200 bg-red-50 p-8 text-center"
    >
      <p class="text-sm font-medium text-red-700">{{ userMessage(error) }}</p>
      <button
        type="button"
        class="rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white hover:bg-red-700"
        @click="emit('retry')"
      >
        Retry
      </button>
    </div>

    <p
      v-else-if="empty"
      class="py-16 text-center text-sm text-gray-400"
    >
      {{ emptyMessage }}
    </p>

    <slot v-else />
  </div>
</template>
```

### Form Validation Error Display

```vue
<!-- features/products/components/ProductForm.vue -->
<script setup lang="ts">
import { ref, computed } from 'vue'
import { useProductStore } from '@/features/products/stores/productStore'
import { getValidationErrors, isApiError } from '@/lib/apiClient'
import type { CreateProductRequest } from '@/features/products/api'

const store = useProductStore()

const form = ref<CreateProductRequest>({
  name: '',
  description: '',
  price: 0,
  stock: 0,
  categoryId: '',
})

const fieldErrors = ref<Record<string, string[]>>({})
const submitError = ref<string | null>(null)
const isSaving = ref(false)

async function handleSubmit() {
  isSaving.value = true
  fieldErrors.value = {}
  submitError.value = null

  try {
    await store.create(form.value)
    // success: navigate away
  } catch (err) {
    if (isApiError(err) && err.statusCode === 422) {
      fieldErrors.value = getValidationErrors(err)
    } else {
      submitError.value = isApiError(err) ? err.message : 'Failed to save product.'
    }
  } finally {
    isSaving.value = false
  }
}

function fieldError(field: string): string | undefined {
  return fieldErrors.value[field]?.[0]
}
</script>

<template>
  <form class="flex flex-col gap-4" novalidate @submit.prevent="handleSubmit">
    <div
      v-if="submitError"
      role="alert"
      class="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700"
    >
      {{ submitError }}
    </div>

    <div class="flex flex-col gap-1">
      <label for="name" class="text-sm font-medium text-gray-700">Name</label>
      <input
        id="name"
        v-model="form.name"
        type="text"
        class="rounded-lg border px-3 py-2 text-sm focus:outline-none focus:ring-1"
        :class="fieldError('Name') ? 'border-red-400 focus:ring-red-400' : 'border-gray-300 focus:ring-indigo-500'"
      />
      <p v-if="fieldError('Name')" class="text-xs text-red-600">{{ fieldError('Name') }}</p>
    </div>

    <button
      type="submit"
      :disabled="isSaving"
      class="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
    >
      {{ isSaving ? 'Saving…' : 'Save product' }}
    </button>
  </form>
</template>
```

### Notification Store (toast system)

```typescript
// stores/notificationStore.ts
import { ref } from 'vue'
import { defineStore } from 'pinia'

export type NotificationType = 'success' | 'error' | 'warning' | 'info'

export interface Notification {
  id: string
  type: NotificationType
  message: string
  durationMs: number
}

export const useNotificationStore = defineStore('notifications', () => {
  const items = ref<Notification[]>([])

  function add(type: NotificationType, message: string, durationMs = 4000) {
    const id = crypto.randomUUID()
    items.value.push({ id, type, message, durationMs })
    setTimeout(() => remove(id), durationMs)
  }

  function remove(id: string) {
    items.value = items.value.filter((n) => n.id !== id)
  }

  const addSuccess = (message: string) => add('success', message)
  const addError = (message: string) => add('error', message, 6000)
  const addWarning = (message: string) => add('warning', message)
  const addInfo = (message: string) => add('info', message)

  return { items, add, remove, addSuccess, addError, addWarning, addInfo }
})
```

## Anti-patterns

### Swallowing errors silently

```typescript
// BAD — the error disappears; the user sees nothing, the developer sees nothing
try {
  await store.fetchAll()
} catch {
  // intentionally empty
}

// GOOD
try {
  await store.fetchAll()
} catch (err) {
  notifications.addError(toApiUserMessage(err))
}
```

### Showing raw error messages

```vue
<!-- BAD — exposes internal details (stack trace, SQL, internal path) -->
<p class="text-red-500">{{ error.message }}</p>
<!-- where error.message = "Cannot read properties of undefined (reading 'id')" -->

<!-- GOOD — user message mapped at the API boundary -->
<p class="text-red-500">{{ userMessage }}</p>
<!-- where userMessage = "Product not found." (from 404 ApiError.message) -->
```

### `console.log` as the only error handling

```typescript
// BAD — invisible in production, no user feedback
} catch (err) {
  console.log('Error:', err)
}

// GOOD
} catch (err) {
  console.error('[productStore.fetchAll]', err)  // for dev debugging
  error.value = toApiUserMessage(err)             // reactive, drives UI
  // + optional: send to Sentry
}
```

### Putting error boundary logic in every page

```vue
<!-- BAD — duplicated across 15 pages -->
<script setup lang="ts">
const hasError = ref(false)
onErrorCaptured(() => { hasError.value = true; return false })
</script>

<!-- GOOD — use <ErrorBoundary> component wrapping the whole feature -->
<template>
  <ErrorBoundary>
    <ProductList />
  </ErrorBoundary>
</template>
```

## Decision Guide

| Scenario | Pattern |
|----------|---------|
| Uncaught error in component lifecycle | `app.config.errorHandler` → log + toast |
| API returns 422 validation errors | `getValidationErrors(err)` → field-level display |
| API returns 404 or 403 | `isApiError(err)` check, show `err.message` (user-safe) |
| API returns 5xx | Show generic message, log full error to monitoring |
| Async data load (loading/error/empty) | `<AsyncBoundary :loading :error :empty>` |
| Component subtree crash | `<ErrorBoundary>` with `onErrorCaptured` |
| User action failure (form submit) | `ref<string \| null>` in the component, rendered inline |
| App-wide notifications | `useNotificationStore().addError(message)` |
| Error logging to monitoring service | `app.config.errorHandler` — single place |

## Execution

Inspect `src/main.ts` and `lib/apiClient.ts` to understand existing error handling. Check for existing notification/toast infrastructure before generating `notificationStore.ts`.

### `/error-handling [scope]`

- `global` — Generate `lib/errorHandlers.ts` with `registerErrorHandlers(app)`, register in `main.ts`, generate `stores/notificationStore.ts` and a `ToastList.vue` component.
- `component` — Generate `components/ErrorBoundary.vue` (using `onErrorCaptured`) and `components/AsyncBoundary.vue`.
- `api` — Ensure `lib/apiClient.ts` has typed `ApiError`, `isApiError`, `toApiUserMessage`, and `getValidationErrors` helpers. Run `find_missing_api_types` to locate untyped API modules.
- (default / no scope) — Generate all of the above.

$ARGUMENTS
