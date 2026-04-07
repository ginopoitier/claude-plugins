# memory-kit

Intelligent memory management meta-kit for Claude Code. Auto-captures corrections and discoveries via hooks, injects relevant memories at session start, and provides semantic retrieval, deduplication, and lifecycle management via the `memory-mcp` server.

## What's Included

### Skills (6)

| Skill | Description |
|-------|-------------|
| `/memory-setup` | Interactive wizard — configure base path, project ID strategy, hooks |
| `/memory-capture` | Manually store a memory with auto-classification and deduplication |
| `/memory-recall` | Surface top relevant memories for the current task |
| `/memory-health` | Audit memory store — stale entries, duplicates, index drift, coverage gaps |
| `/memory-consolidate` | Deduplicate and merge similar memories, resolve conflicts |
| `/memory-forget` | Deprecate or permanently delete memories; bulk stale cleanup |

### Agent (1)

| Agent | Role |
|-------|------|
| `memory-curator` | Deep curation sessions — reviews full store, resolves conflicts, retires stale entries |

### Rules (always-active, 3)

`memory-conventions` · `memory-lifecycle` · `auto-capture-triggers`

### Hooks (auto-active)

| Hook | Event | Behavior |
|------|-------|----------|
| `memory-inject.sh` | `SessionStart` | Loads MEMORY.md summary into context |
| `check-settings.sh` | `UserPromptSubmit` | Warns if config is missing |
| `auto-capture.sh` | `Stop` | Detects correction patterns after each response |

### MCP Server

`memory-mcp` — TF-IDF semantic search, deduplication, health monitoring, and index management. Falls back to grep-based search when unavailable.

## Install

```
/plugin marketplace add ginopoitier/claude-plugins
/plugin install memory-kit@ginopoitier-plugins
```

## First-Time Setup

```
/memory-setup
```

Configures `~/.claude/memory-kit.config.md`, creates the project memory directory, and registers hooks in `~/.claude/settings.json`.

## Memory Types

| Type | Stores |
|------|--------|
| `user` | Role, expertise, collaboration preferences |
| `feedback` | Corrections and approach guidance (class-level rules) |
| `project` | Deadlines, initiatives, team decisions |
| `reference` | External system locations and pointers |

## Requirements

- Claude Code 1.0.0+
- Node.js (for `memory-mcp` via npx)

## License

MIT
