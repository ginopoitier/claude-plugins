# Confluence Kit Config — User / Device Level
<!--
  This file lives at ~/.claude/confluence-kit.config.md
  Run /confluence-setup to configure interactively.

  Authentication is handled by the Atlassian MCP via OAuth — no API token here.
  Run: /mcp authenticate atlassian (same session as jira-kit)
-->

## Confluence
```
CONFLUENCE_BASE_URL=                      # e.g. https://mycompany.atlassian.net/wiki — used for building page URLs
CONFLUENCE_DEFAULT_SPACE_KEY=             # default space key, e.g. ENG (overridable per project)
```

## SDLC
```
SDLC_CONFLUENCE_SPACE=                    # Confluence space key where SDLC lives, e.g. ENG or PLATFORM
SDLC_PARENT_PAGE=                         # Title of the parent SDLC page, e.g. "Software Development Lifecycle"
```
