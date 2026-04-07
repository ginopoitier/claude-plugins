---
name: performance
description: >
  Vue 3 performance patterns — lazy routes, defineAsyncComponent, v-memo, shallowRef, and bundle analysis.
  Load this skill when: "performance", "bundle size", "lazy load", "code split", "optimize",
  "shallowRef", "v-memo", "virtual scroll", "bundle analysis", "defineAsyncComponent", "/performance".
user-invocable: true
argument-hint: "[area: routing|components|lists|bundle]"
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
---

# Performance — Vue 3 Optimization Patterns

## Core Principles

1. **Split at route boundaries first** — Dynamic imports on every route record give the highest return on investment with the least code change. Do this before any other optimization.
2. **Measure before optimizing** — Run `vite build --report` or open the Rollup visualizer to identify actual large chunks. Never guess at bundle composition.
3. **`shallowRef` for large external datasets** — When a ref holds a large array that Vue doesn't need to deeply track (server-fetched rows, file lists), `shallowRef` avoids the cost of recursive proxy wrapping.
4. **`v-memo` only for measurably slow list items** — It has a correctness cost (stale renders if dependencies are wrong). Benchmark first.
5. **`defineAsyncComponent` for conditionally rendered heavy components** — Modals, rich text editors, charts, and data tables rendered behind a `v-if` are ideal candidates.
6. **Avoid `v-if` + `v-for` on the same element** — Always wrap with a container element or use `<template>`.

## Performance Budget Targets

| Metric | Target |
|--------|--------|
| Initial JS bundle (gzipped) | < 150 KB |
| Largest route chunk | < 100 KB gzipped |
| LCP (Largest Contentful Paint) | < 2.5 s |
| Time to Interactive | < 3.5 s |
| List render (1000 rows, no virtual) | < 100 ms |

## Patterns

### Lazy Route Loading (most impactful)

```typescript
// router/index.ts
// ALL route components use dynamic import — no exceptions
const routes: RouteRecordRaw[] = [
  {
    path: '/dashboard',
    component: () => import('@/features/dashboard/pages/DashboardPage.vue'),
  },
  {
    path: '/products',
    // Lazy parent layout
    component: () => import('@/layouts/AppShell.vue'),
    children: [
      {
        path: '',
        // Named chunk for easier bundle identification
        component: () => import(
          /* webpackChunkName: "products" */
          '@/features/products/pages/ProductListPage.vue'
        ),
      },
      {
        path: ':id',
        component: () => import('@/features/products/pages/ProductDetailPage.vue'),
      },
    ],
  },
]
```

### `defineAsyncComponent` for Conditionally Rendered Components

```typescript
// features/products/pages/ProductDetailPage.vue
import { defineAsyncComponent, ref } from 'vue'

// Heavy chart library only loads when the user opens the analytics tab
const SalesChart = defineAsyncComponent({
  loader: () => import('@/features/analytics/components/SalesChart.vue'),
  loadingComponent: () => import('@/components/ui/LoadingSpinner.vue'),
  errorComponent: () => import('@/components/ui/ComponentLoadError.vue'),
  delay: 200,      // show spinner only if load takes > 200ms
  timeout: 10_000, // show error component if not loaded in 10s
})

const showChart = ref(false)
```

```vue
<template>
  <button @click="showChart = true">View analytics</button>
  <!-- SalesChart.vue (+ chart library) loads only when showChart becomes true -->
  <SalesChart v-if="showChart" :product-id="productId" />
</template>
```

### `shallowRef` for Large Server-Fetched Arrays

```typescript
// features/reports/stores/reportStore.ts
import { shallowRef, ref, computed } from 'vue'
import { defineStore } from 'pinia'
import type { ReportRow } from '@/features/reports/types'

export const useReportStore = defineStore('reports', () => {
  // shallowRef: Vue only tracks the ref itself, not the nested objects
  // Correct for server data that is replaced wholesale after each fetch
  const rows = shallowRef<ReportRow[]>([])
  const isLoading = ref(false)

  // Derived value is still reactive — computed re-runs when rows.value is reassigned
  const totalRevenue = computed(() =>
    rows.value.reduce((sum, r) => sum + r.revenue, 0),
  )

  async function loadReport(params: ReportParams) {
    isLoading.value = true
    try {
      // Reassign the ref (not push/splice) to trigger reactivity
      rows.value = await fetchReport(params)
    } finally {
      isLoading.value = false
    }
  }

  return { rows, isLoading, totalRevenue, loadReport }
})
```

