---
name: tech-refinement
description: >
  Technically refine a Jira story: reads the ticket, identifies unknowns, dependencies,
  and risks, breaks it into subtasks with context and time estimates, and creates everything
  in Jira. Uses sprint duration from config for capacity guidance.
  Load this skill when: "refine story", "tech refinement", "technical refinement",
  "break down story", "story breakdown", "subtasks", "estimate story", "refine ticket",
  "prepare for sprint", "definition of ready", "DoR check", "sprint planning".
user-invocable: true
argument-hint: "[TICKET-123 or story title]"
allowed-tools: Read, mcp__atlassian__jira_get_issue, mcp__atlassian__jira_create_issue, mcp__atlassian__jira_update_issue, mcp__atlassian__jira_add_comment, mcp__atlassian__confluence_search, mcp__atlassian__confluence_get_page
---

# Technical Refinement

## Core Principles

1. **Read the SDLC Definition of Ready** — before producing the output, check what your SDLC requires for a story to be sprint-ready. The DoR is the acceptance criterion for refinement.
2. **Unknowns block the sprint** — surface all unknowns explicitly. An unresolved unknown is a sprint risk. Flag them, assign owners, or split the story.
3. **Subtasks have context, not just titles** — a subtask titled "Write unit tests" is useless. Every subtask needs enough context for a developer to start it cold.
4. **Time estimates on subtasks, not stories** — stories are estimated in SP (Fibonacci). Subtasks use remaining time (hours/days). Use `SPRINT_DURATION_DAYS` from config for capacity sanity checks.
5. **Update the story** — after refinement, write the technical description back to the Jira ticket so the team doesn't need to re-derive it.

## Patterns

### Full Refinement Flow

```
/tech-refinement ORD-456

Step 1 — Read config:
  ~/.claude/kit.config.md  → SPRINT_DURATION_DAYS (default 14), SDLC_CONFLUENCE_SPACE
  .claude/project.config.md → JIRA_PROJECT_KEY

Step 2 — Read the story:
  mcp__atlassian__jira_get_issue("ORD-456")
  → description, acceptance_criteria (custom field), technical_description, story_points

Step 3 — Read SDLC:
  mcp__atlassian__confluence_search("definition of ready", space: SDLC_CONFLUENCE_SPACE)
  mcp__atlassian__confluence_search("definition of done", space: SDLC_CONFLUENCE_SPACE)
  → cache DoR and DoD for this session

Step 4 — Analyze and produce refinement output (in-chat first, before creating in Jira):
  - DoR checklist: what's met vs missing
  - Unknowns list with suggested owners
  - Dependencies (other tickets, services, teams)
  - Risks
  - Proposed subtask breakdown

Step 5 — Review with user:
  "Does this breakdown look right? I'll create these subtasks and update the ticket."
  → wait for confirmation or adjustments

Step 6 — Create in Jira:
  For each subtask: mcp__atlassian__jira_create_issue(type: Subtask, parent: ORD-456, ...)
  Update story technical_description field: mcp__atlassian__jira_update_issue(...)
  Add refinement summary comment: mcp__atlassian__jira_add_comment(...)
```

### Refinement Output Format

