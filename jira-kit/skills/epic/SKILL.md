---
name: epic
description: >
  Write epics and user stories in Jira. Reads story format from the company SDLC in Confluence,
  then creates a fully structured epic with child stories, acceptance criteria, technical
  descriptions, and Fibonacci story point estimates (1, 2, 3, 5, 8).
  Load this skill when: "write an epic", "create epic", "create stories", "user stories",
  "break down feature", "new epic", "jira epic", "story breakdown", "acceptance criteria".
user-invocable: true
argument-hint: "[epic title or description]"
allowed-tools: Read, mcp__atlassian__jira_create_issue, mcp__atlassian__jira_update_issue, mcp__atlassian__jira_get_issue, mcp__atlassian__confluence_search, mcp__atlassian__confluence_get_page
---

# Epic & Story Creation

## Core Principles

1. **SDLC format is authoritative** — always read the story format from the SDLC Confluence pages before writing. Never invent a format. Read `SDLC_CONFLUENCE_SPACE` + `SDLC_PARENT_PAGE` from `~/.claude/confluence-kit.config.md`.
2. **Epics describe outcomes, not tasks** — "Customer can track their order" not "Add order tracking endpoint".
3. **Stories must be independently deliverable** — each story should be shippable on its own.
4. **Stories > 8 points must be split** — they are too large for one sprint.
5. **Read config first** — `JIRA_PROJECT_KEY` from `.claude/jira.config.md`, `JIRA_BASE_URL` from `~/.claude/jira-kit.config.md`.

## Patterns

### Read Config

```bash
JIRA_BASE_URL=$(grep "^JIRA_BASE_URL=" ~/.claude/jira-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
JIRA_PROJECT_KEY=$(grep "^JIRA_PROJECT_KEY=" .claude/jira.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SDLC_SPACE=$(grep "^SDLC_CONFLUENCE_SPACE=" ~/.claude/confluence-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SDLC_PAGE=$(grep "^SDLC_PARENT_PAGE=" ~/.claude/confluence-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
```

### Step 1 — Read SDLC Story Format

```
mcp__atlassian__confluence_search
  query: "story format OR user story template OR acceptance criteria format"
  spaceKey: {SDLC_SPACE}
→ Get the page, extract the story format fields and AC format
```

### Step 2 — Draft Epic and Stories

From the user's description, produce:

**Epic:**
- Title: outcome-focused business goal
- Description: context, user value, scope boundaries

**Stories (3–7 per epic typically):**
- Title: "As a {role}, I can {action} so that {value}"
- Acceptance criteria: per SDLC format (Given/When/Then or bullet AC)
- Story points: 1, 2, 3, 5, or 8 (flag anything > 5 for discussion)
- Technical description: approach, affected components, non-obvious risks

### Step 3 — Preview and Confirm

Show the full epic + story breakdown. Ask: "Does this look right? Type `yes` to create in Jira, or tell me what to change."

### Step 4 — Create in Jira

```
mcp__atlassian__jira_create_issue
  projectKey: {JIRA_PROJECT_KEY}
  summary: "{epic title}"
  issueType: "Epic"
  description: "{epic description}"

# For each story:
mcp__atlassian__jira_create_issue
  projectKey: {JIRA_PROJECT_KEY}
  summary: "{story title}"
  issueType: "Story"
  description: "{AC + technical description}"
  # link to epic
```

After creation, output the epic URL: `{JIRA_BASE_URL}/browse/{KEY}`

## Anti-patterns

```
# BAD — inventing story format
"Here's the story: ..."

# GOOD — read SDLC first
mcp__atlassian__confluence_search → read format → apply it

# BAD — story points > 8
"This story is 13 points"

# GOOD — split it
"This story is too large. I suggest splitting into: ..."
```

## Execution

Read config. If `JIRA_PROJECT_KEY` is missing → tell user to run `/jira-setup --project`.
If SDLC config is missing → skip SDLC format step and use standard Given/When/Then AC format.
Draft epic + stories → preview → confirm → create.

$ARGUMENTS
