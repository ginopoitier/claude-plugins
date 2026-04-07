---
name: component
description: >
  Scaffold a Vue 3 SFC with script setup, typed props/emits, and TailwindCSS.
  Load this skill when: "component", "scaffold component", "new component", "create component",
  "vue component", "sfc", "page component", "layout component", "ui component", "/component".
user-invocable: true
argument-hint: "<ComponentName> [--page|--layout|--ui]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# Component — Vue 3 SFC Scaffold

## Core Principles

1. **`<script setup lang="ts">` is the only accepted form** — Options API is banned. The `setup()` function with a `return {}` block is also banned. Always use the macro-based `<script setup>`.
2. **Props and emits are always typed** — `defineProps<{ ... }>()` and `defineEmits<{ ... }>()` generic syntax only. Never use the array form or untyped objects.
3. **TailwindCSS utility classes in the template** — No inline `style` attributes, no `<style scoped>` blocks unless unavoidable (e.g. third-party overrides). Never use `@apply` for one-off combinations.
4. **Separate by concern, not by file type** — A page component orchestrates child components and stores. A UI component receives props and emits events — it never reaches into a store directly.
5. **`defineOptions` for component name** — When a meaningful display name is needed (for Vue DevTools), use `defineOptions({ name: 'MyComponent' })`.

## Component Archetypes

| Flag | What it is | Accesses store? | Emits events? |
|------|-----------|----------------|---------------|
| `--page` | Route-level page | Yes | Rarely |
| `--layout` | Slot-based shell | No | No |
| `--ui` | Reusable presentational | No | Yes |
| (none) | Feature child component | Sometimes | Sometimes |

## Patterns

### UI Component (Presentational)

```vue
<!-- features/products/components/ProductCard.vue -->
<script setup lang="ts">
defineOptions({ name: 'ProductCard' })

interface Props {
  productId: string
  name: string
  price: number
  imageUrl?: string
  inStock?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  inStock: true,
})

const emit = defineEmits<{
  addToCart: [productId: string]
  viewDetails: [productId: string]
}>()

function handleAddToCart() {
  if (!props.inStock) return
  emit('addToCart', props.productId)
}
</script>

<template>
  <article
    class="flex flex-col gap-3 rounded-xl border border-gray-200 bg-white p-4 shadow-sm transition hover:shadow-md"
    :class="{ 'opacity-60': !inStock }"
  >
    <img
      v-if="imageUrl"
      :src="imageUrl"
      :alt="name"
      class="h-48 w-full rounded-lg object-cover"
    />
    <div class="flex flex-col gap-1">
      <h3 class="text-base font-semibold text-gray-900">{{ name }}</h3>
      <p class="text-sm font-medium text-indigo-600">${{ price.toFixed(2) }}</p>
      <span
        v-if="!inStock"
        class="text-xs font-medium text-red-500"
      >Out of stock</span>
    </div>
    <div class="mt-auto flex gap-2">
      <button
        type="button"
        class="flex-1 rounded-lg bg-indigo-600 px-3 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
        :disabled="!inStock"
        @click="handleAddToCart"
      >
        Add to cart
      </button>
      <button
        type="button"
        class="rounded-lg border border-gray-300 px-3 py-2 text-sm font-semibold text-gray-700 hover:bg-gray-50"
        @click="emit('viewDetails', productId)"
      >
        Details
      </button>
    </div>
  </article>
</template>
```

### Page Component

```vue
<!-- features/products/pages/ProductListPage.vue -->
<script setup lang="ts">
import { onMounted } from 'vue'
import { useProductStore } from '@/features/products/stores/productStore'
import ProductCard from '@/features/products/components/ProductCard.vue'
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue'
import EmptyState from '@/components/ui/EmptyState.vue'

defineOptions({ name: 'ProductListPage' })

const store = useProductStore()

onMounted(() => {
  store.fetchProducts()
})

function handleAddToCart(productId: string) {
  store.addToCart(productId)
}

function handleViewDetails(productId: string) {
  // navigate is handled at page level — child doesn't need router
  router.push({ name: 'product-detail', params: { id: productId } })
}
</script>

<template>
  <main class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
    <header class="mb-6 flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">Products</h1>
      <span class="text-sm text-gray-500">{{ store.products.length }} items</span>
    </header>

    <LoadingSpinner v-if="store.isLoading" class="mx-auto mt-16" />

    <EmptyState
      v-else-if="store.products.length === 0"
      message="No products found."
    />

    <ul
      v-else
      class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
    >
      <li v-for="product in store.products" :key="product.id">
        <ProductCard
          v-bind="product"
          :product-id="product.id"
          @add-to-cart="handleAddToCart"
          @view-details="handleViewDetails"
        />
      </li>
    </ul>
  </main>
</template>
```

### Layout Component (Slot-based Shell)

```vue
<!-- layouts/AppShell.vue -->
<script setup lang="ts">
defineOptions({ name: 'AppShell' })

interface Props {
  /** Show the sidebar panel on desktop widths */
  withSidebar?: boolean
}

withDefaults(defineProps<Props>(), {
  withSidebar: false,
})
</script>

<template>
  <div class="flex min-h-screen flex-col bg-gray-50">
    <header class="sticky top-0 z-10 border-b border-gray-200 bg-white shadow-sm">
      <slot name="header" />
    </header>

    <div class="flex flex-1 overflow-hidden">
      <aside
        v-if="withSidebar"
        class="hidden w-64 shrink-0 overflow-y-auto border-r border-gray-200 bg-white lg:block"
      >
        <slot name="sidebar" />
      </aside>

      <main class="flex-1 overflow-y-auto p-6">
        <slot />
      </main>
    </div>

    <footer class="border-t border-gray-200 bg-white py-4 text-center text-xs text-gray-400">
      <slot name="footer" />
    </footer>
  </div>
</template>
```

