---
name: obsidian-setup
description: >
  Configure obsidian-kit vault path and folder structure. Sets up the personal
  knowledge base location for note writing from dev sessions and project work.
  Load this skill when: "obsidian setup", "configure obsidian", "obsidian-setup",
  "setup obsidian kit", "obsidian config", "vault path", "initialize obsidian config".
user-invocable: true
argument-hint: "[--user | --project]"
allowed-tools: Bash, Read, Write
---

# Obsidian Setup

## Core Principles

1. **Vault path must exist** — verify the directory exists before writing config.
2. **User config is per-machine** — vault paths differ between home and work machines.
3. **Project config is optional** — only needed to pin a specific vault subfolder per repo.
4. **Preview before write** — show the full config block and get confirmation.

## Patterns

### User-level config (`--user`)

Ask:
- Full path to your Obsidian vault? (e.g. `D:/Notes` or `~/Obsidian/MyVault`)

Verify it exists:
```bash
[[ -d "$VAULT_PATH" ]] && echo "Found" || echo "WARNING: Directory does not exist"
```

Ask:
- Subfolder for dev notes and session journals? (default: `Dev`)
- Subfolder for project documentation? (default: `Projects`)

Write to `~/.claude/obsidian-kit.config.md`.

### Project-level config (`--project`)

Ask:
- Vault-relative folder for this project's notes? (e.g. `Projects/OrderService`)
  Leave blank to use `{OBSIDIAN_PROJECTS_FOLDER}/{project-name}`.

Write to `.claude/obsidian.config.md`.

## Decision Guide

| Scenario | Command |
|----------|---------|
| First time on a machine | `/obsidian-setup --user` |
| Pin a vault folder per project | `/obsidian-setup --project` |
| Config missing, hook triggered | `/obsidian-setup` — detects which level is missing |

## Execution

Detect which levels are missing. Run sections, verify vault path exists, preview, confirm, write.

$ARGUMENTS