When to use `shallowRef` vs `ref`:

| Use `ref` | Use `shallowRef` |
|-----------|-----------------|
| Small arrays (< 100 items) | Large server-fetched arrays (1000+ rows) |
| Objects where nested mutations drive UI | Data that is always replaced wholesale |
| Form state | Read-only grid/table data |

### `v-memo` for Expensive List Items

```vue
<!-- Only use when profiling confirms the list renders are slow -->
<template>
  <ul>
    <li
      v-for="product in products"
      :key="product.id"
      v-memo="[product.id, product.name, product.price, product.stock, selectedId === product.id]"
    >
      <!-- This subtree skips diffing if the memo dependencies haven't changed -->
      <ProductCard :product="product" :is-selected="selectedId === product.id" />
    </li>
  </ul>
</template>
```

`v-memo` dependencies must include every value the subtree reads. Missing a dependency causes stale renders — a harder bug than a slow render.

### Virtual Scrolling for Very Large Lists

Use `@tanstack/vue-virtual` when lists exceed ~500 rows and each row has non-trivial DOM:

```vue
<!-- features/orders/components/OrderVirtualList.vue -->
<script setup lang="ts">
import { ref, computed } from 'vue'
import { useVirtualizer } from '@tanstack/vue-virtual'
import type { Order } from '@/features/orders/types'

interface Props {
  orders: Order[]
}
const props = defineProps<Props>()

const parentRef = ref<HTMLElement | null>(null)

const virtualizer = useVirtualizer(
  computed(() => ({
    count: props.orders.length,
    getScrollElement: () => parentRef.value,
    estimateSize: () => 72,    // estimated row height in px
    overscan: 5,               // render 5 extra rows above/below viewport
  })),
)

const items = computed(() => virtualizer.value.getVirtualItems())
const totalSize = computed(() => virtualizer.value.getTotalSize())
</script>

<template>
  <div ref="parentRef" class="h-[600px] overflow-y-auto">
    <div :style="{ height: `${totalSize}px`, position: 'relative' }">
      <div
        v-for="item in items"
        :key="item.key"
        :style="{
          position: 'absolute',
          top: 0,
          left: 0,
          width: '100%',
          height: `${item.size}px`,
          transform: `translateY(${item.start}px)`,
        }"
      >
        <OrderRow :order="orders[item.index]" />
      </div>
    </div>
  </div>
</template>
```

### Bundle Analysis with Vite

```typescript
// vite.config.ts
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { visualizer } from 'rollup-plugin-visualizer'

export default defineConfig({
  plugins: [
    vue(),
    // Only active when ANALYZE=true env var is set
    process.env.ANALYZE === 'true' &&
      visualizer({
        open: true,
        filename: 'dist/bundle-report.html',
        gzipSize: true,
        brotliSize: true,
      }),
  ].filter(Boolean),
  build: {
    rollupOptions: {
      output: {
        // Manual chunks: keep large dependencies in their own files
        // so they can be cached independently
        manualChunks: {
          'vue-vendor': ['vue', 'vue-router', 'pinia'],
          'signalr': ['@microsoft/signalr'],
        },
      },
    },
    // Warn when any chunk exceeds 250 KB gzipped
    chunkSizeWarningLimit: 250,
  },
})
```

Run analysis:

```bash
ANALYZE=true vite build
```

### `computed` Caching vs Method

```typescript
// In a component or store — computed is cached until dependencies change
const sortedProducts = computed(() =>
  [...products.value].sort((a, b) => a.name.localeCompare(b.name)),
)

// A method recalculates on every render — correct for volatile inputs
// but not for this case
function getSortedProducts() {
  return [...products.value].sort((a, b) => a.name.localeCompare(b.name))
}
```

