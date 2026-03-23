---
name: confluence-setup
description: >
  Configure confluence-kit user-level and project-level settings. Sets up Confluence
  base URL, default space, SDLC page location, and decision record parent pages.
  Load this skill when: "confluence setup", "configure confluence", "confluence-setup",
  "setup confluence kit", "confluence config", "initialize confluence config".
user-invocable: true
argument-hint: "[--user | --project | --both]"
allowed-tools: Bash, Read, Write
---

# Confluence Setup

## Core Principles

1. **Auth is MCP OAuth** — same session as jira-kit. After writing config, show `/mcp authenticate atlassian` if not already done.
2. **Two levels** — user (`~/.claude/confluence-kit.config.md`) is per-machine; project (`.claude/confluence.config.md`) is per-repo and committed.
3. **SDLC config is user-level** — SDLC lives in one company space, not per-project.
4. **Decision record parents are project-level** — each project has its own ADR/SDR page hierarchy.
5. **Preview before write** — show the full config block and get confirmation before writing.

## Patterns

### User-level config (`--user`)

Ask section by section:

**Section 1 — Confluence Workspace**
- Confluence base URL? (e.g. `https://mycompany.atlassian.net/wiki`)
- Default space key? (e.g. `ENG` — used when no project-level space is set)

**Section 2 — SDLC Location**
- Do you have your company SDLC documented in Confluence? (yes/no)
  - If yes: space key where SDLC lives? (e.g. `ENG` or `PLATFORM`)
  - Parent SDLC page title? (e.g. `Software Development Lifecycle`)

Write to `~/.claude/confluence-kit.config.md`.

Show Atlassian MCP auth reminder if not already shown by jira-setup.

### Project-level config (`--project`)

Ask:
- Confluence space key for this project? (e.g. `ORD`)
- SDR parent page title? (default: `Software Decision Records`)
- ADR parent page title? (default: `Architecture Decisions`)
- SDR space key? (leave blank to use the project space key)

Write to `.claude/confluence.config.md`, then commit.

## Decision Guide

| Scenario | Command |
|----------|---------|
| First time on a new machine | `/confluence-setup --user` |
| Starting a new project | `/confluence-setup --project` |
| Config missing, hook triggered | Run `/confluence-setup` — detects which level is missing |

## Execution

Detect which levels are missing. Run the appropriate sections. Preview, confirm, write.

$ARGUMENTS
