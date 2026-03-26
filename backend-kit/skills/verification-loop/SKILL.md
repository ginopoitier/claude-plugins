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

## Execution

Run all 7 verification phases in order, halting on Phase 1 or Phase 4 failures and logging all other findings, then produce the structured Verification Report with a clear READY FOR REVIEW or NEEDS FIXES verdict.

$ARGUMENTS
