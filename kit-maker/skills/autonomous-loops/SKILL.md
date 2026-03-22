---
name: autonomous-loops
description: >
  Autonomous iteration loops for any software project: build-fix, test-fix, refactor,
  and scaffold loops. Each loop has bounded iterations, progress detection, and
  fail-safe guards that prevent infinite retries and wasted tokens. Load this skill
  when Claude needs to fix build errors, fix failing tests, perform multi-step
  refactoring, scaffold a new feature, or when the user says "fix the build",
  "make the tests pass", "refactor this", "scaffold", "generate and verify",
  "keep going until it works", "autonomous", or "loop".
user-invocable: false
allowed-tools: Read, Write, Edit, Bash, Grep, Glob
---

# Autonomous Loops

## Core Principles

1. **Bounded iteration, always** — Every loop has a maximum iteration count. Default is 5, hard cap is 10. No loop runs forever. If 5 iterations cannot solve a build error, the problem needs human judgment, not a 6th attempt at the same approach.
2. **Progress tracking or exit** — Each iteration must make measurable progress: fewer errors, fewer failing tests, fewer warnings. If an iteration produces the same error count as the previous one, the loop exits with STUCK status. Retrying without progress is token waste.
3. **Fail-safe guards are non-negotiable** — Loops exit on: max iterations reached, no progress detected, critical error encountered, more errors introduced than fixed, or user interruption.
4. **Transparency at every iteration** — Report what changed and why after each iteration. "Iteration 3: fixed missing import, 2 errors remain" is transparent. Silently modifying files is not.
5. **Atomicity per iteration** — Each iteration's changes should leave the codebase in a valid state. Never make partial changes that depend on a future iteration succeeding.

## Patterns

### Build-Fix Loop

Fix compilation/syntax errors iteratively until the build passes. Commands are project-specific — read them from the project's CLAUDE.md or instincts.md.

```
BUILD-FIX LOOP:
  build_cmd = get_build_command()   # e.g., "npm run build", "cargo build", "go build ./..."
  max_iterations = 5
  previous_errors = []

  for iteration in 1..max_iterations:
    result = run(build_cmd)

    if result.exit_code == 0:
      report "BUILD PASS after {iteration} iteration(s)"
      return PASS

    errors = parse_errors(result.output)

    if errors == previous_errors:
      report "STUCK — same {len(errors)} error(s) after fix attempt"
      report "Errors: {errors}"
      return STUCK

    if len(errors) > len(previous_errors) and iteration > 1:
      report "REGRESSING — {len(errors)} errors, up from {len(previous_errors)}"
      revert_last_changes()
      return REGRESSION

    report "Iteration {iteration}: {len(errors)} error(s) found"
    for error in errors:
      category = categorize(error)
      fix = determine_fix(error, category)
      apply(fix)
      report "  Fixed [{error.code}]: {error.message} → {fix.description}"

    previous_errors = errors

  report "MAX ITERATIONS reached with {len(errors)} error(s) remaining"
  return FAIL
```

**Getting the Build Command:**

```
Priority order (highest wins):
1. Project CLAUDE.md — ## Build Commands section
2. ~/.claude/instincts.md — instinct with "build command" tag
3. Infer from project files:
   - package.json + "build" script → npm run build
   - Cargo.toml                   → cargo build
   - go.mod                       → go build ./...
   - *.sln / *.csproj             → dotnet build
   - Makefile with "build" target → make build
   - pyproject.toml               → python -m build
4. Ask the user
```

**Universal Error Categories:**

```
CATEGORY              FIX STRATEGY
Missing import        Add the correct import/using/require
Missing dependency    Install the package (npm install, pip install, etc.)
Type mismatch         Check expected type, cast or convert
Name not found        Check spelling, verify the symbol exists in scope
Syntax error          Fix syntax based on the error line
API change            Update call to match new API signature
Circular dependency   Restructure imports to break the cycle
Configuration error   Check build config files for the project type
```

### Test-Fix Loop

Fix failing tests iteratively. Always determine whether the bug is in the test or production code.

```
TEST-FIX LOOP:
  test_cmd = get_test_command()   # e.g., "npm test", "pytest", "go test ./..."
  max_iterations = 5
  previous_failures = []

  for iteration in 1..max_iterations:
    result = run(test_cmd)

    if result.all_passed:
      report "TESTS PASS after {iteration} iteration(s)"
      return PASS

    failures = parse_failures(result.output)

    if failures == previous_failures:
      report "STUCK — same {len(failures)} failure(s) after fix attempt"
      return STUCK

    report "Iteration {iteration}: {len(failures)} failure(s)"
    for failure in failures:
      diagnosis = diagnose(failure)
      fix_target = "test" if test_is_wrong(failure) else "production"
      apply_fix(failure, fix_target)

    previous_failures = failures
```

