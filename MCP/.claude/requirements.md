# Memory MCP Server — Requirements

## Overview

A lightweight MCP server (`memory-mcp`) that provides intelligent memory management for Claude Code. It manages the file-based memory store at `~/.claude/projects/*/memory/`, enabling semantic search, deduplication, health monitoring, and auto-classification.

Packaged as `@ginopoitier/memory-mcp` on npm. Registered in `memory-kit/.claude-plugin/plugin.json` under `mcpServers`.

## Goals

1. Make memory retrieval intelligent — surface relevant memories automatically based on context
2. Keep memory clean — detect duplicates, conflicts, stale entries
3. Zero-friction capture — classify and store memories with minimal ceremony
4. CLI + MCP dual interface — hooks can call it as a CLI binary; Claude uses it via MCP tools

## Storage Model

Memory files live under a configurable base path (default: `~/.claude/projects/`).

```
~/.claude/projects/
  {project-id}/                # directory-safe slug (git remote origin slug or cwd name)
    memory/
      user_*.md                # user profile memories
      feedback_*.md            # behavioral feedback memories
      project_*.md             # project context/initiative memories
      reference_*.md           # external reference memories
    MEMORY.md                  # index file — one line per memory, auto-maintained by MCP
    .claude/
      instincts.md
      learning-log.md
```

### Memory File Frontmatter Schema

Every memory file starts with YAML frontmatter:

```yaml
---
name: string                          # short human-readable name
description: string                   # one-line hook used in MEMORY.md index
type: user | feedback | project | reference
tags: string[]                        # auto-extracted keywords for search
created: ISO-8601
updated: ISO-8601
confidence: 0.0-1.0                   # 1.0 for manually written; lower for auto-captured
source: manual | auto-capture | promoted-instinct
---

Body content here (markdown). For feedback/project types, use:
**Why:** reason the user gave
**How to apply:** when this guidance kicks in
```

### MEMORY.md Index Format

```markdown
- [Feedback: avoid database mocking](memory/feedback_db_mocking.md) — Integration tests must hit real database; mocked tests mask migration failures
- [User: senior .NET developer](memory/user_profile.md) — 10 years .NET, new to Vue frontend in this project
```

One line per entry, under 150 characters, format: `- [Title](relative/path.md) — one-line hook`.

---

## MCP Tools

### `memory_search`
Find memories relevant to a query using TF-IDF keyword scoring against name + description + body.

**Input:**
```json
{
  "query": "string — natural language query or keywords",
  "type": "user|feedback|project|reference  (optional filter)",
  "project_id": "string (optional, defaults to active project)",
  "limit": "number (default: 5, max: 20)",
  "min_score": "number (default: 0.2, range: 0.0-1.0)"
}
```

**Output:**
```json
{
  "results": [
    {
      "file": "memory/feedback_testing.md",
      "name": "Integration tests require real database",
      "type": "feedback",
      "score": 0.87,
      "excerpt": "Don't mock the database in these tests — we got burned last quarter..."
    }
  ],
  "total": 1,
  "project_id": "my-project"
}
```

**Algorithm:** TF-IDF weighted by field (name: 3×, description: 2×, tags: 2×, body: 1×). No ML runtime dependency in v1.

---

### `memory_get`
Read a specific memory file by path.

**Input:** `{ "file_path": "string" }`

**Output:** Full parsed memory: frontmatter fields + body string.

---

### `memory_store`
Create or update a memory file. If a file with the same name slug exists, updates it in place.

**Input:**
```json
{
  "name": "string",
  "description": "string",
  "type": "user|feedback|project|reference",
  "body": "string",
  "project_id": "string (optional)",
  "file_name": "string (optional — auto-derived from type + name slug if omitted)",
  "confidence": "number (optional, default: 1.0)",
  "source": "manual|auto-capture|promoted-instinct (optional, default: manual)"
}
```

**Output:** `{ "file": "memory/feedback_testing.md", "action": "created|updated" }`

**Side effect:** Rebuilds the MEMORY.md index entry for this file.

---

### `memory_delete`
Remove a memory file and its MEMORY.md index entry.

**Input:** `{ "file_path": "string", "project_id": "string (optional)" }`

**Output:** `{ "deleted": "memory/feedback_testing.md", "index_updated": true }`

---

### `memory_list`
List all memories for a project with metadata.

**Input:**
```json
{
  "type": "user|feedback|project|reference (optional)",
  "project_id": "string (optional)",
  "sort": "updated|created|name|confidence (default: updated)",
  "format": "full|summary (default: full)"
}
```

**Output:**
```json
{
  "memories": [
    {
      "file": "memory/feedback_testing.md",
      "name": "Integration tests require real database",
      "description": "Don't mock database in tests...",
      "type": "feedback",
      "updated": "2026-03-15T14:30:00Z",
      "confidence": 1.0,
      "source": "manual"
    }
  ],
  "total": 7,
  "by_type": { "user": 1, "feedback": 5, "project": 1, "reference": 0 }
}
```

---

### `memory_deduplicate`
Find memories with high semantic similarity — potential duplicates or conflicts.

**Input:**
```json
{
  "project_id": "string (optional)",
  "threshold": "number (default: 0.7, range: 0.0-1.0)"
}
```

**Output:**
```json
{
  "groups": [
    {
      "similarity": 0.85,
      "files": ["memory/feedback_testing.md", "memory/feedback_db_mocking.md"],
      "recommendation": "merge — both address database mocking in tests",
      "conflict": false
    },
    {
      "similarity": 0.72,
      "files": ["feedback_logging_a.md", "feedback_logging_b.md"],
      "recommendation": "review — may conflict on log level strategy",
      "conflict": true
    }
  ],
  "clean": false
}
```

