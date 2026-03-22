---
name: 80-20-review
description: >
  Review strategy: prioritize the 20% of code that causes 80% of issues.
  Blast radius scoring, checkpoint schedules, and Top 10 critical checks for
  data access, security, and concurrency. Complements /review (which does the
  actual review) — this skill decides WHERE to focus and HOW DEEP to go.
  Load this skill when: "blast radius", "review priorities", "review strategy",
  "review depth", "what should I review first", "checkpoint review",
  "critical path review", "how deep to review".
user-invocable: true
argument-hint: "[PR, file, or area to review]"
allowed-tools: Read, Write, Grep, Glob, Bash
---

# 80/20 Review

## Core Principles

1. **Review at checkpoints, not continuously** — Constant review interrupts flow. Schedule reviews at natural breakpoints: post-implementation, pre-PR, post-integration, and post-deploy.

2. **Focus on data access, security, concurrency, integration** — These are the 20% of code areas that cause 80% of production incidents. A missing `CancellationToken` is more dangerous than a misnamed variable.

3. **Blast radius determines depth** — A utility function used in one place gets a glance. A middleware change that affects every request gets a thorough review. Score changes by blast radius and invest review time proportionally.

4. **Automate the trivial** — Formatting, import ordering, naming conventions, and basic anti-patterns should be caught by tools (formatters, analyzers, hooks), not humans.

## Patterns

### Checkpoint Schedule

```
CHECKPOINT 1: Post-Implementation (self-review)
WHEN: After completing a feature or fix, before committing
FOCUS: Does it work? Does it compile? Do tests pass?
DEPTH: Quick — 5 minutes
□ dotnet build passes
□ dotnet test passes (all existing + new tests)
□ get_diagnostics shows no new warnings
□ No obvious anti-patterns (DateTime.Now, new HttpClient, async void)

CHECKPOINT 2: Pre-PR (focused review)
WHEN: Before creating a pull request
FOCUS: Would a staff engineer approve this?
DEPTH: Thorough on critical paths, glance at routine code — 15-30 minutes
□ Data access: N+1 queries, missing AsNoTracking
□ Security: Auth checks, input validation, no secrets in code
□ Concurrency: CancellationToken propagated, no deadlocks
□ Error handling: Result pattern used, no swallowed exceptions
□ API surface: TypedResults, proper status codes, response DTOs (not entities)
□ Tests: Integration tests cover happy path + main error case

CHECKPOINT 3: Post-Integration (system review)
WHEN: After merging to main
FOCUS: Does it play well with the rest of the system?
DEPTH: Targeted — check integration points — 10 minutes
□ Cross-module events consumed correctly
□ Database migrations applied cleanly
□ No circular dependencies introduced
□ CI pipeline passes
```

### Blast Radius Scoring

```
CRITICAL (30+ min review):
- Middleware changes (affects every request)
- Authentication/authorization changes
- Database schema changes (migrations)
- Shared kernel / cross-cutting concern changes

HIGH (15-30 min review):
- New module or subsystem
- Public API surface changes
- Message consumer changes (affects async workflows)
- EF Core configuration changes

MEDIUM (5-15 min review):
- New feature within existing module (follows patterns)
- Test additions or modifications
- New endpoint following established conventions

LOW (glance or auto-approve):
- Documentation updates
- Formatting / import ordering
- Adding logging statements
- Renaming internal variables
```

### The Top 10 Checks (Priority Order)

```
1. SQL INJECTION — Any raw SQL or string-interpolated queries?
   → EF parameterizes by default, but check for FromSqlRaw with user input

2. AUTH GAPS — Every endpoint has explicit auth?
   → Check for missing RequireAuthorization on new endpoint groups

3. N+1 QUERIES — Loading collections without Include/projection?
   → Check any LINQ that accesses navigation properties after the query

4. CANCELLATION PROPAGATION — CancellationToken passed through the full chain?
   → From endpoint → handler → service → EF query

5. SECRET EXPOSURE — Any connection strings, API keys, or tokens in code?
   → Check for hardcoded strings that look like credentials

6. EXCEPTION SWALLOWING — Catch blocks that silently discard errors?
   → Empty catch, catch with only a log, catch(Exception) without rethrow

7. ASYNC DEADLOCKS — .Result, .Wait(), .GetAwaiter().GetResult()?
   → Any synchronous blocking on async code = potential deadlock

8. ENTITY LEAKS — Domain entities returned directly from API endpoints?
   → Entities should map to response DTOs/records at the API boundary

9. MISSING VALIDATION — User input reaching business logic unchecked?
   → Every command/request DTO should have a corresponding validator

10. RESOURCE LEAKS — Disposable objects not in using/await using blocks?
    → HttpClient, FileStream, etc. created without disposal
```

### Review with MCP Tools

```
REVIEW WORKFLOW WITH MCP:
1. get_project_graph → Understand what changed in the solution structure
2. get_diagnostics → Catch compiler warnings
3. detect_antipatterns → Automated anti-pattern scan
4. find_dead_code → Check if the change left dead code behind
5. detect_circular_dependencies → Verify no new cycles introduced
6. get_test_coverage_map → Verify changed code has test coverage
```

## Anti-patterns

### Reviewing Every Trivial Change

```
// BAD — spending 20 minutes reviewing a rename
// This is what Find & Replace + tests are for

// GOOD — trust the tooling for mechanical changes
PR: Rename across 8 files → Tests pass? Build passes? Auto-approve.
Spend that 20 minutes reviewing the authentication change instead
```

### Style Over Substance

```
// BAD — reviewer focuses on naming while missing the N+1
"Line 15: rename 'x' to 'order' for clarity"
*Meanwhile, line 28 has an N+1 query that will hammer the database*

// GOOD — substance first, style if time permits
"Line 28: This will produce an N+1 — add .Include(o => o.Items)"
```

## Decision Guide

| Scenario | Review Depth | Focus Area |
|----------|-------------|------------|
| New endpoint following existing pattern | Medium (5-15 min) | Auth, validation, response mapping |
| Authentication/authorization change | Critical (30+ min) | Every code path, edge cases |
| Database migration | Critical (30+ min) | Data loss risk, rollback strategy |
| New module or subsystem | High (15-30 min) | Architecture, boundaries |
| Bug fix with clear root cause | Medium (5-15 min) | Root cause correctness, regression test |
| Rename/formatting/docs PR | Low (glance) | Tests pass, build passes |
| EF Core query changes | High (15-30 min) | N+1, tracking, cancellation |
| Middleware or filter changes | Critical (30+ min) | Blast radius — affects every request |
