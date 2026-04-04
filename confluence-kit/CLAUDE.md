# Confluence Kit

> **Config:** @~/.claude/confluence-kit.config.md — run `/confluence-setup` if missing.

## Scope
- **Platform:** Confluence — SDLC documentation, ADRs, SDRs, work documentation
- **Covers:** Architecture Decision Records · Software Decision Records · SDLC pages · General work docs
- **Does NOT cover:** Jira sprint work — this kit focuses only on Confluence documentation.

## Authentication

Confluence uses the **Atlassian MCP** via OAuth — no API tokens stored in config.

```
/mcp authenticate atlassian
```

Authenticate once. All `mcp__atlassian__confluence_*` tools then work automatically.

## Always-Active Rules

@~/.claude/rules/confluence-kit/page-conventions.md

## Two-Level Config System

### User / Device Level — `~/.claude/confluence-kit.config.md`
Confluence workspace settings for this machine:
- `CONFLUENCE_BASE_URL` · `CONFLUENCE_DEFAULT_SPACE_KEY`
- `SDLC_CONFLUENCE_SPACE` · `SDLC_PARENT_PAGE`

### Project Level — `.claude/confluence.config.md` (in each repo)
Project-specific Confluence identifiers. Committed to version control:
- `CONFLUENCE_SPACE_KEY` · `SDR_CONFLUENCE_SPACE` · `SDR_PARENT_PAGE` · `ADR_PARENT_PAGE`

Run `/confluence-setup` to configure. Project config **overrides** user config where values overlap.

When config is missing → the `check-settings` hook will prompt automatically.
When Atlassian MCP is not authenticated → run `/mcp authenticate atlassian`.

## Skills Available

### Decision Records
- `/adr` — create, list, view, and deprecate Architecture Decision Records in Confluence
- `/sdr` — create, list, view, and deprecate Software Decision Records in Confluence

### Setup
- `/confluence-setup` — configure user-level and project-level confluence-kit settings
