# Vue App Project

Vue 3 · TypeScript · Vite · TailwindCSS · Pinia · SignalR · Composition API

## Project Layout

```
src/
  assets/
  components/          # Shared reusable UI components
  composables/         # Shared composables (useXxx)
  features/            # Feature-based folders — mirror backend domains
    {feature}/
      components/      # Feature-specific components
      composables/     # Feature-specific composables
      stores/          # Pinia store(s)
      api.ts           # Typed API client functions
      types.ts         # Feature types and interfaces
  layouts/
  pages/               # Route-level components
  router/
  stores/
    authStore.ts
    signalrStore.ts    # SignalR connection management
  lib/
    api.ts             # Base axios client (interceptors, ProblemDetails error handling)
    signalr.ts         # SignalR connection factory
  types/               # Global shared types
  App.vue
  main.ts
```

## Key Conventions

- `<script setup lang="ts">` always — no Options API
- Pinia stores: Composition API style with `defineStore('name', () => { })`
- SignalR: managed in `signalrStore`, consumed via feature composables
- API calls: `features/{name}/api.ts` — never in components
- TailwindCSS only — no custom CSS for component styles
- `@/` imports everywhere — no relative `../../` paths
- Strict TypeScript — no `any`, no `@ts-ignore` without explanation

## SignalR Integration

```ts
// 1. signalrStore manages connections
// 2. Feature composable (useOrderUpdates) adds/removes listeners
// 3. Component calls composable in setup
```

## API Error Handling

The base client in `lib/api.ts` intercepts ProblemDetails responses from the .NET API and throws typed `ApiError` objects. Feature `api.ts` files only need to call the base client — error handling is centralized.

## Vite Proxy

```ts
// Proxy /api → .NET backend
// Proxy /hubs with ws:true → SignalR
```

## Agents Available

- `@vue-expert` — components, stores, SignalR, TypeScript patterns
