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

1. **SDLC format is authoritative** — always read the story format from the SDLC Confluence pages before writing. Never invent a format.
2. **Epics describe outcomes, not tasks** — an epic title should state the business goal, not the technical implementation. "Customer can track their order" not "Add order tracking endpoint".
3. **Stories must be independently deliverable** — each story should be shippable on its own. If it can't be deployed without another story, split or merge them.
4. **Acceptance criteria are testable conditions** — each AC is a pass/fail assertion, not a description. "Given X, when Y, then Z" or "The system must..." format.
5. **Estimates are Fibonacci to 8SP** — 1, 2, 3, 5, or 8. Anything larger gets split. Never estimate in hours at story level.

## Patterns

### Interaction Flow

```
/epic "Customer order tracking"

Step 1 — Read SDLC story format:
  mcp__atlassian__confluence_search("user story template OR story format", space: SDLC_CONFLUENCE_SPACE)
  → Cache format for all stories in this session

Step 2 — Gather epic context (ask user):
  - What problem does this solve? (business context)
  - Who are the users?
  - What are the success criteria for the whole epic?
  - Are there any known technical constraints?
  - Rough size: small (2-3 stories), medium (4-6), large (7+)?

Step 3 — Draft epic and propose story breakdown:
  Present: Epic title + description + proposed stories list
  Ask user: "Does this breakdown look right? Add, remove, or rename before I create."

Step 4 — For each approved story, gather or infer:
  - Description (using SDLC format)
  - Acceptance criteria (3-5 ACs per story)
  - Technical description (implementation notes, API changes, DB impact)
  - Story point estimate (1/2/3/5/8)

Step 5 — Create in Jira:
  1. Create epic: mcp__atlassian__jira_create_issue(type: Epic, ...)
  2. For each story: mcp__atlassian__jira_create_issue(type: Story, parent: epicKey, ...)
  3. Report: created epic + list of story keys and titles
```

### Epic Structure in Jira

```
Epic fields to populate:
  summary:     "Customer Order Tracking"
  description: |
    ## Goal
    Allow customers to see the real-time status of their order from placement to delivery.

    ## Business Value
    Reduces support tickets related to order status by an estimated 30%.
    Directly requested by 3 enterprise customers in last quarter's feedback.

    ## Success Criteria
    - Customer can track order status without contacting support
    - Order events visible within 5 seconds of state change
    - Works on mobile and desktop

    ## Out of Scope
    - Push notifications (separate epic)
    - Historical order archive (existing feature)

  labels:      [from project conventions]
  fixVersion:  [if known]
```

### Story Structure in Jira

```
Story fields to populate:
  summary:     "View current order status on order detail page"

  description: |
    [SDLC format — e.g. As a / I want / So that, or whatever the SDLC specifies]
    As a logged-in customer,
    I want to see the current status of my order on the order detail page,
    So that I know when to expect delivery without contacting support.

  acceptance_criteria: |   ← custom field
    **AC1:** Given a placed order, when I navigate to the order detail page,
    then I see the current status (Placed, Processing, Shipped, Delivered).

    **AC2:** Given a shipped order, when I view the order detail page,
    then I see the estimated delivery date.

    **AC3:** Given a delivered order, when I view the order detail page,
    then the status shows "Delivered" and the actual delivery date.

  technical_description: |  ← custom field
    - New GET /api/orders/{id}/status endpoint
    - Extend OrderResponse DTO with StatusHistory[]
    - No new DB tables required — derives from existing OrderEvents table
    - Frontend: update OrderDetail.vue to poll or use SignalR for live updates

  story_points: 3           ← Fibonacci: 1, 2, 3, 5, 8
```

### Fibonacci Estimation Guide

```
1 SP — trivial change, no unknowns (e.g., display a new field that already exists in API)
2 SP — small, well-understood (e.g., add a filter to an existing list endpoint)
3 SP — medium, some complexity (e.g., new endpoint + frontend component)
5 SP — complex, multiple layers touched or some unknowns
8 SP — large with significant unknowns → consider splitting before committing

> 8 SP → DO NOT estimate. Split the story first.
```

## Anti-patterns

### Writing Technical Epics

```
# BAD — describes implementation, not outcome
Epic: "Add WebSocket connection to order service and implement real-time event emission"

# GOOD — describes the business outcome
Epic: "Customer can track their order in real-time"
```

### Acceptance Criteria That Aren't Testable

```
# BAD — vague, can't be pass/failed
AC1: The order status should be shown clearly.
AC2: Performance should be good.

# GOOD — specific, testable conditions
AC1: Given a shipped order, when I load the order detail page,
     then the status "Shipped" and a tracking number are visible within 2 seconds.
AC2: Given 1000 concurrent users viewing orders, when each loads the detail page,
     then p95 response time is under 500ms.
```

### Skipping SDLC Format Lookup

```
# BAD — using default format without checking
Story: "As a user, I want to..."
→ Company may use Jobs-to-be-Done or BDD format — this creates inconsistency

# GOOD — always check first
mcp__atlassian__confluence_search("story format", space: SDLC_CONFLUENCE_SPACE)
→ Company uses: "In order to [goal] / As a [role] / I want [action]" — use that
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| New feature request | `/epic "title"` → interactive flow |
| Have Jira ticket already | `/epic --from TICKET-123` → read + enrich existing |
| Story estimate > 8SP | Split before creating — ask user how |
| SDLC story format not found | Use As a/I want/So that, note it to user |
| Story with no clear AC | Ask user — don't guess acceptance criteria |
| Epic too vague | Ask for business value + success criteria before proceeding |
| No JIRA_PROJECT_KEY set | Tell user to run `/project-setup` |
