---
name: review
description: >
  Code review of staged changes, a PR number, or specific files.
  Supports two modes: --mentoring (constructive, explains why, coaching tone for juniors)
  and --gatekeeper (terse, blockers/warnings only, strict verdict for critical paths).
  Checks architecture, CQRS, Result pattern, EF Core, logging, Vue/TS, and SDLC compliance.
  Load this skill when: "review", "code review", "pr review", "review this", "review changes",
  "check pr", "review branch", "review my code", "tech lead review", "gatekeeper", "mentoring".
user-invocable: true
argument-hint: "[--mentoring|--gatekeeper] [pr-number | file-path | branch]"
allowed-tools: Read, Glob, Grep, Bash, mcp__atlassian__confluence_search, mcp__atlassian__confluence_get_page, mcp__atlassian__jira_get_issue
---

# Code Review

## Core Principles

1. **Mode determines tone, not depth** — both modes check the same things. Mentoring explains the why and coaches. Gatekeeper states the verdict and the fix, no elaboration.
2. **Blockers block** — a blocker means the PR cannot merge as-is. Architecture violations, security holes, broken Result pattern, missing tests on business logic are always blockers regardless of mode.
3. **SDLC is part of the review** — if `SDLC_CONFLUENCE_SPACE` is configured, fetch the PR process requirements and check compliance. A PR that violates the SDLC is a blocker.
4. **Tech lead lens has two extra dimensions** — architectural direction (does this move the codebase forward or sideways?) and team patterns (does this introduce a pattern inconsistent with the rest of the codebase?).
5. **Reference exact locations** — every finding cites the file path and line number. No generic observations.

## Patterns

### Mode Selection

```
/review                      → auto-detect: check PR size + branch name for context
/review --mentoring 42       → PR #42, coaching tone (junior dev, explaining why)
/review --gatekeeper         → staged changes, strict gate (critical path, senior review)
/review --mentoring OrderService.cs

Auto-detect rules:
  Branch has junior dev's name / "draft" / "learning" → default mentoring
  Branch is release/* or hotfix/* → default gatekeeper
  PR is large (> 300 lines changed) → ask user which mode
  Otherwise → default gatekeeper
```

### Gather the Diff

```
| Input | Action |
|-------|--------|
| Nothing | git diff HEAD |
| PR number | Fetch diff via GitHub/Bitbucket VCS MCP using VCS_ORG from kit config |
| File path | Read file directly |
| Branch name | git diff main...{branch} |

If JIRA ticket found in branch name (e.g. feature/ORD-456-order-status):
  mcp__atlassian__jira_get_issue("ORD-456")
  → Load acceptance criteria to verify implementation completeness
```

### SDLC Check (if configured)

```
If SDLC_CONFLUENCE_SPACE is set:
  mcp__atlassian__confluence_search("pull request process OR PR checklist", space: SDLC_CONFLUENCE_SPACE)
  → Read the PR process requirements
  → Add SDLC compliance section to report

Common SDLC PR requirements to check:
  - Branch naming convention
  - Linked Jira ticket
  - PR description present
  - Required reviewers assigned
  - Tests required for new features
  - Documentation updated for API changes
```

### Review Checklist

**Architecture 🔴 (always blockers)**
- Dependencies pointing the wrong direction (Infrastructure → Application, etc.)
- Business logic in endpoints — endpoints only call `sender.Send()`
- DbContext injected outside Application/Infrastructure

**CQRS / MediatR 🔴**
- Queries that mutate state
- Commands returning full entities (should return IDs or Result)
- Missing `CancellationToken ct` in handlers
- `IMediator` used instead of `ISender` / `IPublisher`

**Result Pattern 🔴**
- Exceptions thrown for expected failures (not found, conflict, validation)
- Result errors not mapped to ProblemDetails at endpoint layer
- Result failures silently swallowed

**Security 🔴**
- Hardcoded secrets or connection strings
- Missing `[Authorize]` on endpoints that require auth
- String-interpolated SQL (injection risk)
- Sensitive data logged

**EF Core 🟡**
- Missing `AsNoTracking()` on read-only queries
- `.Include()` loading full graph instead of `.Select()` projection (N+1)
- `SaveChanges()` instead of `SaveChangesAsync(ct)`

**Logging 🟡**
- String interpolation in log messages (use structured properties)
- Missing logs for significant business events

**Vue / TypeScript 🟡**
- `any` type usage
- Raw API calls in component `<script setup>` (should go through api layer)
- SignalR listeners without `onUnmounted` cleanup

**Tech Lead Lens 🔵**
- Does this change move the codebase in the right direction, or introduce drift?
- Does this introduce a new pattern inconsistent with the rest of the codebase?
- Are there breaking changes to public APIs or contracts?
- Would a less experienced developer understand and maintain this code?
- Is the scope right — doing too much or too little in one PR?

