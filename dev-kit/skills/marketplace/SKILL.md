---
name: marketplace
description: >
  Browse, install, and update kits from your personal marketplace registry.
  Reads MARKETPLACE_URL from kit config to find available kits, shows what's
  installed vs available, and runs install scripts.
  Load this skill when the user mentions "marketplace", "install kit",
  "update kit", "browse kits", "list kits", or "kit registry".
user-invocable: true
argument-hint: "[list | install <kit-id> | update | search <query>]"
allowed-tools: Read, Bash, Glob
---

# Marketplace

Browse and install kits from your personal registry.

## Core Principles

1. **Registry first** — Always fetch the live registry before listing or installing. Never rely on stale state.
2. **Config-driven URL** — Read `MARKETPLACE_URL` from `~/.claude/kit.config.md`. If missing, ask the user to run `/kit-setup` and set it.
3. **Dry-run by default on install** — Show what will be installed before proceeding. Ask for confirmation.
4. **Non-destructive** — Installation never deletes existing files. It overwrites with newer versions.

## Patterns

### Reading the marketplace URL

```
1. Read ~/.claude/kit.config.md
2. Find line: MARKETPLACE_URL=https://...
3. If blank or missing → tell user to set it in /kit-setup
4. Fetch registry.json from that URL
```

### Listing available kits

Run the marketplace installer in list mode:
```bash
bash ~/.claude/marketplace-cache/install-from-marketplace.sh
```

Or if the installer isn't cached, fetch and parse the registry directly:
```bash
curl -sSL "$MARKETPLACE_URL" | jq '.kits[] | {id, version, description}'
```

Display as a table:
```
Available kits in your marketplace:
  dev-kit    v0.2.0   .NET Clean Architecture + Vue/TypeScript developer toolkit
  kit-maker  v1.0.0   Meta-kit for creating and auditing Claude Code kits

Installed:
  dev-kit    v0.2.0   ✓ up to date
  kit-maker  —        not installed
```

### Installing a kit

```bash
# Step 1 — dry run first
curl -sSL "$INSTALL_SCRIPT_URL" | bash -s -- --dry-run

# Step 2 — confirm with user
# Step 3 — install
curl -sSL "$INSTALL_SCRIPT_URL" | bash
```

Or if the kit is already cloned locally:
```bash
bash /path/to/kit/install.sh
```

### Checking installed versions

Installed versions are tracked in `~/.claude/marketplace-installed/`:
```bash
ls ~/.claude/marketplace-installed/
# dev-kit.version    kit-maker.version
cat ~/.claude/marketplace-installed/dev-kit.version
# 0.2.0
```

### Updating all kits

```bash
bash install-from-marketplace.sh --update-all
```

## Anti-patterns

### Don't hardcode the registry URL

```
# BAD — hardcoded URL in skill
REGISTRY="https://hardcoded-url/registry.json"

# GOOD — always from user config
REGISTRY=$(grep "^MARKETPLACE_URL=" ~/.claude/kit.config.md | cut -d= -f2-)
```

### Don't skip the dry-run confirmation

```
# BAD — silently installs
Install dev-kit? Running installer...

# GOOD — show what will happen first
[dry-run preview]
  Will install: 16 rules, 51 skills, 11 agents, 7 hooks
  Will skip: kit.config.md (already exists)
Proceed? (yes/no)
```

## Decision Guide

| Command | Action |
|---------|--------|
| `/marketplace` or `/marketplace list` | Fetch registry, show available + installed |
| `/marketplace install dev-kit` | Dry-run then confirm then install |
| `/marketplace update` | Check installed versions vs registry, update outdated |
| `/marketplace search dotnet` | Filter registry kits by tag or keyword |
| `MARKETPLACE_URL` not set | Tell user to run `/kit-setup` section 7 |
| Kit not in registry | Tell user to install directly via `install.sh` in the kit repo |
| Install script not found | Warn and show manual install instructions |
