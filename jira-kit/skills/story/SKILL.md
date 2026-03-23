---
name: story
description: >
  Write a single Jira story from a business case or technical case.
  Interviews for acceptance criteria, story type, and epic link, then creates
  the story in Jira via Atlassian MCP in the company SDLC format.
  Load this skill when: "write a story", "create a story", "new story", "user story",
  "business case", "technical story", "add story to epic", "story from case",
  "story to jira", "create ticket", "story ticket".
user-invocable: true
argument-hint: "[business or technical description]"
allowed-tools: Read, mcp__atlassian__jira_create_issue, mcp__atlassian__jira_get_issue, mcp__atlassian__jira_search, mcp__atlassian__confluence_search, mcp__atlassian__confluence_get_page
---

# Story Writing

## Core Principles

1. **Business vs technical — pick the right shape** — Business stories follow "As a {role}, I can {action} so that {value}". Technical stories (infrastructure, refactors, spike) use "As a developer, I need to {task} so that {outcome}". Never force a business shape onto a technical case.
2. **SDLC format is authoritative** — Read the story template from Confluence before writing acceptance criteria. Never invent a format.
3. **One story, one slice** — A story covers one deliverable, shippable unit. If the user's description contains multiple independent deliverables, split and confirm before creating.
4. **Stories > 8 points must be split** — Any estimate above 8 means the scope is too large for a single sprint story.
5. **Epic link is always resolved** — If the user names an epic, look it up via `mcp__atlassian__jira_search` to get the key before creating the story. Never accept an epic key from the user without verifying it exists.

## Patterns

### Read Config

```bash
JIRA_BASE_URL=$(grep "^JIRA_BASE_URL=" ~/.claude/jira-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
JIRA_PROJECT_KEY=$(grep "^JIRA_PROJECT_KEY=" .claude/jira.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SDLC_SPACE=$(grep "^SDLC_CONFLUENCE_SPACE=" ~/.claude/confluence-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SDLC_PAGE=$(grep "^SDLC_PARENT_PAGE=" ~/.claude/confluence-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
```

### Step 1 — Classify the Story Type

Ask (or infer from the argument):

```
Business story: adds user-facing value, changes product behaviour
  → Title: "As a {role}, I can {action} so that {value}"

Technical story: infrastructure, refactor, spike, CI/CD work
  → Title: "As a developer, I need to {task} so that {technical outcome}"
```

If ambiguous, ask: "Is this a business story (adds user-visible value) or a technical story (internal improvement/infra)?"

### Step 2 — Read SDLC Acceptance Criteria Format

```
mcp__atlassian__confluence_search
  query: "acceptance criteria format OR story template"
  spaceKey: {SDLC_SPACE}
→ Extract the AC format (Given/When/Then, bullet AC, or custom)
```

If SDLC config is missing → fall back to Given/When/Then format.

### Step 3 — Interview for Missing Information

Ask only for what was not provided in `$ARGUMENTS`:

```
For business stories:
  - Who is the user/role? (if not obvious)
  - What are the acceptance criteria? (2–5 ACs)
  - Are there edge cases or negative paths to cover?
  - Which epic does this belong to? (name or key)
  - Estimate: 1, 2, 3, 5, or 8 story points?

For technical stories:
  - What is the technical goal?
  - How will you know it's done? (definition of done)
  - Is there a risk or dependency to note?
  - Which epic or initiative does this relate to?
  - Estimate: 1, 2, 3, 5, or 8 story points?
```

### Step 4 — Resolve Epic Link

```
mcp__atlassian__jira_search
  jql: "project = {JIRA_PROJECT_KEY} AND issuetype = Epic AND summary ~ '{epic name}'"
→ Confirm the epic key (e.g. PROJ-12)
→ If no match: offer to create without epic link or re-check the name
```

### Step 5 — Draft and Preview

Show the full story before creating:

```markdown
**Type:** Business Story / Technical Story
**Title:** As a customer, I can cancel a pending order so that I don't have to call support

**Acceptance Criteria:**
- Given I have a pending order, when I cancel it, then its status changes to Cancelled
- Given I have a shipped order, when I attempt to cancel, then I receive an error explaining it cannot be cancelled
- Given I cancel an order, then I receive a confirmation email

**Story Points:** 3
**Epic:** PROJ-12 — Order Management

**Technical Description:** *(optional, for dev context)*
Cancel action calls DELETE /api/orders/{id}. Domain: Order.Cancel() already exists.
Need CancelOrderCommand + handler. Email via existing INotificationService.
```

Ask: "Does this look right? Type `yes` to create in Jira, or tell me what to change."

### Step 6 — Create in Jira

```
mcp__atlassian__jira_create_issue
  projectKey: {JIRA_PROJECT_KEY}
  summary: "{story title}"
  issueType: "Story"
  description: "{formatted AC + technical description}"
  storyPoints: {estimate}
  # link to epic: epicLink or parent field depending on Jira config
```

Output: `Created: {JIRA_BASE_URL}/browse/{KEY}`

## Anti-patterns

### Inventing the AC Format

```
# BAD — using a format not in the SDLC
"Acceptance Criteria:
 - The user can cancel"

# GOOD — reading the SDLC format first
mcp__atlassian__confluence_search → read format → apply it exactly
```

### Accepting Vague Descriptions Without Clarifying

```
# BAD — writing a story from "improve the orders page"
"As a user, I can improve the orders page so that it is better"
→ Meaningless ACs, no clear done state

# GOOD — interview first
"What specific change do you want on the orders page? What will the user be able to do that they can't do today? How will you know it's done?"
```

### Forcing Business Shape on Technical Work

```
# BAD — technical refactor written as a user story
"As a user, I can benefit from a refactored OrderService so that it is cleaner"

# GOOD — technical story shape
"As a developer, I need to refactor OrderService to use the Result pattern
 so that error handling is consistent with the rest of the codebase"
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Description clearly has one deliverable | Write one story |
| Description contains multiple independent deliverables | Split → confirm with user before creating |
| Story estimate is > 8 | Flag and split before creating |
| Epic name given but not found in Jira | Search again with partial match; if still missing, offer to proceed without epic link |
| No SDLC Confluence config | Fall back to Given/When/Then AC format |
| No project config (JIRA_PROJECT_KEY missing) | Tell user to run `/jira-setup --project` |
| Ambiguous business vs technical | Ask before drafting |
| User wants multiple stories at once | Use `/epic` instead — it handles bulk breakdown |

## Execution

Read config. Classify the story type (business or technical). Read SDLC AC format from Confluence.
Interview for any missing information. Resolve the epic link. Draft the full story and preview.
On confirmation, create in Jira and return the ticket URL.

$ARGUMENTS
