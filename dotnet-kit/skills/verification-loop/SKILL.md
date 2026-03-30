---
name: verification-loop
description: >
  7-phase .NET verification pipeline Claude runs before marking any feature complete —
  build, diagnostics, antipatterns, tests, security, format, diff review.
  Load this skill when: "verify", "verification", "before marking complete", "check build",
  "run tests", "pre-review check", "phase 1", "phase 4", "dotnet build", "dotnet test",
  "ready for review", "verification pipeline", "quality gate".
user-invocable: false
allowed-tools: []
---

# Verification Loop

Run this before marking any implementation task as complete. Never hand off code that hasn't cleared Phase 1 and Phase 4 at minimum.

## Core Principles

1. **Phases 1 and 4 are hard stops** — A failing build or failing tests means nothing else matters. Fix and restart from Phase 1.
2. **All other failures are warnings, not blockers** — Log them, continue, report at the end. Don't stop for a nullable warning.
3. **Run the full pipeline every time** — Partial verification creates blind spots. Skipping Phase 3 because "I didn't touch SQL" is how security regressions ship.
4. **The diff is the final check** — Phase 7 catches debug artifacts, unrelated changes, and accidental commits that code review misses.

## The 7 Phases

| # | Phase | Tool | Critical? |
|---|-------|------|-----------|
| 1 | Build | `dotnet build` | Yes — halt if fails |
| 2 | Diagnostics | `dotnet build --no-incremental` / Roslyn MCP `get_diagnostics` | No — warnings are WARN |
| 3 | Antipatterns | Roslyn MCP `detect_antipatterns` / Grep | No — log findings |
| 4 | Tests | `dotnet test` | Yes — halt if fails |
| 5 | Security | Manual review | No — log findings |
| 6 | Format | `dotnet format --verify-no-changes` | No — auto-fix then re-check |
| 7 | Diff review | `git diff` analysis | No — catch debug artifacts |

## Phase 3 — Antipatterns to Detect

Search for these via Grep when Roslyn MCP is not available:
- `async void` (except event handlers)
- `.Result` or `.Wait()` on tasks (sync-over-async)
- `new HttpClient()` (use `IHttpClientFactory`)
- `DateTime.Now` (use `TimeProvider`)
- Empty `catch` blocks
- Missing `CancellationToken` in async handler signatures
- `Thread.Sleep` in production code

## Phase 5 — Security Checks

- Hardcoded connection strings or secrets (search for `Password=`, `ApiKey=`, `Secret=`)
- SQL built from string interpolation (should always be parameterized EF or Dapper)
- Missing `[Authorize]` on endpoints that need it
- User input used directly in file paths

## Phase 7 — Diff Review Checklist

- No `Console.WriteLine` or `Debug.WriteLine`
- No `TODO` comments left in committed code
- No commented-out code blocks
- No accidental changes to unrelated files
- No `.DS_Store`, `*.user`, `bin/`, `obj/` in the diff

## Short-Circuit Rules

- If Phase 1 fails → stop, fix, restart from Phase 1
- If Phase 4 fails → stop, fix, restart from Phase 1
- All other failures → log as WARN, continue, report at end

## Output Format

```
Verification Report
===================
Phase 1 Build        ✅ PASS
Phase 2 Diagnostics  ⚠️  WARN — 2 nullable warnings in OrderHandler.cs
Phase 3 Antipatterns ✅ PASS
Phase 4 Tests        ✅ PASS  (47 passed, 0 failed)
Phase 5 Security     ✅ PASS
Phase 6 Format       ✅ PASS
Phase 7 Diff         ✅ PASS

Verdict: READY FOR REVIEW
Warnings to address: [list]
```

## Anti-patterns

### Skipping Phases When Confident

```
# BAD — selective verification based on gut feel
"I only changed the handler, so I'll skip the build and just run tests."
→ Roslyn errors in unrelated files still block CI. Phase 1 takes 10 seconds.

# GOOD — always start at Phase 1
→ dotnet build → dotnet test → proceed
→ The pipeline is fast enough that skipping gains nothing
```

### Treating Warnings as Blockers

```
# BAD — halting on Phase 3 antipattern finding
"Found a DateTime.Now usage — stopping until this is fixed."
→ Blocks delivery for a low-severity finding unrelated to the current task

# GOOD — log and continue
→ WARN: DateTime.Now at CacheService.cs:42 (not in current change set)
→ Continue to Phase 4, surface at end of report
```

### Declaring "Done" Before Phase 7

```
# BAD — skipping diff review after tests pass
"Tests are green, shipping it."
→ Console.WriteLine left in OrderHandler.cs makes it to production

# GOOD — diff is always the last check
→ git diff HEAD | review for debug artifacts, commented code, unrelated files
→ Verdict only after Phase 7 completes
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Phase 1 fails | Stop immediately, fix build, restart from Phase 1 |
| Phase 4 fails | Stop, fix tests, restart from Phase 1 |
| Phase 2 warnings found | Log as WARN, continue |
| Phase 3 antipattern in changed code | Fix it, rerun Phase 1 |
| Phase 3 antipattern in unchanged code | Log as WARN, file a task |
| Phase 5 security finding in changed code | Fix it, rerun from Phase 1 |
| Phase 6 format violations | Run `dotnet format`, rerun Phase 6 |
| Phase 7 debug artifact found | Remove it, rerun Phase 7 |
| All phases pass | Verdict: READY FOR REVIEW |

## Execution

Run all 7 verification phases in order, halting on Phase 1 or Phase 4 failures and logging all other findings, then produce the structured Verification Report with a clear READY FOR REVIEW or NEEDS FIXES verdict.

$ARGUMENTS
