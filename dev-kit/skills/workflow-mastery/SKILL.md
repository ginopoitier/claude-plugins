---
name: workflow-mastery
description: >
  Plan and orchestrate multi-session development workflows — break epics into sessions,
  track velocity, identify blockers, and guide prioritization across work periods.
  Load this skill when: "workflow", "epic", "multi-session", "/workflow-mastery",
  "plan feature", "break down work", "session planning", "sprint planning", "retrospective".
user-invocable: true
argument-hint: "[plan|next|retrospective|status] [epic description]"
allowed-tools: Read, Write, Edit, Glob, Grep, Bash, TodoWrite, Agent
---

# Workflow Mastery — Multi-Session Development Orchestration

## Core Principles

1. **De-risk early, not late** — Plan the riskiest, most uncertain work for the first session. If the hard part fails or changes the approach, it's better to discover that in session 1 than session 4.
2. **Each session ends in a committable state** — A session that leaves the codebase broken or in an uncommittable state has not achieved its goal. Plan sessions around complete, working units of work.
3. **Never plan more than 2 sessions ahead** — Planning sessions 3-5 in detail before completing session 1 is speculation. Adjust the plan after each session based on actual velocity and discoveries.
4. **Architecture decisions become ADRs** — If the planning phase involves a non-obvious technical choice, create an ADR before implementation begins. This forces structured thinking and documents the decision for the team.
5. **Retrospectives improve estimation** — After each epic, compare planned vs actual sessions. This calibrates future estimates and surfaces systemic planning gaps.

## Patterns

### Workflow File Format

```markdown
# Workflow: Order Cancellation Feature
**Created:** 2026-03-22
**Target:** End of sprint (2026-03-29)
**Status:** In Progress

## Acceptance Criteria
- [ ] Customer can cancel a pending order via API
- [ ] Cancellation is rejected if order is already shipped
- [ ] Cancelled orders appear in order history with cancelled status
- [ ] Order cancellation sends email notification to customer
- [ ] All paths covered by integration tests

## Sessions

### Session 1 — Domain + Core Slice (est. 2h)
- [x] Add Cancel() method to Order aggregate
- [x] Create CancelOrderCommand + handler + endpoint
- [x] Integration tests: happy path + conflict (already cancelled)

### Session 2 — Email Notification (est. 1.5h)
- [ ] Scaffold OrderCancelledEmailNotifier (INotificationHandler)
- [ ] Template email with order summary
- [ ] Integration test: verify email queued on cancellation

### Session 3 — Frontend (est. 2h)
- [ ] Add Cancel button to order detail page (Vue)
- [ ] Handle 409 gracefully (order already cancelled)
- [ ] E2E test

## Risks & Blockers
- Email service (IEmailService) not yet implemented — check if stub exists before session 2

## Dependencies
- IEmailService implementation (Infrastructure) — needed for session 2
- OrderDetail Vue component — check if it exists before session 3
```

### Planning Questions to Ask

```
// GOOD — 3-5 targeted questions to understand scope before planning
1. What is the acceptance criteria? How will you know when it's done?
2. Does this change an existing domain aggregate or create a new one?
3. Are there dependencies on external services (email, payment, messaging)?
4. What's the riskiest or most uncertain part? (plan this for session 1)
5. Are there any security or performance concerns I should account for?

// BAD — asking generic questions that don't improve the plan
"What technology stack do you want to use?"
"Do you have any preferences?"
```

### Session Size Calibration

```
// GOOD — sessions sized for 2-4 hours of focused work
Session 1 — Domain model changes (2h):
  - One aggregate method
  - One command/handler
  - One set of integration tests

// BAD — session that's too large to complete in one sitting
Session 1 — Full feature (8h):
  - Domain model
  - All API endpoints
  - Frontend
  - Email notifications
  - Tests
  → Will leave session in broken state; next session has no context
```

### Codebase Exploration Before Planning

```
// GOOD — spawn an Explore subagent to understand affected areas before planning sessions
Agent (Explore/Haiku): "Find the Order aggregate, existing cancellation-related code,
the email service interface if any, and the Order-related Vue components.
Return a 300-token summary of what exists."

// BAD — planning sessions without understanding what already exists
→ Plan assumes IEmailService doesn't exist → session 2 is wasted discovering it does
→ Plan assumes Order aggregate has no Cancel() → session 1 is half as long as planned
```

