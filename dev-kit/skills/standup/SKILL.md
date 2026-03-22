---
name: standup
description: >
  Morning standup prep — surfaces PRs waiting for your review, tickets that moved
  overnight, sprint blockers, and team velocity. Gives you a 2-minute briefing
  before the standup call so you walk in informed.
  Load this skill when: "standup", "daily standup", "morning prep", "daily prep",
  "what happened overnight", "sprint status", "what needs my attention",
  "what prs need review", "team status", "sprint health", "blockers today",
  "what did the team complete", "velocity check".
user-invocable: true
argument-hint: "[--mine | --team | --sprint]"
allowed-tools: Bash, mcp__atlassian__jira_search_issues, mcp__atlassian__jira_get_issue, mcp__atlassian__confluence_search
---

# Standup Prep

## Core Principles

1. **Read, don't interrupt** — Pull status from Jira and VCS instead of asking the team. The standup brief is compiled before the meeting, not during it.
2. **Tech lead focus** — Highlight what needs YOUR action: PRs awaiting your review, blocked tickets only you can unblock, stories stalled without a comment.
3. **Sprint health at a glance** — Show velocity (done vs committed), days remaining, and any stories at risk of not completing. A flat "3 tickets in progress" is useless; "37 SP remaining with 6 days left at 3 SP/day pace — at risk" is actionable.
4. **Time-boxed output** — The brief must be readable in under 2 minutes. If there are more than 5 "needs attention" items, something is systemically wrong — flag that instead.
5. **Requires Jira configured** — Dormant when `PM_PROVIDER=none`. On home machines, offer a lighter git-only summary instead.

## Patterns

### Full Standup Brief Flow

```
/standup

Step 1 — Read config:
  ~/.claude/kit.config.md → PM_PROVIDER, JIRA_BASE_URL, SPRINT_DURATION_DAYS
  .claude/project.config.md → JIRA_PROJECT_KEY

Step 2 — Query Jira (parallel):
  a) My open items:
     mcp__atlassian__jira_search_issues(
       "project = {KEY} AND assignee = currentUser() AND sprint in openSprints()"
     )

  b) Blockers in the sprint:
     mcp__atlassian__jira_search_issues(
       "project = {KEY} AND sprint in openSprints() AND status = 'Blocked'"
     )

  c) Stories moved to Done since yesterday:
     mcp__atlassian__jira_search_issues(
       "project = {KEY} AND sprint in openSprints() AND status changed to Done after -1d"
     )

  d) Stories not updated in 2+ days (stale):
     mcp__atlassian__jira_search_issues(
       "project = {KEY} AND sprint in openSprints() AND status != Done AND updated < -2d"
     )

  e) Sprint totals (for velocity):
     mcp__atlassian__jira_search_issues(
       "project = {KEY} AND sprint in openSprints()"
     ) → count by status, sum story points

Step 3 — Query VCS for PRs needing review:
  git log --oneline --remotes --not --tags --since="24 hours ago"
  Or: fetch open PRs from VCS MCP if available

Step 4 — Compose brief
```

### Standup Brief Format

```markdown
## Standup Brief — {DATE} {TIME}
*{PROJECT_NAME} | Sprint {N} | Day {X} of {SPRINT_DURATION_DAYS}*

---

### Needs Your Attention
1. **PR review:** feature/ORD-456-order-status (Ali, 18h ago) — 3 files, medium risk
2. **Blocked:** ORD-478 "Payment webhook handler" — waiting on Platform team (PLT-234)
   → Do you need to chase the Platform team today?
3. **Stale:** ORD-471 "Export to CSV" — no update in 3 days, assigned to Ben
   → Worth a quick check-in?

---

### Sprint Health
| Status | Stories | Points |
|--------|---------|--------|
| Done | 4 | 18 SP |
| In Progress | 5 | 21 SP |
| To Do | 3 | 11 SP |
| Blocked | 1 | 5 SP |
| **Total committed** | **13** | **55 SP** |

Days remaining: 6 of 14
Points remaining: 37 SP | Needed pace: ~6.2 SP/day | Actual pace: ~3 SP/day
⚠ **At risk** — current pace won't complete the sprint commitment

---

### Yesterday's completions
- ORD-461: Add order search by customer email (Ben, 5SP)
- ORD-463: Fix null reference in payment handler (Sam, 2SP)

---

### Your items today
| Ticket | Title | Status | Notes |
|--------|-------|--------|-------|
| ORD-456 | Order status endpoint | In Progress | PR raised, waiting for review |
| ORD-479 | Refine ORD-480 and ORD-481 | To Do | Refinement session at 2pm |

---

### Blockers to raise
- ORD-478 blocked on Platform team — escalate if no response by EOD
```

### Lightweight Mode (no Jira, git-only)

```
When PM_PROVIDER=none or Jira config is missing:

/standup → git-based brief only

- Recent commits (last 24h): git log --since="24h" --oneline --all
- Changed files: git diff --name-only HEAD~5..HEAD
- Active branches: git branch -r --sort=-committerdate | head -10
- Output: simple list of what changed, who committed, what might need review
```

### Sub-command Modes

`/standup --mine` — Only your tickets. Faster. Use when the sprint is healthy and you just need your personal plan.

`/standup --sprint` — Full sprint view: all tickets, all assignees, velocity chart. Use before sprint review or when reporting sprint status to the product owner.

## Anti-patterns

### Pulling Too Much Detail

```
# BAD — reading every ticket in full
mcp__atlassian__jira_get_issue(each ticket individually)
→ Slow, verbose, buries the signal in noise

# GOOD — search query returns summary-level data
mcp__atlassian__jira_search_issues("project = KEY AND sprint in openSprints()")
→ Fast, shows what matters: status, assignee, last update
Only fetch full issue detail for blocked and stale items
```

### Standup Brief That Replaces the Standup

```
# BAD — 20-minute read covering every ticket, every comment
The brief is longer than the standup itself. The team stops reading it.

# GOOD — 2-minute scan, surfaces only items that need attention
"Needs Your Attention" section has at most 5 items.
If there are more, flag: "Sprint has 8 items needing attention — systemic issue?"
```

### Ignoring Sprint Health

```
# BAD — only showing individual ticket status
"You have 3 tickets in progress"

# GOOD — showing sprint trajectory
"37 SP remaining, 6 days left, current pace 3 SP/day, needed pace 6.2 SP/day → at risk"
This tells the tech lead whether to escalate, re-scope, or stay the course
```

## Decision Guide

| Scenario | Mode |
|----------|------|
| Quick personal prep | `/standup --mine` |
| Full team status before standup | `/standup` (default) |
| Sprint review prep | `/standup --sprint` |
| No Jira configured (`PM_PROVIDER=none`) | Git-based brief — note Jira would add more |
| Sprint is healthy | Skip the health section, focus on attention items |
| Sprint is at risk | Lead with health section, flag to product owner |
| No PRs to review | Remove that section — don't show empty sections |
| Blockers found | Always include in output with suggested escalation action |
| Stale tickets found | List with days since last update, suggested follow-up |