**Test Failure Diagnosis:**

```
DIAGNOSING A TEST FAILURE:
1. Read the test code — understand the assertion and setup
2. Read the production code — understand actual behavior
3. Determine root cause:
   a. Test expects wrong value → fix the test
   b. Production code has a bug → fix the production code
   c. Test setup is incomplete → fix the test setup
   d. Contract changed → update test to match new contract
4. NEVER fix a test by weakening assertions
   BAD:  assertEqual(expected, actual) → assertNotNull(actual)
   GOOD: assertEqual(expected, actual) → fix production code to return expected
```

### Refactor Loop

Multi-step refactoring with build + test verification at each step.

```
REFACTOR LOOP:
  targets = identify_refactoring_targets()
  max_iterations = min(len(targets), 10)

  for iteration, target in enumerate(targets, 1):
    report "Refactoring {iteration}/{len(targets)}: {target.description}"
    apply_refactoring(target)

    if build_fix_loop(max_iterations=3) != PASS:
      revert_changes(); return FAIL

    if test_fix_loop(max_iterations=3) != PASS:
      revert_changes(); return FAIL

    report "Refactoring {iteration} complete. Build: PASS, Tests: PASS"
```

### Scaffold Loop

Generate a new feature end-to-end and verify build + tests pass.

```
SCAFFOLD LOOP:
  1. GENERATE source files
     → Create feature files per project's conventions (from instinct-system)

  2. BUILD VERIFICATION
     → Run build-fix loop (max 5 iterations)
     → If FAIL: report — generated code has fundamental issues

  3. GENERATE test files
     → Create tests matching project's test conventions

  4. TEST VERIFICATION
     → Run test-fix loop (max 5 iterations)
     → If FAIL: report which tests fail and why

  5. QUALITY CHECK
     → Run project's linter if configured
     → Verify naming matches project conventions

  FINAL REPORT:
  "Scaffold complete:
   - Source files: [list with paths]
   - Test files: [list with paths]
   - Build: PASS
   - Tests: [N/N] passing"
```

### Progress Detection

```
PROGRESS METRICS:
  Build-Fix:  error_count[N] < error_count[N-1]
  Test-Fix:   failure_count[N] < failure_count[N-1]
  Refactor:   target_count[N] < target_count[N-1]
  Scaffold:   phase advances (generate → build → test → verify)

STUCK DETECTION:
  Same errors/failures after a fix attempt → STUCK
  Error count oscillates (3→2→3→2) → STUCK (after 2 oscillations)
  Fix introduces errors in previously passing code → REGRESSION

NO-PROGRESS RESPONSE:
  1. Report the stuck state clearly
  2. List the errors/failures that could not be fixed
  3. Suggest what a human should investigate
  4. Do NOT retry the same approach
```

## Anti-patterns

### Unbounded Loops

```
# BAD — no iteration limit
"Keep fixing build errors until it compiles"
*Claude tries 47 iterations, burns through context window*

# GOOD — explicit bounds with progress checks
build_fix_loop(max_iterations=5)
*After 5 iterations or zero progress, stops and reports*
```

### Retrying the Same Fix

```
# BAD
Iteration 1: Add missing import → same error persists
Iteration 2: Add missing import → same error persists
Iteration 3: Add missing import → same error persists

# GOOD — detect no progress, try different approach or exit
Iteration 1: Add missing import → error persists
Iteration 2: STUCK — same error after fix.
  → Search for where the symbol is defined
  → Consider if the package is installed
```

### Fixing by Deletion

```
# BAD — making the build pass by removing functionality
Error: Symbol 'OrderValidator' not found
Fix: Delete all validation code
*Build passes! ...but the feature is broken*

# GOOD — fix the root cause
Error: Symbol 'OrderValidator' not found
Fix: Create the missing class or add the correct import
```

## Decision Guide

| Scenario | Loop Type | Max Iterations | Notes |
|----------|-----------|----------------|-------|
| Build fails after code changes | Build-Fix | 5 | Categorize errors, fix systematically |
| Tests fail after code changes | Test-Fix | 5 | Diagnose test vs production bug first |
| Multi-file refactoring | Refactor | 10 (or target count) | Verify build+tests after each target |
| Generating a new feature | Scaffold | 1 (phases) | Build-fix and test-fix nested inside |
| Same error persists after fix | Exit STUCK | N/A | Report error, suggest human investigation |
| Fix introduces more errors | Emergency exit | N/A | Revert, report regression |
| User says "keep going" | Extend by 3 | Current + 3 | Never exceed hard cap of 10 |
| User says "stop" | Exit immediately | N/A | Report progress, preserve state |
| 3+ cascading failures | Exit immediately | N/A | The approach is fundamentally wrong |
| Build command unknown | Ask or infer | N/A | Check CLAUDE.md, package files, Makefile |
