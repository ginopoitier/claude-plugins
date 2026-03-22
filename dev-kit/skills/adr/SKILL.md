---
name: adr
description: >
  Create, list, view, and manage Architecture Decision Records (ADRs).
  Load this skill when: "adr", "architecture decision", "decision record",
  "document decision", "technical decision", "ADR-", "/adr".
user-invocable: true
argument-hint: "[new|list|show|deprecate] [decision title or number]"
allowed-tools: Read, Write, Glob, Bash
---

# ADR — Architecture Decision Records

## Core Principles

1. **One decision per record** — ADRs capture a single architectural choice with its context, the alternatives that were considered, and the consequences. Never combine multiple decisions in one ADR.
2. **Immutable history** — Accepted ADRs are never edited after the decision is made. When a decision changes, create a new ADR that supersedes the old one and mark the original as Deprecated.
3. **Context first** — The most valuable part of an ADR is explaining *why*, not *what*. Anyone reading it a year later must understand the forces and constraints that led to the decision.
4. **Create automatically when making non-obvious choices** — If you are choosing a library, rejecting a common pattern, or making a trade-off with known consequences, an ADR should be created or suggested to the user.
5. **Suggest during planning** — When `/workflow-mastery plan` identifies significant architectural decisions, proactively suggest creating an ADR before implementation begins.

## Patterns

### When to Create an ADR

```
// GOOD — decisions that warrant an ADR
Choosing MediatR over a custom mediator implementation
Using Result<T> pattern over exceptions for business failures
Rejecting microservices in favor of modular monolith for this team size
Using HybridCache over IDistributedCache directly
Choosing Neo4j for a specific data relationship problem

// BAD — decisions that do NOT need an ADR
Which variable name to use
Choosing between two nearly equivalent NuGet packages
Deciding file/folder structure (covered by project conventions)
```

### ADR File Format

```markdown
# ADR-0042 — Use HybridCache for Distributed Caching

**Status:** Accepted
**Date:** 2026-03-22
**Deciders:** Tech Lead, Backend Team

## Context
Our product catalog API is under increasing read load. Fetching product
data hits SQL Server on every request. We need a caching layer that works
in both single-instance dev and multi-instance production deployments.

## Decision
We will use .NET 9's `HybridCache` (Microsoft.Extensions.Caching.Hybrid)
which provides a two-level cache: L1 in-memory per instance, L2 Redis
in production. This replaces ad-hoc `IMemoryCache` usage.

## Alternatives Considered
1. `IMemoryCache` only — rejected: doesn't work across multiple instances
2. `IDistributedCache` (Redis) only — rejected: higher latency on L1 hits
3. Custom caching middleware — rejected: unnecessary complexity

## Consequences
- Positive: Consistent API across dev (no Redis) and prod (Redis)
- Positive: Automatic stampede protection via locking
- Negative: Requires Redis in production (additional infrastructure)
- Negative: HybridCache is newer — less community examples available

## References
- [Microsoft HybridCache docs](https://learn.microsoft.com/...)
- Supersedes: None
```

### Numbering and Naming Convention

```
docs/decisions/
  0001-use-clean-architecture.md
  0002-use-result-pattern-over-exceptions.md
  0003-reject-microservices-monolith-first.md
  0042-use-hybridcache-for-distributed-caching.md

// GOOD — slug is descriptive, lowercase, hyphenated
0005-use-mediatr-for-cqrs.md

// BAD — slug is vague or omits context
0005-caching.md
0005-mediatr.md
```

## Anti-patterns

### Editing Accepted ADRs

```markdown
// BAD — modifying an existing accepted ADR when the decision changes
ADR-0005 (Accepted): Use Redis for caching
→ [edit file to say "Use HybridCache instead"]

// GOOD — create a new ADR that supersedes the old one
ADR-0005 (Deprecated): Use Redis for caching
  → "Superseded by ADR-0042"
ADR-0042 (Accepted): Use HybridCache for distributed caching
  → "Supersedes ADR-0005"
```

### Writing "What" Without "Why"

```markdown
// BAD — describes only what was decided, not why
## Decision
We will use MediatR for command/query dispatching.

// GOOD — explains forces, constraints, and reasoning
## Context
Multiple endpoints need to trigger the same business logic (web + background jobs).
We want to avoid coupling endpoints directly to service classes to keep testability high.

## Decision
We will use MediatR because it provides a clean dispatch mechanism with pipeline
behaviors for cross-cutting concerns (validation, logging, transactions) that
work identically from HTTP endpoints and background job runners.
```

### Creating ADRs After the Fact

```markdown
// BAD — writing ADRs months after the decision was made, based on memory
(Produces thin, inaccurate records that miss the real constraints)

// GOOD — creating the ADR at decision time, before or during implementation
→ Use /workflow-mastery plan to identify decisions that need ADRs upfront
→ Create the ADR as the first step, before writing code
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Choosing between two architectures | Create ADR before implementation |
| Adopting a new library or framework | Create ADR with alternatives considered |
| Rejecting a common/expected pattern | Create ADR explaining why |
| Minor implementation choice | No ADR needed |
| Previous decision no longer fits | New ADR superseding the old one |
| Planning a feature with `/workflow-mastery` | Suggest ADR for any non-obvious choices |
| ADR status unknown | `/adr list` then `/adr show <number>` |
| Team disagreement on approach | Create ADR to force structured thinking |

## Execution

### `/adr new <title>`
Create a new ADR:
1. Determine the next ADR number by counting existing files in `docs/decisions/`
2. Create `docs/decisions/{NNNN}-{slug}.md` using the template above
3. Pre-fill with the title; leave body for user to fill
4. If the user provides enough context in the command (e.g., `/adr new use Redis for caching because memcache won't scale`), auto-fill the Context and Decision sections from that context
5. Ask the user for any sections not yet filled:
   - Context (what forces this decision?)
   - Decision (what was chosen?)
   - Alternatives considered
   - Consequences

### `/adr list`
List all ADRs:
- Read all files in `docs/decisions/`
- Show: number, title, status (Accepted/Deprecated/Proposed)
- Group by status

### `/adr show <number>`
Display a specific ADR:
- Read `docs/decisions/{NNNN}-*.md`
- Format and display in full

### `/adr deprecate <number> [superseded-by]`
Mark an ADR as deprecated:
- Update the Status field to `Deprecated`
- If superseded-by given, add "Superseded by ADR-{N}" to the record

## ADR Directory
Default: `docs/decisions/` relative to project root. Create the directory if it does not exist.
