# Software Decision Records — Guide

## What Makes a Good SDR

An SDR is not a changelog entry or a ticket comment. It's a **durable record of why** a decision was made, written so that someone who joins the team in 18 months can understand the decision without needing to ask anyone.

The test: could a new senior engineer read this SDR and understand why the team chose Option C over Option A, what was at stake, and what the trade-offs are? If yes, it's a good SDR.

## When to Write One

Write an SDR when the decision meets **two or more** of these criteria:
- Affects more than one service or team
- Involves a technology choice that's hard to reverse
- Has significant cost, performance, or security implications
- Will be questioned by future team members
- Involves rejecting a common/obvious approach in favour of something less standard

**Skip the SDR** for:
- Obvious choices with no real alternatives ("use Git for version control")
- Internal implementation details that don't cross boundaries
- Reversible decisions with low cost to change

## Anatomy of Each Section

### Context
The hardest section to write well. Don't describe the decision — describe the **situation** that made a decision necessary.

```markdown
## Context

# BAD — describes the solution, not the situation
We needed a caching layer so we decided to evaluate Redis vs Memcached.

# GOOD — describes the forces and constraints that existed
The Orders service is handling 2,000 requests/second at peak with p99 latency
of 800ms. Profiling shows 60% of that is repeated database reads for product
catalogue data that changes at most once per hour. We need sub-50ms p99 for
the catalogue endpoint to meet our SLA renewal with Enterprise customers.
We are running on AWS and have an existing ECS cluster. The team has no
prior experience with managed caching services.
```

Good context answers:
- What is the current situation that's causing pain?
- What are the constraints? (team, budget, existing infrastructure, deadlines)
- What happens if we make no decision? (sometimes valid — document it)
- Who does this decision affect?

### Options Considered
List minimum 2. Include the obvious "do nothing" or "keep current" option when it was genuinely considered.

For each option:
- Give it a name, not just "Option A"
- State the actual pros and cons as they applied to **your** context
- Don't be balanced for the sake of it — if an option was clearly wrong, say so

```markdown
### Option 1: ElastiCache (Redis) — managed AWS service
**Pros:**
- Zero operational overhead — AWS handles patching, failover, scaling
- Native integration with our existing ECS service discovery
- Redis data structures (sorted sets) will enable the real-time leaderboard
  feature planned for Q3 without architectural changes

**Cons:**
- Additional AWS cost ~$180/month for a multi-AZ r7g.large instance
- Team has no Redis experience — estimated 1 sprint ramp-up time

### Option 2: In-process MemoryCache
**Pros:** Zero cost, no infrastructure, team knows it well

**Cons:**
- Cache is per-instance — with 8 ECS tasks, each task caches independently.
  On a cache miss each task hits the DB independently (8x load vs 1x with shared cache).
- Cannot survive a deploy — cache warm-up spike on every deployment.
- Does not solve the p99 problem at current scale.
```

### Decision
One clear sentence stating what was chosen. Then the **reasoning** — not a repeat of the pros, but the specific factors that tipped the balance.

```markdown
## Decision

We will use AWS ElastiCache (Redis) with a multi-AZ r7g.large instance.

The deciding factors were:
1. In-process MemoryCache fails at our current instance count — cache invalidation
   cannot be coordinated across 8 tasks, which defeats the purpose.
2. The Q3 leaderboard feature has a hard dependency on sorted sets. Choosing Redis
   now avoids a second caching decision in 6 months.
3. The cost ($180/month) is covered under the existing platform budget approved
   in the Q2 planning cycle.

We rejected self-managed Redis on EC2 because the operational overhead outweighs
the cost saving given our team size (3 backend engineers).
```

### Consequences
Be honest about the negatives. Pretending a decision has no downsides makes the SDR useless.

```markdown
## Consequences

**Positive:**
- p99 latency drops from 800ms to ~45ms for catalogue endpoints (measured in staging)
- Removes 60% of DB read load, extending DB headroom by ~6 months

**Negative / Trade-offs:**
- $180/month new infrastructure cost
- Team needs Redis training — allocated 3 days in Sprint 22
- Cache invalidation logic adds complexity to the product publish flow

**Risks:**
- ElastiCache failover time is ~30-60s — catalogue endpoint will return stale data
  during a node failure. Acceptable given the data updates only hourly.
  If we add write-through caching later, this needs re-evaluation.
```

## SDR Lifecycle

```
Proposed  → written but not yet reviewed/accepted by the team
Accepted  → team has reviewed and agreed to proceed
Active    → decision is in production
Deprecated → decision was reversed or superseded
Superseded → explicitly replaced by another SDR (link to new one)
```

Transition rules:
- A tech lead can write a Proposed SDR and then accept it after team review
- Don't self-accept immediately — give the team 24-48h to review async
- When reversing a decision, always create a new SDR (the old one stays as history)

## Numbering and Storage

SDRs are numbered sequentially per project: `SDR-001`, `SDR-002`, etc.
They live in Confluence under the project's SDR parent page (configured in `.claude/project.config.md`).

Page title format: `SDR-{NNN}: {Title in sentence case}`

Link SDRs to Jira epics when the decision arose from a feature. Add the SDR link as a Jira comment so it's discoverable.

## Common Mistakes

**Too short:** An SDR with 3 bullet points per section is a stub, not a record. Future readers cannot reconstruct the reasoning.

**Written after the fact:** SDRs written weeks after the decision lose the authentic context and option analysis. Write while the decision is fresh.

**Options that were never real:** Listing "Option A: do nothing" when doing nothing was never genuinely on the table wastes the reader's time. Only include options that were actually considered.

**Missing the "why not":** The most valuable part of an SDR is often why the obvious choice was rejected. Make this explicit.
