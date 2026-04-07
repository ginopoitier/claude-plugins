# vue-kit

Vue 3 + TypeScript developer toolkit for Claude Code — Composition API, Pinia stores, typed API clients, Vitest testing, Vue Router patterns, SignalR integration, and TailwindCSS.

## What's Included

### Skills (11)

| Category | Skills |
|----------|--------|
| **Scaffolding** | `/component`, `/composable`, `/pinia-store`, `/api-client`, `/signalr-hub` |
| **Quality & Testing** | `/testing`, `/error-handling`, `/performance` |
| **Architecture** | `/routing` |
| **Workflow** | `/wrap-up-ritual` |
| **Setup** | `/kit-setup` |

### Agents (2)

| Agent | Role |
|-------|------|
| `code-reviewer` | Vue 3 / TypeScript code review — correctness, patterns, security, performance |
| `security-auditor` | XSS risks, CSRF gaps, exposed secrets, insecure API usage, vulnerable dependencies |

### Rules (always-active, 7)

`vue` · `typescript` · `security` · `performance` · `agents` · `hooks` · `git-workflow`

### MCP Server

`vue-mcp` — Vue SFC analysis, Pinia store inspection, TypeScript type checking, project structure. Powers component-level diagnostics without prompt stuffing.

## Install

```
/plugin marketplace add ginopoitier/claude-plugins
/plugin install vue-kit@ginopoitier-plugins
```

## First-Time Setup

```
/kit-setup
```

Configures `~/.claude/kit.config.md` with project defaults.

Install the MCP server:

```bash
npm install -g @ginopoitier/vue-mcp
```

## Key Patterns

- Always `<script setup lang="ts">` — never Options API
- Shared state → Pinia store (Composition API style)
- Local state → `ref` / `reactive` in `<script setup>`
- Shared logic without state → composable (`useXxx.ts`)
- All API calls → `features/{name}/api.ts`, always typed
- SignalR → `signalrStore` owns the connection lifecycle

## Requirements

- Claude Code 1.0.0+
- Node.js 18+
- `vue-mcp` (optional but recommended)

## License

MIT
