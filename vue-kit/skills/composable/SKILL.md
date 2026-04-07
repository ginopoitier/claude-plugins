---
name: composable
description: >
  Scaffold a Vue 3 composable (useXxx) with reactive state, typed return, and lifecycle cleanup.
  Load this skill when: "composable", "use function", "useXxx", "create composable",
  "shared logic", "vue hook", "custom hook", "/composable".
user-invocable: true
argument-hint: "<ComposableName> [description of what it does]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# Composable — Vue 3 Composition Function Scaffold

## Core Principles

1. **Name starts with `use`** — Every composable is named `useXxx` and lives in a file named `useXxx.ts`. This is a Vue convention enforced by ESLint rules.
2. **Always return a plain object** — Return `{ foo, bar, doSomething }` — never return the reactive proxy itself. Destructuring must work without losing reactivity (i.e. return `ref`s and computed values, not raw values).
3. **Clean up every side effect** — Every `watch`, `watchEffect`, `addEventListener`, `setInterval`, or external subscription registered inside a composable must be torn down in `onUnmounted` (or via the `stop` handle from `watchEffect`).
4. **Accept options as a plain object parameter** — When a composable needs configuration, accept a single typed `options` object with sensible defaults via destructuring. This is more readable than positional arguments.
5. **Composables are not stores** — If state needs to be shared across unrelated component trees, put it in a Pinia store. Composables manage per-component or per-instance state.

## Composable Categories

| Category | Pattern |
|----------|---------|
| Local reactive state wrapper | `ref` / `reactive` returned directly |
| DOM side effect | `onMounted` attach, `onUnmounted` detach |
| Async data fetch | `ref` for data/loading/error + `execute()` function |
| External subscription (WebSocket, EventBus) | `onMounted` subscribe, `onUnmounted` unsubscribe |
| Derived / computed logic | `computed` based on passed-in reactive source |
| Cross-component communication | Prefer Pinia store instead |

## Patterns

### Basic Reactive State Wrapper

```typescript
// composables/useCounter.ts
import { ref, computed } from 'vue'

interface UseCounterOptions {
  initial?: number
  min?: number
  max?: number
}

export function useCounter(options: UseCounterOptions = {}) {
  const { initial = 0, min = -Infinity, max = Infinity } = options

  const count = ref(initial)
  const isAtMin = computed(() => count.value <= min)
  const isAtMax = computed(() => count.value >= max)

  function increment(step = 1) {
    count.value = Math.min(count.value + step, max)
  }

  function decrement(step = 1) {
    count.value = Math.max(count.value - step, min)
  }

  function reset() {
    count.value = initial
  }

  return { count, isAtMin, isAtMax, increment, decrement, reset }
}
```

### Async Data Fetch Composable

```typescript
// composables/useAsync.ts
import { ref, type Ref } from 'vue'

interface UseAsyncReturn<T> {
  data: Ref<T | null>
  isLoading: Ref<boolean>
  error: Ref<Error | null>
  execute: () => Promise<void>
}

export function useAsync<T>(fn: () => Promise<T>): UseAsyncReturn<T> {
  const data = ref<T | null>(null) as Ref<T | null>
  const isLoading = ref(false)
  const error = ref<Error | null>(null)

  async function execute() {
    isLoading.value = true
    error.value = null
    try {
      data.value = await fn()
    } catch (err) {
      error.value = err instanceof Error ? err : new Error(String(err))
    } finally {
      isLoading.value = false
    }
  }

  return { data, isLoading, error, execute }
}
```

Usage:

```vue
<script setup lang="ts">
import { onMounted } from 'vue'
import { useAsync } from '@/composables/useAsync'
import { fetchProduct } from '@/features/products/api'

const { data: product, isLoading, error, execute } = useAsync(() => fetchProduct('123'))
onMounted(execute)
</script>
```

### DOM Side Effect with Cleanup

```typescript
// composables/useEventListener.ts
import { onMounted, onUnmounted } from 'vue'

export function useEventListener<K extends keyof WindowEventMap>(
  type: K,
  handler: (event: WindowEventMap[K]) => void,
  options?: AddEventListenerOptions,
) {
  onMounted(() => {
    window.addEventListener(type, handler, options)
  })

  onUnmounted(() => {
    window.removeEventListener(type, handler, options)
  })
}
```

### Watcher with Cleanup

```typescript
// composables/useDebounce.ts
import { ref, watch, type Ref } from 'vue'

export function useDebounce<T>(source: Ref<T>, delay = 300): Ref<T> {
  const debounced = ref<T>(source.value) as Ref<T>
  let timer: ReturnType<typeof setTimeout>

  watch(source, (value) => {
    clearTimeout(timer)
    timer = setTimeout(() => {
      debounced.value = value
    }, delay)
  })

  // cleanup is handled by Vue — watch is automatically stopped when the
  // component that called this composable is unmounted

  return debounced
}
```

### Intersection Observer (cleanup required)

```typescript
// composables/useIntersectionObserver.ts
import { ref, onUnmounted, type Ref } from 'vue'

interface UseIntersectionObserverOptions {
  threshold?: number | number[]
  rootMargin?: string
  once?: boolean
}

export function useIntersectionObserver(
  target: Ref<HTMLElement | null>,
  options: UseIntersectionObserverOptions = {},
) {
  const { threshold = 0.1, rootMargin = '0px', once = false } = options

  const isVisible = ref(false)
  let observer: IntersectionObserver | null = null

  function observe() {
    if (!target.value) return

    observer = new IntersectionObserver(
      ([entry]) => {
        isVisible.value = entry.isIntersecting
        if (entry.isIntersecting && once) {
          observer?.disconnect()
          observer = null
        }
      },
      { threshold, rootMargin },
    )

    observer.observe(target.value)
  }

  onUnmounted(() => {
    observer?.disconnect()
    observer = null
  })

  return { isVisible, observe }
}
```