```markdown
## Technical Refinement: ORD-456 — {Story Title}

### Definition of Ready Check
- [x] Story has description
- [x] Acceptance criteria defined (3 ACs)
- [ ] ⚠️ UI mockups missing — needed before frontend work starts
- [x] Dependencies identified
- [ ] ⚠️ Story points not estimated

### Unknowns
| # | Unknown | Impact | Owner |
|---|---------|--------|-------|
| 1 | Does the existing OrderEvents table have all required state transitions? | High — affects scope | @backend-dev to verify schema |
| 2 | Does SignalR hub exist or needs to be built? | High — adds 2SP if new | @tech-lead to confirm |

### Dependencies
- Blocked by: ORD-123 (OrderEvents schema migration) — must complete first
- Requires: ORD-234 (Auth middleware) — needed for endpoint authorization

### Risks
| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Real-time event delay > 5s under load | Medium | Load test with SignalR before sprint end |

### Proposed Subtasks

| # | Title | Context | Estimate |
|---|-------|---------|----------|
| 1 | Create GET /api/orders/{id}/status endpoint | New endpoint on OrdersController. Returns OrderStatusResponse: { status, history[], estimatedDelivery? }. Must return 404 via Result pattern if order not found. Auth: [Authorize]. | 4h |
| 2 | Extend OrderStatusResponse DTO | Add StatusHistory[] property. Map from OrderEvents table in query handler. No migration needed. | 1h |
| 3 | Add SignalR event emission on order state change | In OrderStateMachine, after each transition, publish OrderStatusChangedEvent to the orders hub. Hub already exists (see OrderHub.cs). | 2h |
| 4 | Update OrderDetail.vue — status display | Add status badge + history timeline. Use existing useOrderUpdates composable for real-time. Design: [link to mockup]. | 3h |
| 5 | Integration tests | WebApplicationFactory test: place order → transition state → assert status endpoint returns new state. Testcontainers for SQL. | 2h |

**Total estimate: 12h**
Sprint capacity check (SPRINT_DURATION_DAYS=14, ~6h/dev/day): fits in ~2 dev-days.

### Technical Description (written back to ticket)
[Summary of above for the Jira field]
```

### Subtask Jira Fields

```
Each subtask created with:
  summary:     "{title}"
  description: |
    **Context:**
    {context paragraph — enough for a developer to start cold}

    **Acceptance:**
    {what "done" looks like for this specific subtask}

    **References:**
    - {links to related code, ADRs, SDRs, design docs}

  timetracking:
    originalEstimate: "{N}h"     # remaining time in hours (or days: "1d 2h")
  parent: {story key}
```

### Estimate Calibration Using Sprint Duration

```
SPRINT_DURATION_DAYS=14
Assume 6 effective hours/dev/day (meetings, reviews, overhead)
Sprint capacity per dev = 14 × 6 = ~84h

If total subtask hours > 1 developer's sprint capacity:
→ Flag as "this story may not fit in one sprint"
→ Suggest splitting the story or flagging as a stretch goal

Rule of thumb:
  1h  = trivial, single file change
  2h  = small, obvious implementation
  4h  = medium, requires some design thought
  8h  = complex, touches multiple layers
  > 8h = consider splitting the subtask
```

## Anti-patterns

### Subtasks Without Context

```
# BAD — developer has no idea where to start
Subtask: "Add unit tests"
Subtask: "Frontend changes"
Subtask: "API update"

# GOOD — enough context to start cold
Subtask: "Integration test: POST /orders → assert OrderCreatedEvent published"
Context: Use AppFactory in OrderTests.cs. Test pattern in tests/Orders/CreateOrderTests.cs.
         Assert via: verify event in outbox table after SaveChanges.
Estimate: 2h
```

### Leaving DoR Gaps Unresolved

```
# BAD — proceed with unknowns unaddressed
Unknown: "Not sure if we need a new DB table"
→ Creates subtasks anyway

# GOOD — surface the unknown as a blocker
Unknown: "Schema question blocks subtask sizing"
→ "This story is NOT ready for sprint until ORD-123 is resolved.
   I've created a spike subtask: 'Verify OrderEvents schema supports all transitions' (1h, @backend-dev)"
```

### Forgetting to Update the Jira Ticket

```
# BAD — refinement lives only in chat, never in Jira
Great analysis produced in conversation. Developer later reads the ticket and finds no technical context.

# GOOD — always write back
mcp__atlassian__jira_update_issue(technical_description: refinement summary)
mcp__atlassian__jira_add_comment("Refined on {date}. Subtasks created: ORD-456-1 through ORD-456-5.")
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Story has no technical description | Derive from description + ACs, ask user to confirm |
| DoR not met | List gaps, create spike subtasks for unknowns, do not mark ready |
| Story estimate missing | Propose estimate based on subtask sum, ask user to confirm |
| Story too large (> 1 sprint) | Flag it, propose split before creating subtasks |
| SDLC DoR/DoD not found in Confluence | Apply defaults, note the gap |
| Subtask > 8h | Split it — flag to user before creating |
| No SPRINT_DURATION_DAYS configured | Default to 14, note assumption |
