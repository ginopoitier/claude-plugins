---
name: routing
description: >
  Vue Router 4 patterns — typed routes, lazy loading, navigation guards, and route meta.
  Load this skill when: "router", "routing", "vue router", "routes", "navigation guard",
  "lazy load route", "route meta", "programmatic navigation", "route params", "/routing".
user-invocable: true
argument-hint: "[feature to add routes for]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# Routing — Vue Router 4 Patterns

## Core Principles

1. **All routes are lazy-loaded** — Every `component` in a route record uses `() => import(...)`. This splits each page into its own chunk and eliminates the need for eager-load decisions later.
2. **Route params and query are always typed** — Use a typed composable wrapper around `useRoute` so `params.id` is `string`, not `string | string[]`.
3. **Navigation guards enforce auth at the router level** — Never put redirect logic inside page components. `router.beforeEach` checks `route.meta.requiresAuth` and redirects unauthenticated users.
4. **Route names are string literal union types** — Define a `RouteName` type so `router.push({ name: 'product-detail' })` is checked at compile time.
5. **Meta is typed via `RouteMeta` augmentation** — Extend Vue Router's `RouteMeta` interface in a `.d.ts` file so `route.meta.requiresAuth` is `boolean`, not `unknown`.

## Patterns

### Router Setup with Lazy Routes

```typescript
// router/index.ts
import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/features/auth/stores/authStore'

export type RouteName =
  | 'home'
  | 'login'
  | 'product-list'
  | 'product-detail'
  | 'product-create'
  | 'order-list'
  | 'order-detail'
  | 'profile'
  | 'not-found'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'home' satisfies RouteName,
    component: () => import('@/features/home/pages/HomePage.vue'),
  },
  {
    path: '/login',
    name: 'login' satisfies RouteName,
    component: () => import('@/features/auth/pages/LoginPage.vue'),
    meta: { requiresGuest: true },
  },
  {
    path: '/products',
    component: () => import('@/layouts/AppShell.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'product-list' satisfies RouteName,
        component: () => import('@/features/products/pages/ProductListPage.vue'),
      },
      {
        path: ':id',
        name: 'product-detail' satisfies RouteName,
        component: () => import('@/features/products/pages/ProductDetailPage.vue'),
        props: true,
      },
      {
        path: 'new',
        name: 'product-create' satisfies RouteName,
        component: () => import('@/features/products/pages/ProductCreatePage.vue'),
        meta: { requiresRole: 'admin' },
      },
    ],
  },
  {
    path: '/orders',
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'order-list' satisfies RouteName,
        component: () => import('@/features/orders/pages/OrderListPage.vue'),
      },
      {
        path: ':id',
        name: 'order-detail' satisfies RouteName,
        component: () => import('@/features/orders/pages/OrderDetailPage.vue'),
        props: true,
      },
    ],
  },
  {
    path: '/profile',
    name: 'profile' satisfies RouteName,
    component: () => import('@/features/auth/pages/ProfilePage.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'not-found' satisfies RouteName,
    component: () => import('@/pages/NotFoundPage.vue'),
  },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior(to, _from, savedPosition) {
    if (savedPosition) return savedPosition
    if (to.hash) return { el: to.hash, behavior: 'smooth' }
    return { top: 0 }
  },
})
```

### Typed `RouteMeta` Augmentation

```typescript
// router/meta.d.ts
import 'vue-router'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    requiresGuest?: boolean
    requiresRole?: string
    title?: string
    breadcrumb?: string
  }
}
```

### Global Navigation Guard

```typescript
// router/guards.ts
import type { Router } from 'vue-router'
import { useAuthStore } from '@/features/auth/stores/authStore'

export function registerGuards(router: Router) {
  router.beforeEach((to, _from) => {
    // Auth store is available here because Pinia is installed before the router
    const auth = useAuthStore()

    if (to.meta.requiresAuth && !auth.isAuthenticated) {
      return { name: 'login', query: { redirect: to.fullPath } }
    }

    if (to.meta.requiresGuest && auth.isAuthenticated) {
      return { name: 'home' }
    }

    if (to.meta.requiresRole && auth.user?.role !== to.meta.requiresRole) {
      return { name: 'home' }
    }

    return true
  })

  // Update document title from route meta
  router.afterEach((to) => {
    document.title = to.meta.title ? `${to.meta.title} — MyApp` : 'MyApp'
  })
}
```

```typescript
// main.ts
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import { router } from './router'
import { registerGuards } from './router/guards'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)       // Pinia must come before router so guards can call stores
app.use(router)

registerGuards(router)

app.mount('#app')
```

### Typed Route Params Composable

```typescript
// composables/useTypedRoute.ts
// Vue Router returns `string | string[]` for params — this narrows to string

import { useRoute } from 'vue-router'
import { computed } from 'vue'

export function useStringParam(name: string) {
  const route = useRoute()
  return computed(() => {
    const val = route.params[name]
    return Array.isArray(val) ? val[0] : val
  })
}

export function useStringQuery(name: string) {
  const route = useRoute()
  return computed(() => {
    const val = route.query[name]
    if (Array.isArray(val)) return val[0] ?? null
    return val ?? null
  })
}
```

Usage:

