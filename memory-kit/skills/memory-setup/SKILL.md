---
name: memory-setup
description: >
  Interactive configuration wizard for the memory-kit. Creates ~/.claude/memory-kit.config.md,
  sets up the memory directory for the current project, and registers hooks in settings.json.
  Load this skill when: "setup memory", "configure memory kit", "memory-setup", "/memory-setup",
  "initialize memory", "memory config", "first time memory".
user-invocable: true
argument-hint: "[--project <id>]"
allowed-tools: Read, Write, Edit, Bash, Glob
---

# Memory Setup

## Core Principles

1. **Never overwrite existing config without confirmation** — Ask before replacing an existing `~/.claude/memory-kit.config.md`.
2. **Detect project automatically** — Use git remote or cwd to suggest the project ID; let user confirm.
3. **Create memory directory** — Ensure `{MEMORY_BASE_PATH}/{project-id}/memory/` exists after setup.
4. **Initialize MEMORY.md** — Write an empty index header if the file doesn't exist.
5. **Register hooks** — Add memory-inject, check-settings, and auto-capture hooks to `~/.claude/settings.json`.

## Setup Flow

### Phase 1: Config File

Check if `~/.claude/memory-kit.config.md` exists:

```
EXISTS → "Memory kit config found at ~/.claude/memory-kit.config.md. Reconfigure? [y/N]"
  No  → Done (load existing config and show current values)
  Yes → Proceed to Phase 2

MISSING → Proceed to Phase 2
```

### Phase 2: Interactive Wizard

Ask each question in sequence, showing the default in brackets:

```
1. Memory base path [~/.claude/projects]:
   Where should memory files be stored? (Default is recommended)

2. Project ID strategy [git-remote]:
   How to identify projects?
   - git-remote: use git origin URL slug (recommended)
   - cwd: use current directory name
   - manual: always prompt

3. Auto-inject at session start? [true]:
   Load MEMORY.md summary into context at each session start?

4. Auto-capture enabled? [true]:
   Detect corrections and discoveries after each response?

5. MCP enabled? [true]:
   Use memory-mcp for semantic search? (Requires npx)
   Set to false to use grep-based fallback.
```

Write the config file based on answers using the template at `config/kit.config.template.md`.

### Phase 3: Project Initialization

Detect project ID from the configured strategy:

```bash
# git-remote strategy
git remote get-url origin 2>/dev/null | sed 's/.*[:/]//' | sed 's/\.git$//' | tr '[:upper:]' '[:lower:]'

# cwd strategy
basename "$(pwd)" | tr '[:upper:]' '[:lower:]'
```

Show detected project ID and ask for confirmation or manual override.

Create memory directory:
```bash
mkdir -p "${MEMORY_BASE_PATH}/${PROJECT_ID}/memory"
```

Initialize MEMORY.md if missing:
```markdown
# Memory — {project-id}

<!-- Auto-maintained by memory-mcp. Do not edit directly. -->
<!-- Use /memory-capture to add entries, /memory-forget to remove. -->
```

### Phase 4: Hook Registration

Add hooks to `~/.claude/settings.json` under the `hooks` key:

```json
{
  "hooks": {
    "SessionStart": [
      {
        "hooks": [{
          "type": "command",
          "command": "bash ~/.claude/plugins/memory-kit/hooks/memory-inject.sh",
          "timeout": 10000
        }]
      }
    ],
    "UserPromptSubmit": [
      {
        "hooks": [{
          "type": "command",
          "command": "bash ~/.claude/plugins/memory-kit/hooks/check-settings.sh"
        }]
      }
    ],
    "Stop": [
      {
        "hooks": [{
          "type": "command",
          "command": "bash ~/.claude/plugins/memory-kit/hooks/auto-capture.sh",
          "timeout": 5000
        }]
      }
    ]
  }
}
```

**Read `settings.json` first** — merge into existing hooks rather than overwriting.

### Phase 5: Confirmation

```
Memory Kit Setup Complete:
  Config: ~/.claude/memory-kit.config.md
  Memory store: {MEMORY_BASE_PATH}/{project-id}/memory/
  Index: {MEMORY_BASE_PATH}/{project-id}/MEMORY.md
  Hooks: SessionStart, UserPromptSubmit, Stop registered

Run /memory-health to audit an existing memory store.
Run /memory-capture to add your first memory.
```

## Anti-patterns

### Overwriting Hooks Without Merging

```
# BAD — replaces all user hooks
Write settings.json with only memory-kit hooks

# GOOD — read first, merge, then write
Read settings.json → merge memory-kit hooks into existing structure → write
```

### Hardcoding Paths

```
# BAD
"command": "bash /Users/gino/.claude/plugins/memory-kit/hooks/memory-inject.sh"

# GOOD — use ${CLAUDE_PLUGIN_ROOT} or derive from config
"command": "bash ${CLAUDE_PLUGIN_ROOT}/hooks/memory-inject.sh"
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| First-time setup | Run full wizard, all phases |
| Config exists, user ran /memory-setup again | Show current values, ask to reconfigure |
| Git remote not found | Fall back to cwd strategy, tell user |
| settings.json doesn't exist | Create it with memory-kit hooks only |
| settings.json exists with other hooks | Merge, never overwrite |
| Project already has memory directory | Skip mkdir, show existing memory count |
| MCP=false selected | Skip mcpServers in plugin.json, use grep fallback only |
