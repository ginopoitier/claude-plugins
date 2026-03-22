---
name: verification-loop
description: >
  7-phase verification pipeline Claude runs before marking any implementation task complete.
  Covers build, diagnostics, antipatterns, tests, security, format, and diff review.
  Works for any language or framework — commands are read from project config.
  Load this skill when: "verify", "verification", "before merge", "check my work",
  "is it done", "ready for review", "pre-commit check", "quality gate", "final check".
user-invocable: false
allowed-tools: Read, Bash, Grep, Glob
---

# Verification Loop

## Core Principles

1. **Phases 1 and 4 are hard gates** — If build fails or tests fail, stop immediately, fix, and restart from Phase 1. Never hand off broken code.
2. **Commands come from the project, not this skill** — Read the project's CLAUDE.md or infer from project files (`package.json`, `Makefile`, `Cargo.toml`, etc.). Never hardcode language-specific commands.
3. **Warnings are WARN, not FAIL** — Phases 2, 3, 5, 6, 7 log findings into the report. They don't block delivery unless a hardcoded secret is found (Phase 5 → always block).
4. **Diff review catches what tools miss** — Phase 7 catches debug artifacts, console output statements, accidental file changes, and leftover TODOs. Automated tools don't find these.
5. **Output a structured report** — A report proves the verification ran and documents any warnings for the reviewer.

## Patterns

### The 7 Phases

| # | Phase | How to Get the Command | Critical? |
|---|-------|----------------------|-----------|
| 1 | Build | `{BUILD_CMD}` from project config | **Yes — halt if fails** |
| 2 | Diagnostics | `{BUILD_CMD}` verbose/warnings output | No — log warnings |
| 3 | Antipatterns | `Grep` for project's known antipatterns | No — log findings |
| 4 | Tests | `{TEST_CMD}` from project config | **Yes — halt if fails** |
| 5 | Security | Grep for secrets + auth patterns | No (block on secrets) |
| 6 | Format | `{FORMAT_CMD}` from project config | No — auto-fix, re-run |
| 7 | Diff review | `git diff` analysis | No — catch debug artifacts |

### Resolving Project Commands

Before running any phase, identify the project's commands:

```
READ PRIORITY (highest first):
1. Project CLAUDE.md → ## Build / ## Commands / ## Scripts section
2. ~/.claude/instincts.md → build/test/format command instincts
3. Infer from project files:

   package.json scripts:
     "build": "..." → npm run build
     "test": "..."  → npm test
     "lint": "..."  → npm run lint / npm run format

   Makefile:
     build/test/lint/format targets → make {target}

   Language-specific:
     Cargo.toml    → cargo build / cargo test / cargo fmt
     go.mod        → go build ./... / go test ./... / gofmt
     *.sln/*.csproj → dotnet build / dotnet test / dotnet format
     pyproject.toml → python -m build / pytest / black .
     pom.xml        → mvn compile / mvn test
     build.gradle   → ./gradlew build / ./gradlew test

4. Ask the user if unclear
```

### Phase 3: Antipatterns Detection

Detect patterns the project considers bad practice. Source from project CLAUDE.md or infer by language:

```bash
# Universal antipatterns (any language):
# Hardcoded credentials
grep -rn "password\s*=\s*['\"][^'\"]" src/
grep -rn "api_key\s*=\s*['\"][^'\"]" src/
grep -rn "secret\s*=\s*['\"][^'\"]" src/

# Debug output left in code
grep -rn "console\.log\|print(\|printf(" src/ --include="*.js" --include="*.ts"
grep -rn "debugger;" src/
grep -rn "TODO\|FIXME\|HACK" src/

# Language-specific (infer from project type):
# JavaScript/TypeScript: no var, no any, no console.log
# Python: no bare except, no print() in non-CLI code
# Rust: no unwrap() in production, no TODO! macros
# Go: no blank identifier for errors (_)
# C#: no async void, no .Result/.Wait(), no new HttpClient()
```

### Phase 5: Security Checks

```bash
# Hardcoded secrets (any language)
grep -rn "password\s*=" src/ --include="*.{js,ts,py,go,rs,cs,java}"
grep -rn "api.key\s*=" src/
grep -rn "secret\s*=" src/
grep -rn "token\s*=\s*['\"]" src/

# Auth gaps (check per framework — read project CLAUDE.md)
# e.g., Express: routes without auth middleware
# e.g., ASP.NET: endpoints without [Authorize]
# e.g., Django: views without @login_required
```

### Phase 6: Format

```bash
# Run the project's format command in verify/check mode first:
# JavaScript: prettier --check .
# Python: black --check . / ruff check .
# Rust: cargo fmt -- --check
# Go: gofmt -l .
# C#: dotnet format --verify-no-changes

# If check fails:
# 1. Run the auto-fix variant
# 2. MUST restart from Phase 1 (format changes files)
```

### Short-Circuit Rules

```
Phase 1 fails → STOP. Fix build. Restart Phase 1.
Phase 4 fails → STOP. Fix tests. Restart Phase 1.
Phase 5 hardcoded secret → BLOCK. Do not deliver.
Phase 6 fails → Auto-fix. Restart Phase 1.
All other findings → Log as WARN. Continue. Include in report.
```

## Anti-patterns

### Hardcoding Language-Specific Commands

```
# BAD — assumes dotnet
Phase 1: dotnet build
Phase 4: dotnet test

# GOOD — reads project configuration
Phase 1: [read from project CLAUDE.md or infer from project files]
Phase 4: [read from project CLAUDE.md or infer from project files]
```

### Skipping Phase 4 (Tests Are Slow)

```
# BAD
Phase 1 Build    ✅ PASS
Phase 4 Tests    ⏭ SKIPPED — takes 5 minutes
Verdict: READY FOR REVIEW

# GOOD — tests are a hard gate, no exceptions
Phase 4 Tests    ✅ PASS  (47 passed, 0 failed — 4m 32s)
```

### Not Restarting Phase 1 After Format Fix

```
# BAD
Phase 6 Format   ❌ FAIL → ran formatter (auto-fixed)
Verdict: READY FOR REVIEW   ← never re-ran Phase 1!

# GOOD — format changes files; must re-verify
Phase 6 Format   ❌ → auto-fixed → restart Phase 1
Phase 1 Build    ✅ PASS (confirmed formatter didn't break build)
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Build command unknown | Infer from project files or ask |
| Phase 1 fails | Stop, fix, restart Phase 1 |
| Phase 4 fails | Stop, fix, restart Phase 1 |
| Phase 3 antipattern found | Log WARN, continue |
| Phase 5 hardcoded secret found | BLOCK — fix before delivering |
| Phase 6 format fails | Auto-fix, restart Phase 1 |
| Phase 7 debug output found | Fix, re-run Phase 7 |
| Greenfield (no tests yet) | Run Phases 1,2,3,5,6,7 — note "no tests" |
| Quick pre-commit check | Phase 1 + Phase 7 minimum |
| Full feature delivery | All 7 phases + structured report |

## Output Format

```
Verification Report
===================
Phase 1 Build        ✅ PASS   (npm run build)
Phase 2 Diagnostics  ⚠️  WARN — 2 TypeScript strict warnings
Phase 3 Antipatterns ✅ PASS
Phase 4 Tests        ✅ PASS   (47 passed, 0 failed)
Phase 5 Security     ✅ PASS
Phase 6 Format       ✅ PASS   (prettier --check)
Phase 7 Diff         ✅ PASS

Verdict: READY FOR REVIEW
Warnings to address: [list any WARNs]
```