---

### `memory_health`
Full health report on the memory store.

**Input:** `{ "project_id": "string (optional)" }`

**Output:**
```json
{
  "total": 12,
  "by_type": { "user": 2, "feedback": 7, "project": 2, "reference": 1 },
  "issues": [
    {
      "severity": "warning",
      "file": "memory/project_sprint.md",
      "issue": "stale — not updated in 30+ days, may no longer be accurate"
    },
    {
      "severity": "error",
      "file": "memory/feedback_testing.md",
      "issue": "missing required 'description' field in frontmatter"
    },
    {
      "severity": "info",
      "file": "memory/feedback_logging.md",
      "issue": "potential duplicate of feedback_console.md (similarity: 0.78)"
    }
  ],
  "index_sync": "ok|drift",
  "drift_details": "3 files not in MEMORY.md index",
  "coverage_gaps": [
    "no reference memories found — consider documenting external system locations",
    "no user profile memory — consider capturing user role and expertise"
  ],
  "score": 0.75
}
```

**Staleness threshold:** `project` type memories older than 30 days; `reference` older than 90 days; `user` and `feedback` don't expire.

---

### `memory_sync_index`
Rebuild MEMORY.md from actual memory files (fixes index drift).

**Input:** `{ "project_id": "string (optional)" }`

**Output:** `{ "added": 3, "removed": 1, "unchanged": 8, "index_path": "~/.claude/projects/myproject/MEMORY.md" }`

---

### `memory_classify`
Auto-classify a piece of text into a memory type.

**Input:** `{ "content": "string" }`

**Output:**
```json
{
  "type": "feedback",
  "confidence": 0.82,
  "reasoning": "Contains behavioral correction pattern: 'don't mock the database' with a 'why' explanation",
  "suggested_name": "avoid_database_mocking_in_tests",
  "suggested_description": "Integration tests must hit a real database; mocked tests mask migration failures"
}
```

**Classification heuristics:**
- `feedback`: correction patterns ("don't", "stop", "always/never do X", "we got burned"), approach validations, behavioral guidance
- `user`: role/expertise descriptors ("I'm a", "I've been using", "my background is"), tools/workflow preferences
- `project`: temporal context ("we're doing", "the reason we", "by Thursday", deadline, team, initiative, "because legal")
- `reference`: external system pointers ("tracked in", "can be found at", "board at", URLs, system names + "is where")

---

### `memory_export`
Export all memories for a project.

**Input:**
```json
{
  "project_id": "string (optional)",
  "format": "markdown|json (default: markdown)",
  "type": "user|feedback|project|reference (optional)"
}
```

**Output:** Combined export string in requested format.

---

## CLI Interface

The server exposes a CLI mode for use in hooks (bash scripts). Binary name: `memory-mcp`.

```bash
# Search — returns JSON to stdout
memory-mcp search --query "testing database" --limit 3 --project myproject

# List all memories for current project (auto-detected via git or cwd)
memory-mcp list --type feedback --format summary

# Health check
memory-mcp health

# Sync MEMORY.md index
memory-mcp sync-index

# Classify text
memory-mcp classify --content "Don't mock the database in tests"
```

**Project auto-detection order:**
1. `--project` flag
2. `$MEMORY_PROJECT_ID` env var
3. `git remote get-url origin` slug (strip `.git`, lowercase, replace `/` with `-`)
4. `basename $(pwd)` lowercased

---

## Technology Stack

| Concern | Choice | Reason |
|---------|--------|--------|
| Runtime | Node.js 20+ / TypeScript | Same stack as vue-mcp; no extra runtime |
| MCP SDK | `@modelcontextprotocol/sdk` | Same as vue-mcp |
| Frontmatter parsing | `gray-matter` | Robust YAML frontmatter parsing |
| Search | TF-IDF (custom implementation) | No ML runtime dep; good enough for v1 |
| File I/O | Node.js `fs/promises` | Native, no extra deps |
| CLI arg parsing | `minimist` or `yargs` | Lightweight |

**npm package name:** `@ginopoitier/memory-mcp`

---

## Non-Goals (v1)

- No vector/embedding-based semantic search (add in v2 if keyword search is insufficient)
- No cross-project memory sharing or sync
- No cloud sync or remote storage
- No encryption of memory files
- No web UI

---

## Integration with memory-kit

The memory-mcp is consumed by the memory-kit plugin in two ways:

**1. Via MCP tools (Claude uses during conversation):**
- `/memory-capture` skill calls `memory_classify` then `memory_store`
- `/memory-health` skill calls `memory_health` then `memory_sync_index` if needed
- `/memory-recall` skill calls `memory_search` with the current task context
- `/memory-consolidate` skill calls `memory_deduplicate` then guides merging

**2. Via CLI in hooks (bash scripts):**
- `memory-inject.sh` (SessionStart): `memory-mcp list --format summary` to load context
- Future: `auto-capture.sh` (Stop): `memory-mcp classify` to detect capture candidates

---

## File Location

Source: `MCP/Memory/` (to be created)
Structure mirrors `MCP/Vue/vue-mcp/` layout:
```
MCP/Memory/
  memory-mcp/
    src/
      index.ts          # MCP server + CLI entrypoint
      tools/
        search.ts
        store.ts
        list.ts
        deduplicate.ts
        health.ts
        classify.ts
        syncIndex.ts
        export.ts
      services/
        storage.ts      # file I/O, frontmatter parsing
        tfidf.ts        # TF-IDF search implementation
        projectDetector.ts  # project ID auto-detection
    package.json
    tsconfig.json
```