```vue
<!-- features/products/pages/ProductDetailPage.vue -->
<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { useStringParam } from '@/composables/useTypedRoute'
import { useProductStore } from '@/features/products/stores/productStore'

const productId = useStringParam('id')
const store = useProductStore()

onMounted(() => store.fetchById(productId.value))

// Re-fetch if the user navigates between detail pages without unmounting
watch(productId, (id) => store.fetchById(id))
</script>
```

### Programmatic Navigation

```typescript
// Typed navigation helper
import { useRouter } from 'vue-router'
import type { RouteName } from '@/router'

export function useNav() {
  const router = useRouter()

  return {
    goToProduct: (id: string) =>
      router.push({ name: 'product-detail' satisfies RouteName, params: { id } }),

    goToProducts: () =>
      router.push({ name: 'product-list' satisfies RouteName }),

    goBack: () => router.back(),

    replaceToLogin: (redirect?: string) =>
      router.replace({
        name: 'login' satisfies RouteName,
        query: redirect ? { redirect } : undefined,
      }),
  }
}
```

### Route-Level Loading State with `<Suspense>`

```vue
<!-- App.vue -->
<script setup lang="ts">
import { RouterView } from 'vue-router'
</script>

<template>
  <RouterView v-slot="{ Component }">
    <Suspense>
      <component :is="Component" />
      <template #fallback>
        <div class="flex h-screen items-center justify-center">
          <div class="h-10 w-10 animate-spin rounded-full border-4 border-indigo-200 border-t-indigo-600" />
        </div>
      </template>
    </Suspense>
  </RouterView>
</template>
```

### Per-Route Scroll Preservation

```typescript
// router/index.ts (scrollBehavior already shown above)
// For infinite-scroll pages, save scroll position manually:

router.beforeEach((to, from) => {
  if (from.name === 'product-list') {
    from.meta.savedScrollY = window.scrollY
  }
})

// In ProductListPage.vue
onMounted(() => {
  const y = route.meta.savedScrollY as number | undefined
  if (y) window.scrollTo({ top: y })
})
```

## Anti-patterns

### Eager loading all routes

```typescript
// BAD — entire app bundled into one chunk
import ProductListPage from '@/features/products/pages/ProductListPage.vue'
import ProductDetailPage from '@/features/products/pages/ProductDetailPage.vue'

const routes = [
  { path: '/products', component: ProductListPage },
  { path: '/products/:id', component: ProductDetailPage },
]

// GOOD — each page is its own chunk
const routes = [
  { path: '/products', component: () => import('@/features/products/pages/ProductListPage.vue') },
  { path: '/products/:id', component: () => import('@/features/products/pages/ProductDetailPage.vue') },
]
```

### Auth guard logic inside a page component

```vue
<!-- BAD — duplicated guard in every protected page -->
<script setup lang="ts">
const auth = useAuthStore()
const router = useRouter()
onMounted(() => {
  if (!auth.isAuthenticated) router.replace('/login')
})
</script>

<!-- GOOD — guard in router/guards.ts, meta on the route record -->
```

### Using `route.params.id` without narrowing

```typescript
// BAD — route.params.id is string | string[]
const id: string = route.params.id  // TypeScript error or unsafe cast
const product = await fetchProductById(route.params.id)  // type mismatch

// GOOD — use the narrowing composable
const productId = useStringParam('id')
const product = await fetchProductById(productId.value)
```

### Using `any` for route meta

```typescript
// BAD — no IntelliSense, runtime errors possible
if (route.meta.requiresAuth) { ... }  // type is `any` without augmentation

// GOOD — augment RouteMeta in router/meta.d.ts
// route.meta.requiresAuth is now `boolean | undefined`
```

## Decision Guide

| Scenario | Pattern |
|----------|---------|
| New feature with multiple sub-pages | Nested routes under a layout `component: () => import(...)` |
| Protect routes for authenticated users | `meta: { requiresAuth: true }` + `beforeEach` guard |
| Protect routes for a specific role | `meta: { requiresRole: 'admin' }` + guard check |
| Redirect logged-in users away from login | `meta: { requiresGuest: true }` |
| Read route param as string | `useStringParam('id')` composable |
| Read optional query string | `useStringQuery('search')` composable |
| Navigate programmatically | `useNav()` composable with named route constants |
| Loading state between route transitions | `<Suspense>` in `App.vue` wrapping `<RouterView>` |
| Per-page document title | `route.meta.title` + `afterEach` in guards |
| Preserve scroll on back navigation | `scrollBehavior` returning `savedPosition` |

## Execution

Run `get_vue_project_structure` via vue-mcp to read existing route configuration and import aliases before adding routes.

### `/routing [feature]`

1. Read the current `router/index.ts` (or `router.ts`) to understand existing route structure.
2. Add route record(s) for the feature:
   - All `component` values use `() => import(...)` dynamic imports
   - Apply `meta: { requiresAuth: true }` where appropriate
   - Use `props: true` on routes with `:id` params
3. If `router/meta.d.ts` does not exist, generate it with `RouteMeta` augmentation.
4. If a `RouteName` union type exists, extend it with the new names.
5. If route params are consumed in page components, use `useStringParam` — generate it in `composables/` if missing.
6. Register guards in `router/guards.ts` if a `beforeEach` guard does not already exist.

$ARGUMENTS