### Component with v-model

```vue
<!-- components/ui/SearchInput.vue -->
<script setup lang="ts">
defineOptions({ name: 'SearchInput' })

interface Props {
  placeholder?: string
  disabled?: boolean
}

withDefaults(defineProps<Props>(), {
  placeholder: 'Search...',
  disabled: false,
})

const model = defineModel<string>({ default: '' })
</script>

<template>
  <div class="relative">
    <span class="pointer-events-none absolute inset-y-0 left-3 flex items-center text-gray-400">
      <!-- heroicon: magnifying glass -->
      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
        <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-4.35-4.35M17 11A6 6 0 1 1 5 11a6 6 0 0 1 12 0z" />
      </svg>
    </span>
    <input
      v-model="model"
      type="search"
      :placeholder="placeholder"
      :disabled="disabled"
      class="w-full rounded-lg border border-gray-300 bg-white py-2 pl-9 pr-4 text-sm text-gray-900 placeholder-gray-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:cursor-not-allowed disabled:bg-gray-50"
    />
  </div>
</template>
```

### Async Component Wrapper (error + loading states)

```vue
<!-- components/ui/AsyncBoundary.vue -->
<script setup lang="ts">
defineOptions({ name: 'AsyncBoundary' })

interface Props {
  loading: boolean
  error?: Error | null
  empty?: boolean
  emptyMessage?: string
}

withDefaults(defineProps<Props>(), {
  error: null,
  empty: false,
  emptyMessage: 'Nothing to show.',
})

const emit = defineEmits<{
  retry: []
}>()
</script>

<template>
  <div>
    <div v-if="loading" class="flex justify-center py-12">
      <div class="h-8 w-8 animate-spin rounded-full border-4 border-indigo-200 border-t-indigo-600" />
    </div>
    <div
      v-else-if="error"
      class="flex flex-col items-center gap-3 rounded-xl border border-red-200 bg-red-50 p-8 text-center"
    >
      <p class="text-sm font-medium text-red-700">{{ error.message }}</p>
      <button
        type="button"
        class="rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white hover:bg-red-700"
        @click="emit('retry')"
      >
        Retry
      </button>
    </div>
    <div
      v-else-if="empty"
      class="py-12 text-center text-sm text-gray-400"
    >
      {{ emptyMessage }}
    </div>
    <slot v-else />
  </div>
</template>
```

## Anti-patterns

### Options API

```vue
<!-- BAD -->
<script lang="ts">
export default {
  props: ['name'],
  data() {
    return { count: 0 }
  },
  methods: {
    increment() { this.count++ }
  }
}
</script>

<!-- GOOD -->
<script setup lang="ts">
interface Props { name: string }
defineProps<Props>()
const count = ref(0)
function increment() { count.value++ }
</script>
```

### Untyped props

```vue
<!-- BAD -->
<script setup>
const props = defineProps(['name', 'price'])
// No TypeScript, no IDE completion, no compile errors
</script>

<!-- GOOD -->
<script setup lang="ts">
interface Props {
  name: string
  price: number
}
const props = defineProps<Props>()
</script>
```

### Inline styles instead of Tailwind

```vue
<!-- BAD -->
<template>
  <div :style="{ padding: '16px', backgroundColor: '#6366f1', borderRadius: '8px' }">
    content
  </div>
</template>

<!-- GOOD -->
<template>
  <div class="rounded-lg bg-indigo-500 p-4">content</div>
</template>
```

### UI component reaching into a store

```vue
<!-- BAD — tight coupling, can't reuse or test in isolation -->
<script setup lang="ts">
import { useCartStore } from '@/stores/cartStore'
const cart = useCartStore()
</script>

<!-- GOOD — emit an event, let the parent/page handle it -->
<script setup lang="ts">
const emit = defineEmits<{ addToCart: [id: string] }>()
</script>
```

## Decision Guide

| Scenario | Pattern |
|----------|---------|
| Route-level view | Page component (`--page`), orchestrates stores and child components |
| Reusable UI widget (button, card, badge) | UI component (`--ui`), props in / events out |
| Full-page shell with nav + sidebar | Layout component (`--layout`), slot-driven |
| Two-way binding needed | `defineModel<T>()` |
| Long list of similar items | `v-for` with `:key`, consider `v-memo` for expensive rows |
| Conditional heavy subtree | `v-if` + `defineAsyncComponent` for deferred load |
| Component name in DevTools | `defineOptions({ name: 'MyComponent' })` |
| Shared logic across components | Extract to composable, not a mixin |

## Execution

Read the existing component directory structure with Glob to match project conventions (file naming, import aliases, existing UI primitives). Check `vite.config.ts` for the `@` alias target.

### `/component <ComponentName> [--page|--layout|--ui]`

1. Determine the correct directory:
   - `--page` → `features/{feature}/pages/{Name}Page.vue`
   - `--layout` → `layouts/{Name}Layout.vue`
   - `--ui` → `components/ui/{Name}.vue`
   - default → `features/{feature}/components/{Name}.vue`
2. Generate the SFC with `<script setup lang="ts">`, typed `Props` interface, typed `defineEmits`, and a semantic template skeleton with TailwindCSS classes.
3. For `--page`: add `onMounted` store fetch stub and relevant imports.
4. For `--layout`: add at least a default slot plus `header` and `footer` named slots.
5. For `--ui`: emit events rather than accessing stores; include `withDefaults` when defaults are meaningful.
6. After writing, run `get_vue_type_errors` via the vue-mcp tool to validate the generated file.

$ARGUMENTS
