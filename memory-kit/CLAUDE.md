# Memory Kit

> **Intelligent memory management meta-kit for Claude Code.** Auto-captures corrections and discoveries via hooks, injects relevant memories at session start, and provides semantic retrieval, deduplication, and lifecycle management via the `memory-mcp` MCP server.

## Always-Active Rules

@~/.claude/rules/memory-kit/memory-conventions.md
@~/.claude/rules/memory-kit/memory-lifecycle.md
@~/.claude/rules/memory-kit/auto-capture-triggers.md

## Meta — Always Apply

@~/.claude/skills/context-discipline/SKILL.md
@~/.claude/skills/model-selection/SKILL.md

## Self-Improvement — Auto-Active

@~/.claude/skills/instinct-system/SKILL.md
@~/.claude/skills/self-correction-loop/SKILL.md
@~/.claude/skills/learning-log/SKILL.md
@~/.claude/skills/autonomous-loops/SKILL.md

## MCP Integration

Powered by **memory-mcp** — a Node.js MCP server for intelligent memory operations.

Available tools (use these directly rather than reading memory files manually):
- `memory_search` — keyword/TF-IDF search across all memory files
- `memory_store` — create or update a memory with frontmatter
- `memory_list` — enumerate all memories for current project
- `memory_health` — health report: stale entries, duplicates, index drift, coverage gaps
- `memory_deduplicate` — find semantically similar or conflicting memories
- `memory_sync_index` — rebuild MEMORY.md index from actual memory files
- `memory_classify` — auto-detect memory type from content
- `memory_export` — export all memories as markdown or JSON

All paths come from kit config (`~/.claude/memory-kit.config.md`):
`MEMORY_BASE_PATH` · `PROJECT_ID_STRATEGY` · `AUTO_CAPTURE_ENABLED` · `AUTO_INJECT`

When config is missing → tell user to run `/memory-setup`.

## Hooks (auto-active when kit is installed)

| Hook | Event | Behavior |
|------|-------|----------|
| `memory-inject.sh` | `SessionStart` | Loads MEMORY.md summary into session context so existing rules are always visible |
| `check-settings.sh` | `UserPromptSubmit` | Advisory: warns if memory-kit.config.md is missing |
| `auto-capture.sh` | `Stop` | Advisory: detects correction patterns and nudges Claude to persist them |

## Skills Available

### Memory Management

- `/memory-setup` — configure memory-kit: base path, project ID strategy, auto-capture and inject toggles; registers hooks in settings.json
- `/memory-capture` — manually capture a memory: auto-classify type, name it, write with correct frontmatter, update MEMORY.md index
- `/memory-recall` — explicit context retrieval: search by query, surface top relevant memories for current work session
- `/memory-health` — audit memory store: stale entries, missing fields, duplicates, index drift, coverage gaps; produce health score
- `/memory-consolidate` — deduplicate and merge similar memories, resolve conflicts, rewrite for clarity and precision
- `/memory-forget` — deprecate or permanently delete memories; bulk-remove stale project entries

### Meta (auto-active)

- `context-discipline` — token budget management, lazy loading patterns
- `model-selection` — route tasks to Haiku/Sonnet/Opus by complexity
- `instinct-system` — confidence-scored pattern learning, observe→confirm→promote cycle
- `self-correction-loop` — captures every user correction into MEMORY.md as a permanent rule
- `learning-log` — session discovery capture (bugs, gotchas, architectural decisions)
- `autonomous-loops` — bounded iteration for bulk memory operations

## Knowledge (on demand)

- `memory-taxonomy.md` — when to use each memory type, classification rules, anti-patterns
- `mcp-tools-reference.md` — full MCP tool signatures, example inputs/outputs, integration patterns
