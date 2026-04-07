# Vue Kit

> **Config:** @~/.claude/kit.config.md — run `/kit-setup` if missing.

## Stack
- **Language:** TypeScript · Vue 3 Composition API · Pinia · Vue Router
- **Build:** Vite · TailwindCSS
- **Real-time:** SignalR (`@microsoft/signalr`)
- **MCP:** `vue-mcp` — Vue SFC analysis, Pinia store inspection, TypeScript type checking, project structure

## Always-Active Rules

@~/.claude/rules/vue-kit/vue.md
@~/.claude/rules/vue-kit/typescript.md
@~/.claude/rules/vue-kit/security.md
@~/.claude/rules/vue-kit/performance.md
@~/.claude/rules/vue-kit/agents.md
@~/.claude/rules/vue-kit/hooks.md
@~/.claude/rules/vue-kit/git-workflow.md

## Config System

### User / Device Level — `~/.claude/kit.config.md`
Run `/kit-setup` to configure.

When a skill needs config and `~/.claude/kit.config.md` is missing → tell user to run `/kit-setup`.

## MCP Tools Available

The `vue-mcp` server exposes these tools:

| Tool | What it does |
|------|-------------|
| `analyze_vue_components` | Scan `.vue` files for issues: missing `<script setup>`, untyped props/emits, inline styles, SignalR leaks |
| `find_vue_component` | Locate a component by name, return its script and template |
| `validate_pinia_stores` | Check stores for Options API usage, missing returns, async computed |
| `get_vue_type_errors` | Run `vue-tsc --noEmit` and return type errors |
| `get_vue_composables` | List composable files and their exports |
| `get_vue_project_structure` | Vite config, router routes, proxy paths, file counts |
| `find_missing_api_types` | Find `api.ts` files with `Promise<any>` or no typed interfaces |

## Knowledge

- `knowledge/vue/vue-patterns.md` — deep reference: component patterns, composable design, Pinia patterns, typing guide

## Skills Available

### Code Generation / Scaffolding
- `/component` — scaffold a Vue 3 SFC (`<script setup lang="ts">`, typed props/emits, TailwindCSS)
- `/composable` — scaffold a `useXxx` composable with cleanup and TypeScript types
- `/pinia-store` — scaffold a Pinia Composition API store with typed state, getters, and async actions
- `/api-client` — scaffold a typed `features/{name}/api.ts` API client module
- `/signalr-hub` — .NET-hosted SignalR client integration via Vue composables and Vite proxy

### Quality & Testing
- `/testing` — Vitest + @vue/test-utils: component tests, store tests, composable tests, API mocking
- `/error-handling` — global error handler, error boundary pattern, typed API error responses
- `/performance` — lazy routes, defineAsyncComponent, v-memo, shallowRef, bundle analysis

### Architecture
- `/routing` — Vue Router 4: typed routes, lazy loading, navigation guards, route meta typing

### Session & Workflow
- `/wrap-up-ritual` — structured session ending with handoff note

### Setup
- `/kit-setup` — configure kit settings

## Key Patterns

### Component structure
Always use `<script setup lang="ts">`. Never use Options API.

### State management
- Shared state → Pinia store (Composition API style: `defineStore('id', () => { ... })`)
- Local state → `ref` / `reactive` in `<script setup>`
- Shared logic without state → composable (`useXxx.ts`)

### SignalR
- Connection lifecycle lives in `signalrStore`
- Feature composables add/remove listeners — always pair `connection.on` with `connection.off` in `onUnmounted`
- Never put `connection.on(...)` directly in a component

### API client
- All API calls go in `features/{name}/api.ts`
- Always type request and response — no `Promise<any>`
- Call from store actions, not directly from components

### Vite proxy
Always proxy `/api` and `/hubs` (WebSocket) to the .NET backend:
```ts
server: {
  proxy: {
    '/api': 'http://localhost:5000',
    '/hubs': { target: 'http://localhost:5000', ws: true }
  }
}
```