Usage in component:

```vue
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useIntersectionObserver } from '@/composables/useIntersectionObserver'

const cardRef = ref<HTMLElement | null>(null)
const { isVisible, observe } = useIntersectionObserver(cardRef, { once: true })

onMounted(observe)
</script>

<template>
  <div
    ref="cardRef"
    class="transition-opacity duration-500"
    :class="isVisible ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4'"
  >
    content
  </div>
</template>
```

### Clipboard API Composable

```typescript
// composables/useClipboard.ts
import { ref } from 'vue'

interface UseClipboardReturn {
  isCopied: import('vue').Ref<boolean>
  copy: (text: string) => Promise<void>
}

export function useClipboard(resetDelay = 2000): UseClipboardReturn {
  const isCopied = ref(false)
  let timer: ReturnType<typeof setTimeout>

  async function copy(text: string) {
    try {
      await navigator.clipboard.writeText(text)
      isCopied.value = true
      clearTimeout(timer)
      timer = setTimeout(() => {
        isCopied.value = false
      }, resetDelay)
    } catch {
      isCopied.value = false
    }
  }

  return { isCopied, copy }
}
```

### Composable That Reads a Pinia Store

```typescript
// features/products/composables/useProductSearch.ts
import { ref, computed, watch } from 'vue'
import { useProductStore } from '@/features/products/stores/productStore'
import { useDebounce } from '@/composables/useDebounce'

export function useProductSearch() {
  const store = useProductStore()
  const query = ref('')
  const debouncedQuery = useDebounce(query, 350)

  const results = computed(() =>
    debouncedQuery.value.trim().length < 2
      ? []
      : store.products.filter((p) =>
          p.name.toLowerCase().includes(debouncedQuery.value.toLowerCase()),
        ),
  )

  watch(debouncedQuery, (q) => {
    if (q.length >= 2) {
      store.searchProducts(q)
    }
  })

  return { query, results, isSearching: store.isSearching }
}
```

## Anti-patterns

### Returning raw values (reactivity lost on destructure)

```typescript
// BAD — caller does const { count } = useCounter() and count is a plain number
export function useCounter() {
  const state = reactive({ count: 0 })
  return { count: state.count }  // primitive copy, not reactive
}

// GOOD — return refs
export function useCounter() {
  const count = ref(0)
  return { count }  // ref, stays reactive when destructured
}
```

### Mutating props inside a composable

```typescript
// BAD — composables must not mutate their arguments
export function useSort<T>(items: T[]) {
  items.sort(...)  // mutates the caller's array
}

// GOOD — work on a copy or return a computed
export function useSort<T>(items: Ref<T[]>, compareFn: (a: T, b: T) => number) {
  const sorted = computed(() => [...items.value].sort(compareFn))
  return { sorted }
}
```

### Forgotten cleanup for intervals and subscriptions

```typescript
// BAD — interval runs forever after unmount
export function usePolling(fn: () => void, intervalMs: number) {
  onMounted(() => {
    setInterval(fn, intervalMs)  // handle never saved, never cleared
  })
}

// GOOD
export function usePolling(fn: () => void, intervalMs: number) {
  let handle: ReturnType<typeof setInterval>

  onMounted(() => {
    handle = setInterval(fn, intervalMs)
  })

  onUnmounted(() => {
    clearInterval(handle)
  })
}
```

### Using composable outside `<script setup>` or `setup()`

```typescript
// BAD — onMounted/onUnmounted have no active component instance
setTimeout(() => {
  const { count } = useCounter()  // lifecycle hooks will silently do nothing
}, 1000)
```

## Decision Guide

| Scenario | Use |
|----------|-----|
| State shared across many components | Pinia store, not a composable |
| Reusable DOM interaction (resize, scroll, click outside) | Composable with lifecycle cleanup |
| Async fetch pattern | `useAsync` composable or store action |
| Debouncing a reactive value | `useDebounce` composable |
| Formatting / pure transformation logic | Plain utility function in `utils/`, not a composable |
| SignalR event listeners | `use{Name}Hub.ts` composable (see `/signalr-hub`) |
| Feature-scoped logic using its Pinia store | `features/{name}/composables/` directory |
| Cross-feature shared logic | `composables/` at src root |

## Execution

Run `get_vue_composables` via vue-mcp to inspect existing composable exports. Match the naming and structure conventions already present.

### `/composable <ComposableName> [description]`

1. Determine the correct file path:
   - Feature-scoped → `features/{feature}/composables/use{Name}.ts`
   - App-wide shared → `composables/use{Name}.ts`
2. Generate the composable with:
   - `UseXxxOptions` interface if configurable
   - `UseXxxReturn` interface for the return type (or inline type)
   - Typed `ref` / `computed` values
   - `onMounted` / `onUnmounted` hooks when side effects are needed
   - A `return {}` of only refs and functions — never the reactive proxy
3. Write a brief JSDoc comment above the function describing parameters and return values.
4. After writing, run `get_vue_type_errors` to validate.

$ARGUMENTS