### Avoiding Expensive Watchers

```typescript
// BAD — deep watch on a large array triggers on any nested mutation
watch(products, doSomethingExpensive, { deep: true })

// GOOD — watch a derived scalar instead
watch(
  () => products.value.map((p) => p.id).join(','),
  doSomethingExpensive,
)

// OR — watch a specific property that actually drives the side effect
watch(
  () => products.value.length,
  doSomethingExpensive,
)
```

### Lazy Heavy Imports

```typescript
// BAD — date-fns imported at module load time, included in main chunk
import { format } from 'date-fns'

// GOOD — imported only when the function is first called
async function formatDate(date: Date) {
  const { format } = await import('date-fns')
  return format(date, 'PPP')
}

// OR — import in the component that needs it (Vite handles deduplication)
const DatePicker = defineAsyncComponent(() => import('@/components/ui/DatePicker.vue'))
```

## Anti-patterns

### `v-if` and `v-for` on the same element

```vue
<!-- BAD — v-if re-evaluates on every item during the v-for loop -->
<li v-for="product in products" v-if="product.inStock" :key="product.id">
  {{ product.name }}
</li>

<!-- GOOD — filter first, or wrap with <template> -->
<template v-for="product in products" :key="product.id">
  <li v-if="product.inStock">{{ product.name }}</li>
</template>

<!-- BETTER — filter in a computed -->
<li v-for="product in inStockProducts" :key="product.id">{{ product.name }}</li>
```

### Importing heavy libraries eagerly in the main bundle

```typescript
// BAD — Chart.js (600 KB) ends up in the main bundle
import { Chart } from 'chart.js'

// GOOD — dynamic import in the component that uses it
const SalesChart = defineAsyncComponent(() => import('./SalesChart.vue'))
// SalesChart.vue imports Chart.js — Vite bundles it into that chunk only
```

### `reactive` for large read-only datasets

```typescript
// BAD — Vue wraps every nested object recursively
const rows = reactive<ReportRow[]>([])  // 50,000 rows → 50,000 Proxy objects

// GOOD
const rows = shallowRef<ReportRow[]>([])
```

### Synchronous heavy computation in template expressions

```vue
<!-- BAD — sort runs on every render -->
<li v-for="p in products.sort((a, b) => a.name.localeCompare(b.name))" :key="p.id">

<!-- GOOD — computed caches the sorted array -->
<li v-for="p in sortedProducts" :key="p.id">
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Initial page load is slow | Add lazy routes (`() => import(...)`) to every route record |
| Large modal / editor loads even when not opened | `defineAsyncComponent` |
| List of 500+ rows is slow to scroll | Virtual scrolling with `@tanstack/vue-virtual` |
| Store holds 1000+ objects from API | Replace `ref<T[]>` with `shallowRef<T[]>` |
| List re-renders too often | Profile first; then consider `v-memo` with correct deps |
| Unknown bundle composition | `ANALYZE=true vite build` → open visualizer |
| Third-party library is too large | Check for lighter alternative; or dynamic import |
| Deep watcher is slow | Replace with computed scalar watch or targeted property watch |
| Repeated expensive derivation in template | Move to `computed` in `<script setup>` |

## Execution

Run `get_vue_project_structure` via vue-mcp to inspect `vite.config.ts` and route definitions. Check for routes that use static `component:` imports rather than dynamic ones.

### `/performance [area]`

- `routing` — Convert all static route `component:` imports to dynamic `() => import(...)`. Verify no route is already lazy.
- `components` — Find components rendered behind `v-if` that are large; wrap in `defineAsyncComponent`.
- `lists` — Identify large list renders; propose `shallowRef`, `v-memo`, or virtual scrolling depending on list size.
- `bundle` — Add `rollup-plugin-visualizer` to `vite.config.ts` and generate a `manualChunks` strategy for large vendor dependencies found in the project.
- (default) — Audit all four areas and apply the highest-impact changes first.

$ARGUMENTS
