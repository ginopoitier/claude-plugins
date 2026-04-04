# jira-kit

Jira sprint toolkit for Claude Code — epics, story writing, technical refinement, and daily standup prep via the Atlassian MCP.

## What's Included

| Category | Skills |
|----------|--------|
| Sprint Work | `/epic` — write a Jira epic with child stories, ACs, and story point estimates |
| Sprint Work | `/tech-refinement` — technically refine a story: subtasks, estimates, DoR check |
| Sprint Work | `/standup` — morning standup brief: sprint health, blockers, PRs needing review |
| Setup | `/jira-setup` — configure user-level and project-level settings |

## Install

```bash
bash install.sh
```

Then authenticate and configure:

```
/mcp authenticate atlassian
/jira-setup
```

## Authentication

jira-kit uses the **Atlassian MCP** via OAuth — no API tokens stored in config files.

```
/mcp authenticate atlassian
```

Authenticate once. All `mcp__atlassian__jira_*` tools work automatically.

## Configuration

| Level | File | Contains |
|-------|------|----------|
| User / Device | `~/.claude/jira-kit.config.md` | Jira base URL, sprint duration |
| Project | `.claude/jira.config.md` | Project key, board ID |

## Requirements

- Claude Code 1.0.0+ with MCP support
- Atlassian MCP authenticated via OAuth (`/mcp authenticate atlassian`)
