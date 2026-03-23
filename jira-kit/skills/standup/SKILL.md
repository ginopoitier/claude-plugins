---
name: standup
description: >
  Morning standup prep — surfaces sprint health, your open tickets, blockers,
  and PRs waiting for review. Gives you a 2-minute briefing before the standup call.
  Load this skill when: "standup", "daily standup", "morning prep", "daily prep",
  "what happened overnight", "sprint status", "what needs my attention",
  "what prs need review", "team status", "sprint health", "blockers today".
user-invocable: true
argument-hint: "[--mine | --team | --sprint]"
allowed-tools: Bash, mcp__atlassian__jira_search_issues, mcp__atlassian__jira_get_issue
---

# Standup Prep

## Core Principles

1. **Read, don't interrupt** — pull status from Jira instead of asking the team.
2. **Three sections only** — Yesterday / Today / Blockers. Keep it scannable.
3. **Blockers are explicit** — a blocker is something that stops forward progress. Slow things are not blockers.
4. **Sprint health at a glance** — show remaining points vs days left.

## Patterns

### Read Config

```bash
JIRA_PROJECT_KEY=$(grep "^JIRA_PROJECT_KEY=" .claude/jira.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
JIRA_BASE_URL=$(grep "^JIRA_BASE_URL=" ~/.claude/jira-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
```

### JQL Queries

```jql
-- My open items in current sprint
project = {KEY} AND assignee = currentUser() AND sprint in openSprints() AND status != Done

-- Recently moved to Done (yesterday)
project = {KEY} AND assignee = currentUser() AND status changed to Done after -1d

-- Blockers in sprint
project = {KEY} AND sprint in openSprints() AND status = "Blocked"

-- Unassigned or stale items
project = {KEY} AND sprint in openSprints() AND status not in (Done, "Won't Do") AND updated < -2d
```

### Output Format

```
## Standup Brief — {DATE}

### Yesterday
- {ticket}: {what was done} → {status}

### Today
- {ticket}: {planned work}

### Blockers
- {blocker description} — waiting on: {person/team}
  (or: No blockers)

### Sprint Health
- {X} points remaining | {Y} days left | {Z} points completed
- At-risk: {tickets not started that should be}
```

### Modes

| Flag | Scope |
|------|-------|
| `--mine` (default) | Your tickets only |
| `--team` | All team members' tickets in sprint |
| `--sprint` | Sprint-level health: velocity, burndown, at-risk items |

## Execution

Read config. Run JQL queries for the appropriate mode. Format the brief. Output it — no Jira writes during standup prep.

$ARGUMENTS