## Anti-patterns

### Planning All Sessions in Detail Upfront

```
// BAD — 6 detailed sessions planned before any code is written
Session 1: Domain model
Session 2: API layer
Session 3: Infrastructure
Session 4: Frontend
Session 5: Tests
Session 6: Polish

// Problem: Session 3 assumptions depend on Session 2, which depends on Session 1.
// If Session 1 takes longer or reveals a different approach, sessions 2-6 are wrong.

// GOOD — plan 1-2 sessions, review after each
Plan Session 1: [detailed]
Plan Session 2: [high-level, finalize after session 1]
Sessions 3+: [placeholder — plan after session 2 retrospective]
```

### Skipping Architecture Decision for Significant Choices

```
// BAD — choosing between two significant approaches without documenting the reasoning
"Let's use outbox pattern for the email notification."
→ [implements outbox]
→ [6 months later: why did we use outbox here?]

// GOOD — significant choices become ADRs before implementation
"The email notification could use direct IEmailService call or the outbox pattern.
Let me create an ADR so we document the trade-offs."
→ /adr new use outbox pattern for order email notifications
→ ADR-0018 captures: context, decision, alternatives, consequences
→ Now implementation proceeds with documented rationale
```

### Retrospective Without Estimation Calibration

```
// BAD — retrospective that only notes what happened
"Session 1 was good. Session 2 had some issues. Feature is done."

// GOOD — retrospective that improves future estimation
Planned sessions: 3    Actual sessions: 4.5
Overrun areas:
  - Email integration took 2× longer (IEmailService had a different interface than expected)
  - Frontend had unexpected Vue component compatibility issue

Calibration insight:
  - Infrastructure-touching sessions need a 50% buffer
  - Always check exact interface signatures before planning infrastructure sessions
  → Update MEMORY.md with this calibration note
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Starting a significant feature or epic | `/workflow-mastery plan <epic description>` |
| Beginning a new work session | `/workflow-mastery next` — get the recommended next session |
| Checking epic progress | `/workflow-mastery status` |
| Feature complete — learn from it | `/workflow-mastery retrospective` |
| Architecture choice is non-obvious | Create ADR via `/adr new ...` before planning sessions |
| Session ran long — plan is off | Update workflow.md, plan next 2 sessions only |
| Blocker discovered mid-session | Add to Risks & Blockers in workflow.md, note in session.md |
| Scope grows beyond original plan | Add new sessions, update acceptance criteria, re-estimate |

## Execution

### `/workflow-mastery plan <epic>`
Break a large feature or epic into a structured multi-session plan:

1. **Clarify scope**: Ask 3-5 targeted questions to understand requirements, constraints, and acceptance criteria
2. **Explore codebase**: Spawn an Explore subagent (Haiku) to understand the affected areas — what exists, what needs to be created
3. **Architecture decision**: If the approach is non-obvious or involves a significant trade-off, create an ADR via `/adr new ...` before planning sessions
4. **Create workflow file** at `~/.claude/projects/{sanitized-cwd}/workflow.md` using the format above
5. Show the plan and ask for approval before saving

### `/workflow-mastery next`
Determine the best next task to work on:
1. Read `workflow.md` and `session.md`
2. Check git status for uncommitted work
3. Consider: what's blocking, what's highest value, what's feasible in one session
4. Recommend the next session goal with reasoning
5. Optionally start a session via `/session-management start`

### `/workflow-mastery retrospective`
After a major feature or sprint:
1. Read workflow.md — what was planned vs done
2. Calculate actual vs estimated sessions
3. Identify what took longer and why
4. Generate a retrospective summary and save calibration insights to MEMORY.md
5. Update workflow.md with actual completion data

### `/workflow-mastery status`
Show current epic progress:
- Sessions completed / remaining
- Percentage of acceptance criteria met
- Current blockers
- Estimated sessions to completion

## Planning Principles
- Sessions should be 2-4 hours of focused work
- Each session should produce a working, committable state
- Identify the riskiest/most uncertain work first — de-risk early
- Never plan more than 2 sessions ahead without reviewing progress
- Use the Plan subagent (Opus) for complex architecture decisions

## Integration
- Use with `/session-management` for individual session tracking
- Use with `/adr` to document architecture decisions made during planning
- Use with `/review` before marking sessions complete
