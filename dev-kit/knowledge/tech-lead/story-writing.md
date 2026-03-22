# Story Writing — Guide

## What a Story Is (and Isn't)

A story is a **unit of deliverable value** — something you can ship that a user or stakeholder can observe and care about. It is not a task, a technical step, or a chunk of work for a developer's convenience.

The acid test: could you demo this story to a product stakeholder and have them say "yes, that's what I wanted"? If not, it's probably a task masquerading as a story.

## Story Size

Stories entering a sprint should be estimable in Fibonacci points (1–8). If a story cannot be estimated with confidence, it is not ready.

```
1 SP — trivial, fully understood, single layer touched (e.g. add a field to an existing API response)
2 SP — small, well-understood, maybe two layers (e.g. new filter on an existing list)
3 SP — standard feature, some design needed, 2-3 layers (e.g. new endpoint + frontend component)
5 SP — complex, multiple layers, some unknowns resolved at refinement
8 SP — large, significant cross-cutting change, or multiple known unknowns

> 8 SP → split before estimating. A story that can't fit in one sprint is an epic, not a story.
```

The most common mistake: 8SP stories that are actually 3-4 stories bundled together because splitting felt awkward. Splitting is always worth the awkwardness.

## Writing Good Acceptance Criteria

Acceptance criteria are the contract between the story writer and the developer. Good ACs are:

**Testable** — each AC is a pass/fail condition, not a description
**Specific** — references actual data, states, or user actions
**Minimal** — covers the behaviour, not the implementation

### AC Formats

**Given/When/Then** (BDD) — best for user-facing behaviour:
```
Given a logged-in customer with an order in "Shipped" status,
When they navigate to the order detail page,
Then they see the current status "Shipped" and an estimated delivery date.
```

**The system must / shall** — best for non-functional or system requirements:
```
The system must return a 404 response if the order ID does not exist.
The system must not expose other customers' order data.
Response time must be under 200ms at p95 for up to 500 concurrent users.
```

**Checklist** — best for verification-oriented stories (bug fixes, migrations):
```
- [ ] The duplicate email validation error message is consistent across web and mobile
- [ ] Existing users with duplicate emails can still log in
- [ ] The error is logged at Warning level with the user ID
```

### How Many ACs?

3-5 ACs per story is the sweet spot. Fewer than 3 usually means the story isn't fully understood. More than 7 usually means the story is too large or the ACs are describing implementation.

### AC Anti-patterns

```
# BAD — describes implementation, not behaviour
AC: Use AsNoTracking() in the query handler.

# BAD — too vague to test
AC: The page should look good on mobile.

# BAD — duplicates existing behaviour (don't document what's already working)
AC: Existing orders are not affected by this change.

# GOOD — testable, specific, behaviour-focused
AC: Given an order with 0 items, when I submit the form, then I see
    "Your cart is empty" and the order is not created.
```

## Epic vs. Story vs. Task

```
Epic   = A business goal that takes multiple sprints to achieve
         "Customers can track their orders in real time"

Story  = A unit of deliverable value within an epic; fits in one sprint
         "View current order status on the order detail page"
         "Receive email notification when order ships"

Task/Subtask = A technical step to implement a story; not independently valuable
               "Create GET /api/orders/{id}/status endpoint"
               "Add SignalR event emission to OrderStateMachine"
               "Write integration tests for status endpoint"
```

Common mistake: writing tasks as stories. If the "story" title starts with "Create", "Add", "Implement", "Write", "Migrate", or "Refactor" — it's probably a task. Ask: "Who benefits, and how do they notice?"

## Technical Description Field

The technical description bridges the gap between the business-facing story and the implementation. It should contain:

```markdown
## Technical Description

**Approach:**
[One paragraph: how this will be implemented at a high level]

**API changes:**
- New: GET /api/orders/{id}/status → OrderStatusResponse
- Modified: OrderResponse — add StatusHistory[] field

**Database impact:**
- No new tables required
- New index on OrderEvents(OrderId, CreatedAt) for the history query

**Affected services/components:**
- Orders API (OrdersEndpoints, GetOrderStatusQuery)
- OrderDetail.vue (frontend)
- OrderHub.cs (SignalR event emission)

**Known risks / unknowns:**
- SignalR hub capacity under load — needs load test before release
```

Don't write the technical description until after technical refinement — it's the output of refinement, not an input.

## Definition of Ready Checklist

Before a story enters a sprint it must have:

- [ ] Clear, testable acceptance criteria (3-5 ACs)
- [ ] Story points estimated (1-8; split if > 8)
- [ ] Epic link set
- [ ] Dependencies identified (blocking tickets, external teams)
- [ ] Technical description written (from refinement)
- [ ] No open unknowns that block implementation
- [ ] UI mockups linked (for frontend stories)
- [ ] Assignee set or left for sprint planning

A story that fails the DoR is not blocked — it goes back to refinement.

## Splitting Stories

When a story is too large, split on the **value axis**, not the **technical axis**.

```
# BAD — splitting by technical layer (each piece is a task, not a story)
Story A: Create the API endpoint for order status
Story B: Create the frontend component for order status
Story C: Write tests for order status

→ None of these are independently shippable. The user sees nothing until all three are done.

# GOOD — splitting by scenario or user path
Story A: View order status on the order detail page (web)
  → Ships the full stack for the basic status display
Story B: Real-time status updates via SignalR
  → Adds live updates on top of the static display
Story C: Order status in the mobile app
  → Extends to the mobile channel

→ Story A is shippable and valuable on its own. B and C enhance it.
```

Common split patterns:
- **Happy path first** → edge cases and error states as follow-up stories
- **Read before write** → display existing data before adding edit capability
- **Core feature before enhancements** → basic version ships, enhanced version follows
- **One user type at a time** → admin flow first, then customer flow
