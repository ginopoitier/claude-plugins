# Rule: Jira Story Conventions

## DO
- Always read `JIRA_PROJECT_KEY` from `.claude/jira.config.md` before creating issues — never hardcode it
- Read `JIRA_BASE_URL` from `~/.claude/jira-kit.config.md` to build ticket URLs in output
- Read the SDLC story format from Confluence (`SDLC_CONFLUENCE_SPACE` in confluence-kit config) before writing stories — never invent a format
- Use Fibonacci story points: 1, 2, 3, 5, 8 (never 4, 6, 7, or anything over 8)
- Stories > 8 points must be split — they are too large for one sprint
- Include acceptance criteria in every story (Given/When/Then or bullet list per SDLC format)
- Transition tickets to "In Progress" when starting work, "Done" when merged

## DON'T
- Don't create issues without reading the SDLC format first
- Don't estimate in hours — use story points only
- Don't leave subtasks without an assignee and a time estimate
- Don't use raw Jira REST API — always use `mcp__atlassian__jira_*` tools

## Reading Config

```bash
# User config
JIRA_BASE_URL=$(grep "^JIRA_BASE_URL=" ~/.claude/jira-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SPRINT_DAYS=$(grep "^SPRINT_DURATION_DAYS=" ~/.claude/jira-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SPRINT_DAYS=${SPRINT_DAYS:-14}

# Project config
JIRA_PROJECT_KEY=$(grep "^JIRA_PROJECT_KEY=" .claude/jira.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
```

## JQL Patterns

```jql
# Current sprint
project = {KEY} AND sprint in openSprints() ORDER BY priority DESC

# My open items
project = {KEY} AND assignee = currentUser() AND status != Done

# Unrefined stories (no subtasks, no estimate)
project = {KEY} AND issuetype = Story AND "Story Points" is EMPTY AND sprint in openSprints()

# Blocked items
project = {KEY} AND status = "Blocked" AND sprint in openSprints()
```
