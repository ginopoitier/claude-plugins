# confluence-kit

Confluence documentation toolkit for Claude Code — Architecture Decision Records, Software Decision Records, SDLC pages, and work documentation via the Atlassian MCP.

## What's Included

| Category | Skills |
|----------|--------|
| Decision Records | `/adr` — create, list, view, and deprecate Architecture Decision Records |
| Decision Records | `/sdr` — create, list, view, and deprecate Software Decision Records |
| Setup | `/confluence-setup` — configure user-level and project-level settings |

## Install

```bash
bash install.sh
```

Then authenticate and configure:

```
/mcp authenticate atlassian
/confluence-setup
```

## Authentication

confluence-kit uses the **Atlassian MCP** via OAuth — no API tokens stored in config files.

```
/mcp authenticate atlassian
```

Authenticate once. All `mcp__atlassian__confluence_*` tools then work automatically.

## Configuration

| Level | File | Contains |
|-------|------|----------|
| User / Device | `~/.claude/confluence-kit.config.md` | Confluence base URL, default space key, SDLC space |
| Project | `.claude/confluence.config.md` | Space key, ADR parent page, SDR parent page |

## Requirements

- Claude Code 1.0.0+ with MCP support
- Atlassian MCP authenticated via OAuth (`/mcp authenticate atlassian`)
