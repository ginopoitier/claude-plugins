# Technical Refinement — Guide

## What Refinement Is For

Refinement converts a story from "understood by product" to "ready for a developer to start without asking questions". The output of a good refinement session is:

1. A story the team has collectively understood
2. Unknowns surfaced and either resolved or converted to spike subtasks
3. Technical subtasks with enough context to start cold
4. Time estimates on each subtask
5. A definition of done the developer can self-check against

Refinement is not design. You're not making architecture decisions in refinement — those go in SDRs. You're translating an agreed design into executable tasks.

## The Unknowns Inventory

The most important output of refinement is the unknowns list. An unknown that enters a sprint unresolved is a sprint risk.

Categories of unknowns:

```
BLOCKING — must resolve before estimation or task creation
  "We don't know if the OrderEvents table has all the state transitions we need"
  → Cannot estimate the data access layer without this

NON-BLOCKING — can be resolved during the sprint, but flag it
  "Not sure if we'll need rate limiting on the new endpoint"
  → Can implement without it, add later if needed

ASSUMPTION — we're proceeding as if X is true; document it
  "Assuming the SignalR hub can handle 500 concurrent connections at current capacity"
  → If the assumption is wrong, the risk is known

SPIKE NEEDED — unknown can only be resolved by investigation
  → Create a spike subtask: timebox 2-4h, output is a written answer
```

Always assign an owner to each unknown. An unowned unknown stays unresolved.

## Writing Good Subtasks

A subtask title is not a subtask. The goal is that a developer who has never seen this story can read the subtask and start working within 5 minutes.

### Required subtask content

```markdown
**Summary:** Create GET /api/orders/{id}/status endpoint

**Context:**
New endpoint on the Orders feature group. Returns the current order status and
the last 5 status history entries. Follows the existing pattern in OrderEndpoints.cs.
Requires auth (existing [Authorize] policy). Returns 404 via Result pattern if order
not found.

**Implementation notes:**
- Query: GetOrderStatusQuery (new) → GetOrderStatusHandler
- Response DTO: OrderStatusResponse { Status, EstimatedDelivery?, History[] }
- History entries: map from OrderEvents table — no new table needed
- Endpoint path: GET /api/orders/{id}/status
- Reference: GetOrderQuery.cs for the established query pattern

**Done when:**
- Endpoint returns 200 with correct data for existing orders
- Returns 404 for unknown order IDs
- CancellationToken propagated through query chain
- AsNoTracking() on the EF query
- Integration test: happy path + 404 case

**Estimate:** 4h
```

The "Done when" section is what makes subtasks self-checkable. Developers should be able to verify completion without asking the tech lead.

## Time Estimation for Subtasks

Subtask estimates are in hours (remaining time), not story points.

```
Reference scale:
  1h — trivial change, single file, clear implementation
  2h — small, obvious, well-established pattern
  4h — standard complexity, requires some design thought
  8h — complex, multiple layers, or significant new pattern

Over 8h → split the subtask. A developer should not go a full day without
           being able to check something off.

Common traps:
  Under-estimating test writing (add 30-50% of implementation time for tests)
  Under-estimating "figuring it out" time for patterns the developer hasn't used before
  Forgetting to count code review turnaround time in capacity
```

### Capacity sanity check

```
Sprint capacity per developer:
  SPRINT_DURATION_DAYS × effective_hours_per_day

  Effective hours ≈ 6h/day (accounts for meetings, reviews, context switching)
  14-day sprint: 14 × 6 = 84h per developer

If total story subtask hours > 50h:
→ Story will likely span two sprints or consume a full developer
→ Flag to product owner before sprint planning

If total subtask hours < 4h:
→ Story may be too small — consider combining with a related story
→ Or ensure the estimate is realistic (missing edge cases?)
```

## Dependency Mapping

Before finalizing subtasks, map dependencies:

```
Internal dependencies (within the team):
  "Subtask 3 (frontend) cannot start until Subtask 1 (API) is complete and in staging"
  → Sequence the subtasks, note the dependency explicitly

External dependencies (other teams or services):
  "The authentication middleware change from the Platform team (PLT-234) must be
   deployed before our endpoint can use the new auth policy"
  → Add as a blocker to the story in Jira (not just a comment)

Database migration dependencies:
  "ORD-233 (add EstimatedDelivery column) must be merged first"
  → Block the story on ORD-233, ensure ORD-233 is in the same sprint
```

## Definition of Done for a Story

The DoD is the team-level agreement on what "done" means. It lives in the SDLC. Common elements:

```
Technical DoD (usually team-standard):
  - [ ] Code reviewed and approved (≥1 approver)
  - [ ] All ACs verified by developer
  - [ ] Integration tests pass in CI
  - [ ] No new compiler warnings
  - [ ] No secrets or hardcoded config values
  - [ ] Merged to main

Quality DoD (team-specific):
  - [ ] New API endpoints documented (OpenAPI)
  - [ ] Breaking changes flagged and migration path documented
  - [ ] Runbook updated if infrastructure changed
  - [ ] Security review for auth-touching changes

Product DoD (stakeholder-specific):
  - [ ] Demoed to product owner (or async approval)
  - [ ] Release notes drafted if user-facing
```

Always check what your SDLC says — these items may be mandatory, not optional.

## When a Story Fails Refinement

A story fails refinement when it has blocking unknowns that cannot be resolved in the refinement session. Common outcomes:

```
Outcome 1: Return to backlog
  The story needs more information from product (missing ACs, unclear scope)
  → Return to product owner with specific questions. Schedule a follow-up.

Outcome 2: Add a spike to the sprint
  The unknown can only be resolved by investigation
  → Create a spike subtask (2-4h) for the upcoming sprint
  → The parent story is NOT included in the sprint until the spike is resolved

Outcome 3: Split the story
  The story is too large, or the unclear part is separable
  → Define the clear, estimable part as a new story for the sprint
  → The unclear part becomes a separate story or spike

Outcome 4: Accept with documented assumption
  The unknown is non-blocking and the team agrees to proceed
  → Document the assumption in the technical description
  → The developer flags it immediately if the assumption proves wrong
```

## Refinement Anti-patterns

**Refinement without reading the story first**
The tech lead arrives at refinement having not read the ticket. The team spends 20 minutes reading it together. Schedule 10 minutes of pre-refinement reading.

**Creating subtasks without knowing the implementation approach**
Tasks are created before the team agrees on how it will be implemented. The developer then re-designs the approach during implementation, invalidating the estimates and tasks.

**Ignoring the non-happy-path**
Subtasks are written for the happy path only. Error cases, edge cases, and rollback scenarios are discovered during implementation, adding unplanned work.

**Not writing context in subtasks**
Subtasks have only a title. The developer must read the whole story and ask the tech lead to understand what to do. This defeats the purpose of refinement.

**Estimating before unknowns are resolved**
The team estimates a story with blocking unknowns. The estimate turns out to be wrong when the unknowns are resolved. Always resolve blockers before estimating.
