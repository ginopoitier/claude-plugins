# Jira Kit

> **Config:** @~/.claude/jira-kit.config.md — run `/jira-setup` if missing.

## Scope
- **Platform:** Jira — sprint work, epics, stories, refinement, standup
- **Covers:** Epic creation · Story writing · Technical refinement · Daily standup prep · Sprint health
- **Does NOT cover:** Confluence documentation — this kit focuses only on Jira sprint work.

## Authentication

Jira uses the **Atlassian MCP** via OAuth — no API tokens stored in config.

```
/mcp authenticate atlassian
```

Authenticate once. All `mcp__atlassian__jira_*` tools then work automatically.

## Always-Active Rules

@~/.claude/rules/jira-kit/story-conventions.md

## Two-Level Config System

### User / Device Level — `~/.claude/jira-kit.config.md`
Jira identity and sprint settings for this machine:
- `JIRA_BASE_URL` · `SPRINT_DURATION_DAYS`

### Project Level — `.claude/jira.config.md` (in each repo)
Project-specific Jira identifiers. Committed to version control:
- `JIRA_PROJECT_KEY` · `JIRA_BOARD_ID`

Run `/jira-setup` to configure. Project config **overrides** user config where values overlap.

When config is missing → the `check-settings` hook will prompt automatically.
When Atlassian MCP is not authenticated → run `/mcp authenticate atlassian`.

## Skills Available

### Sprint Work
- `/epic` — write a Jira epic with child stories, ACs, and story point estimates
- `/story` — write a single story from a business case or technical case; interviews for ACs, epic link, and estimate
- `/tech-refinement` — technically refine a story: subtasks, estimates, DoR check
- `/standup` — morning standup brief: sprint health, blockers, PRs needing review

### Setup
- `/jira-setup` — configure user-level and project-level jira-kit settings
