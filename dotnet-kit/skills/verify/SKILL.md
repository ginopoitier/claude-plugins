---
name: verify
description: >
  Run the 7-phase verification pipeline on the current project — build, diagnostics,
  antipatterns, tests, security, format, and diff review. Use before any PR or handoff.
  Load this skill when: "verify", "verification", "run verification", "check build",
  "pre-PR check", "before PR", "run tests", "check quality", "7-phase", "pipeline check",
  "ready for review", "pre-merge check".
user-invocable: true
argument-hint: "[optional: phase number to run only, e.g. 1 or 4]"
allowed-tools: Bash, Read, Grep, Glob
---

# Verify — 7-Phase Verification Pipeline

## Core Principles

1. **Phases 1 and 4 are hard gates** — if build fails or tests fail, stop immediately, fix, and restart from Phase 1. Never continue to later phases on broken code.
2. **Halt on Phase 5 secrets** — a hardcoded secret is always a blocker. Do not deliver code with credentials in source.
3. **Warnings log, don't block** — Phases 2, 3, 5 (non-secret), 6, 7 produce WARN entries in the report. They don't stop the pipeline unless a secret is found.
4. **Format auto-fix requires Phase 1 restart** — if Phase 6 fixes files, the build must be re-verified. Format changes can break compilation.
5. **Phase 7 diff review catches what tools miss** — debug artifacts, `Console.WriteLine`, accidental file changes, leftover TODOs. Always run it.

## Patterns

### Phase Execution Order

```bash
# Phase 1 — Build (HARD GATE)
dotnet build
# → FAIL: stop, fix, restart Phase 1

# Phase 2 — Diagnostics (warnings only)
dotnet build --verbosity normal 2>&1 | grep -E "warning|error"

# Phase 3 — Antipatterns (Grep-based)
# See antipattern grep patterns below

# Phase 4 — Tests (HARD GATE)
dotnet test --no-build
# → FAIL: stop, fix, restart Phase 1

# Phase 5 — Security
# See security grep patterns below

# Phase 6 — Format
dotnet format --verify-no-changes
# → FAIL: run dotnet format, then restart Phase 1

# Phase 7 — Diff review
git diff HEAD
# Scan for debug artifacts, console output, accidental changes
```

### Phase 3 — Antipattern Grep Patterns

```bash
# async void (not in event handlers — use async Task)
grep -rn "async void" src/ --include="*.cs"

# .Result and .Wait() — blocking async calls
grep -rn "\.Result\b\|\.Wait()" src/ --include="*.cs"

# new HttpClient() — use IHttpClientFactory
grep -rn "new HttpClient(" src/ --include="*.cs"

# DateTime.Now — use TimeProvider
grep -rn "DateTime\.Now\b" src/ --include="*.cs"

# Empty catch blocks — swallowing exceptions
grep -rn "catch\s*(\s*)" src/ --include="*.cs"
grep -rn "catch\s*{" src/ --include="*.cs"

# Thread.Sleep — use async delays
grep -rn "Thread\.Sleep" src/ --include="*.cs"
```

### Phase 5 — Security Grep Patterns

```bash
# Hardcoded secrets (BLOCKER — always block delivery)
grep -rn "password\s*=\s*['\"][^'\"]" src/ --include="*.cs"
grep -rn "connectionstring\s*=\s*['\"]" src/ --include="*.cs" -i
grep -rn "apikey\s*=\s*['\"]" src/ --include="*.cs" -i
grep -rn "secret\s*=\s*['\"][^'\"]" src/ --include="*.cs" -i

# Auth gaps — endpoints without authorization
grep -rn "MapGet\|MapPost\|MapPut\|MapDelete" src/ --include="*.cs" -A 2 |
  grep -v "RequireAuthorization\|AllowAnonymous\|authorize"
```

### Report Format

```
Verification Report — {ProjectName}
=====================================
Phase 1 Build        ✅ PASS   (dotnet build)
Phase 2 Diagnostics  ⚠️  WARN  — 2 nullable warnings in OrderHandler.cs
Phase 3 Antipatterns ✅ PASS
Phase 4 Tests        ✅ PASS   (47 passed, 0 failed — 4m 12s)
Phase 5 Security     ✅ PASS
Phase 6 Format       ✅ PASS   (dotnet format --verify-no-changes)
Phase 7 Diff         ⚠️  WARN  — Console.WriteLine found in OrderService.cs:45 (remove before PR)

Verdict: READY FOR REVIEW
Warnings to address:
  - OrderHandler.cs — 2 nullable warnings (non-blocking)
  - OrderService.cs:45 — Console.WriteLine (fix before merge)
```

## Anti-patterns

### Continuing After Phase 1 Failure

```bash
# BAD — running tests on broken build
Phase 1 Build   ❌ FAIL (3 errors)
Phase 4 Tests   ⏭ SKIP  ← wrong, should have stopped at Phase 1

# GOOD — hard gate enforcement
Phase 1 Build   ❌ FAIL
→ STOP. Fix compilation errors. Restart from Phase 1.
```

### Skipping Phase 4 Because Tests Are Slow

```
# BAD — skipping tests to save time
Phase 1 ✅ Phase 2 ✅ Phase 3 ✅ Phase 4 ⏭ SKIPPED Phase 5 ✅
Verdict: READY FOR REVIEW

# GOOD — tests are a hard gate, no exceptions
Phase 4 Tests   ✅ PASS  (47 passed, 0 failed — 4m 32s)
```

### Not Restarting Phase 1 After Format Auto-Fix

```bash
# BAD — format fixes files, then declares victory without re-running build
Phase 6 Format  ❌ FAIL → ran dotnet format (auto-fixed 3 files)
Verdict: READY FOR REVIEW  ← never re-ran Phase 1!

# GOOD — restart after format change
Phase 6 Format  ❌ FAIL → ran dotnet format → restarting from Phase 1
Phase 1 Build   ✅ PASS  (re-verified after format)
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Before any PR | Run all 7 phases |
| Phase 1 fails | Stop, fix, restart Phase 1 |
| Phase 4 fails | Stop, fix, restart Phase 1 |
| Phase 5 hardcoded secret | BLOCK — fix before delivering, no exceptions |
| Phase 6 format fails | Auto-fix, restart Phase 1 |
| Phase 7 debug output found | Fix, re-run Phase 7 |
| Quick pre-commit check | Phase 1 + Phase 7 minimum |
| Greenfield (no tests yet) | Run Phases 1, 2, 3, 5, 6, 7 — note "no tests" in report |
| Single-phase requested | Run that phase only, note partial verification in verdict |

## Execution

Run the full verification pipeline on the current .NET project.

### Execute all 7 phases

Run phases in order. Halt on Phase 1 or Phase 4 failure — don't continue to later phases on broken code.

```bash
# Phase 1 — Build
dotnet build

# Phase 4 — Tests (run early if Phase 1 passes)
dotnet test --no-build

# Phase 6 — Format check
dotnet format --verify-no-changes
```

For Phase 3 (antipatterns), Grep for:
- `async void` (not in event handlers)
- `\.Result\b` or `\.Wait\(\)`
- `new HttpClient\(`
- `DateTime\.Now`
- `catch\s*\(\s*\)` or empty catch
- `Thread\.Sleep`

For Phase 7 (diff), run `git diff HEAD` and scan for debug artifacts.

### Output

Produce the verification report format above.

Final verdict: **READY FOR REVIEW** or **NEEDS FIXES — [list blockers]**

$ARGUMENTS
