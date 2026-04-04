# Vue Kit

> **Config:** @~/.claude/kit.config.md — run `/kit-setup` if missing.

## Stack
- **Language:** TypeScript · Vue 3 Composition API · Pinia · Vue Router
- **Build:** Vite · TailwindCSS
- **Real-time:** SignalR (`@microsoft/signalr`)
- **MCP:** `vue-mcp` — Vue SFC analysis, Pinia store inspection, TypeScript type checking, project structure

## Always-Active Rules

@~/.claude/rules/vue.md
@~/.claude/rules/typescript.md
@~/.claude/rules/security.md
@~/.claude/rules/performance.md
@~/.claude/rules/agents.md
@~/.claude/rules/hooks.md
@~/.claude/rules/sdlc.md
@~/.claude/rules/git-workflow.md

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

## Skills Available

### Code Generation
- `/signalr-hub` — .NET-hosted SignalR client integration via Vue composables and Vite proxy

### Session & Workflow
- `/session-management` — start/end/resume development sessions
- `/workflow-mastery` — plan and track multi-session epics
- `/wrap-up-ritual` — structured session ending with handoff note
- `/sdlc-check` — validate work against company SDLC

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