**SDLC 🔴 (if configured)**
- PR process requirements from Confluence

### Mentoring Mode Report

```markdown
## Code Review — {branch or PR} [Mentoring Mode]

### Summary
[What this change does in 2-3 sentences. Acknowledge the positive intent.]
[Jira ticket: {KEY}-{N} if found]

### Blockers 🔴
1. **OrderEndpoints.cs:45** — Business logic in endpoint
   **What's wrong:** The discount calculation on line 45 is directly in the endpoint handler.
   **Why it matters:** This makes the logic untestable and breaks Clean Architecture —
   business rules belong in the domain or application layer, not the presentation layer.
   **How to fix:** Move `CalculateDiscount()` to `Order.ApplyDiscount()` on the domain entity,
   then call it from the `ApplyDiscountHandler`. Here's an example of the pattern used elsewhere:
   `src/Orders/Domain/Order.cs:78` — see how `Cancel()` is implemented on the entity.

### Warnings 🟡
1. **GetOrderHandler.cs:23** — Missing `AsNoTracking()`
   **What's wrong:** The query loads a tracked entity but never modifies it.
   **Why it matters:** EF Core tracks all loaded entities by default, which adds
   memory overhead and processing time for no benefit on read-only operations.
   **How to fix:** Add `.AsNoTracking()` after `db.Orders` on line 23.
   Rule of thumb: if the handler name starts with `Get`, it needs `AsNoTracking()`.

### Suggestions 🔵
1. Consider extracting the validation logic to a `CreateOrderValidator` (FluentValidation)
   — the inline checks in the handler will grow over time. See existing validators in
   `src/Products/Application/Commands/CreateProduct/CreateProductValidator.cs`.

### Positives ✅
- Clean use of the Result pattern — errors correctly surface as ProblemDetails
- Good structured logging on the happy path
- Tests cover both success and not-found cases — well done

### Verdict
- [ ] Changes required (blockers above must be resolved)
```

### Gatekeeper Mode Report

```markdown
## Code Review — {branch or PR} [Gatekeeper Mode]

### Summary
[What this change does in 1 sentence.]
[Jira: {KEY}-{N}]

### Blockers 🔴
1. **OrderEndpoints.cs:45** — Business logic in endpoint. Move to domain entity or handler.
2. **OrderService.cs:12** — DbContext injected in Domain project. Remove — Domain has no EF dependency.

### Warnings 🟡
1. **GetOrderHandler.cs:23** — `AsNoTracking()` missing on read query.
2. **orderStore.ts:67** — SignalR `.on('OrderUpdated')` has no `.off()` in `onUnmounted`.

### Suggestions 🔵
1. Extract inline validation to `CreateOrderValidator`.

### SDLC 🔴
1. No Jira ticket linked in branch name — required by PR process.

### Verdict
- [x] Changes required — 2 architecture blockers + 1 SDLC blocker must be resolved before merge.
```

## Anti-patterns

### Generic Observations Without Location

```
# BAD — no location, no fix
"There's business logic in the wrong layer and some EF Core issues."

# GOOD — exact location, exact fix
"OrderEndpoints.cs:45 — discount calculation belongs in Order.ApplyDiscount() (domain entity).
 GetOrderHandler.cs:23 — add .AsNoTracking() after db.Orders."
```

### Harsh Mentoring Tone

```
# BAD — demoralizes rather than teaches
"This is wrong. DbContext should never be in the domain layer.
 You clearly haven't read the Clean Architecture rules."

# GOOD — constructive and educational
"Line 12 injects DbContext into the Domain project — this creates a dependency from Domain
 to Infrastructure, which reverses the dependency direction Clean Architecture requires.
 Move this to the handler in the Application layer. Here's the pattern used elsewhere: ..."
```

### Skipping the Tech Lead Lens

```
# BAD — only checks individual code quality, misses systemic issues
All files individually correct — verdict: Approved

# GOOD — also checks architectural direction
Individual code: ✅
Tech Lead Lens: ⚠️ This PR introduces a service layer (IOrderService) not used elsewhere.
  The codebase uses handlers directly. This creates two patterns for the same thing.
  Recommend: Remove the service layer and use the handler directly, as in ProductEndpoints.cs.
```

## Decision Guide

| Scenario | Mode |
|----------|------|
| Junior developer's PR | `--mentoring` — explain every finding |
| Senior developer's PR | `--gatekeeper` — terse, trust them to understand |
| Release branch / hotfix | `--gatekeeper` — strict gate, no noise |
| Draft / learning PR | `--mentoring` — encourage, explain patterns |
| Own code review | `--gatekeeper` — find the blockers fast |
| Auto-detect ambiguous | Ask user which mode before reviewing |
| SDLC check fails | Always a blocker, regardless of mode |
| Verdict: approved with minor changes | State exactly what "minor" means |

## Execution

$ARGUMENTS
