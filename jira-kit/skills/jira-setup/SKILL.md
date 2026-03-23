---
name: jira-setup
description: >
  Configure jira-kit user-level and project-level settings. Sets up Jira base URL,
  sprint duration, and project key. Load this skill when:
  "jira setup", "configure jira", "jira-setup", "setup jira kit",
  "jira config", "jira kit config", "initialize jira config".
user-invocable: true
argument-hint: "[--user | --project | --both]"
allowed-tools: Bash, Read, Write
---

# Jira Setup

## Core Principles

1. **Auth is MCP OAuth** — no API tokens. After writing config, show `/mcp authenticate atlassian`.
2. **Two levels** — user (`~/.claude/jira-kit.config.md`) is per-machine; project (`.claude/jira.config.md`) is per-repo and committed.
3. **Project config is committed** — all developers share the same project key.
4. **Preview before write** — show the full config block and get confirmation before writing.

## Patterns

### User-level config (`--user`)

Ask:
- Jira base URL? (e.g. `https://mycompany.atlassian.net`)
- Sprint duration in days? (default: `14`)

Write to `~/.claude/jira-kit.config.md`.

After saving, show:
```
Atlassian MCP authentication (covers both Jira and Confluence):
  /mcp authenticate atlassian
```

### Project-level config (`--project`)

Detect from the repo if possible:
```bash
# Try to infer project key from existing branch names
git branch -a | grep -oE '[A-Z]{2,6}-[0-9]+' | head -1 | grep -oE '^[A-Z]+'
```

Ask:
- Jira project key? (e.g. `ORD` or `CUST`)
- Jira board ID? (numeric, optional — find it in the Jira board URL; used for standup sprint queries)

Write to `.claude/jira.config.md`, then commit:
```bash
git add .claude/jira.config.md
git commit -m "chore: add jira-kit project config"
```

## Decision Guide

| Scenario | Command |
|----------|---------|
| First time on a new (work) machine | `/jira-setup --user` |
| Starting a new project | `/jira-setup --project` |
| Config missing, hook triggered | Run `/jira-setup` — detects which level is missing |
| Both levels missing | `/jira-setup --both` |

## Execution

Detect which levels are missing, run the appropriate setup sections. Show preview, confirm, write.

$ARGUMENTS
