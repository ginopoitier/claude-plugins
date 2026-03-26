---
name: autonomous-loops
description: >
  Autonomous iteration loops for Vue 3 / TypeScript development: build-fix, test-fix,
  refactor, and scaffold loops. Each loop has bounded iterations, progress detection,
  and fail-safe guards that prevent infinite retries and wasted tokens. Load this skill
  when Claude needs to fix TypeScript or Vite build errors, fix failing Vitest tests,
  perform multi-step refactoring, scaffold a new component or store, or when the user
  says "fix the build", "make the tests pass", "refactor this", "scaffold",
  "generate and verify", "keep going until it works", "autonomous", or "loop".
user-invocable: false
allowed-tools: Read, Write, Edit, Bash, Grep, Glob
---

# Autonomous Loops

## Core Principles

1. **Bounded iteration, always** — Every loop has a maximum iteration count. Default is 5, hard cap is 10. No loop runs forever. If 5 iterations cannot solve a build error, the problem needs human judgment, not a 6th attempt at the same approach.

2. **Progress tracking or exit** — Each iteration must make measurable progress: fewer errors, fewer failing tests, fewer warnings. If an iteration produces the same error count as the previous one, the loop exits with a STUCK status. Retrying without progress is token waste.

3. **Fail-safe guards are non-negotiable** — Loops exit on: max iterations reached, no progress detected, critical error encountered, more errors introduced than fixed, or user interruption. These guards exist to prevent the most common failure mode: Claude stubbornly retrying the same broken approach 20 times.

4. **Transparency at every iteration** — Report what changed and why after each iteration. The user should be able to follow the loop's reasoning without reading every file. "Iteration 3: fixed CS0246 by adding `using System.Text.Json`, 2 errors remain" is transparent. Silently modifying files is not.

5. **Atomicity per iteration** — Each iteration's changes should leave the codebase in a valid state (or at least no worse than before). Never make partial changes that depend on a future iteration succeeding. If iteration 3 fails, the code should still be in the state that iteration 2 left it in.

## Patterns

### Build-Fix Loop

The most common loop. Fix TypeScript type errors and Vite build errors iteratively until the build succeeds.

```
BUILD-FIX LOOP:
  max_iterations = 5
  previous_errors = []

  for iteration in 1..max_iterations:
    result = npm run build          # or: npx vue-tsc --noEmit for type-check only

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
      report "Last fix introduced new errors. Reverting iteration {iteration}."
      revert_last_changes()
      return REGRESSION

    report "Iteration {iteration}: {len(errors)} error(s) found"
    for error in errors:
      category = categorize(error)
      fix = determine_fix(error, category)
      apply(fix)
      report "  Fixed {error.code}: {error.message} → {fix.description}"

    previous_errors = errors

  report "MAX ITERATIONS reached with {len(errors)} error(s) remaining"
  return FAIL
```

**Error Categories and Fix Strategies:**

```
CATEGORY                    EXAMPLE CODE    FIX STRATEGY
Type mismatch               TS2322          Check expected type, adjust assignment or cast
Property missing on type    TS2339          Add property to interface or fix typo
Argument type mismatch      TS2345          Correct argument type or update interface
Object possibly undefined   TS2532          Add null check, use optional chaining ?.
Missing import              TS2304          Add import or install missing package
Implicit any                TS7006          Add explicit type annotation
Non-null assertion needed   TS2531          Use ! or add null guard
Vue template type error     VTI             Fix prop type or update defineProps<{...}>()
Module not found            TS2307          Check import path, add path alias to tsconfig
```

### Test-Fix Loop

Fix failing tests iteratively. Critically, this loop must determine whether the bug is in the test or in the production code.

```
TEST-FIX LOOP:
  max_iterations = 5
  previous_failures = []

  for iteration in 1..max_iterations:
    result = npx vitest run        # or: npm run test -- --run

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
      report "  {failure.test_name}: {failure.message}"
      report "  Diagnosis: {diagnosis.root_cause}"
      report "  Fix target: {diagnosis.fix_in}"  # "test" or "production"

      if diagnosis.fix_in == "test":
        fix = fix_test(failure, diagnosis)
      else:
        fix = fix_production_code(failure, diagnosis)

      apply(fix)
      report "  Applied: {fix.description}"

    previous_failures = failures

  report "MAX ITERATIONS with {len(failures)} failure(s) remaining"
  return FAIL
```

**Diagnosis Protocol:**

```
DIAGNOSING A TEST FAILURE:
1. Read the test code — understand the assertion and setup
2. Read the production code — understand the actual behavior
3. Determine root cause:
   a. Test expects wrong value → fix the test
   b. Production code has a bug → fix the production code
   c. Test setup is incomplete → fix the test setup
   d. API contract changed → update test to match new contract
4. NEVER fix a test by weakening the assertion without understanding why
   BAD:  Assert.Equal(expected, actual) → Assert.NotNull(actual)
   GOOD: Assert.Equal(expected, actual) → fix production code to return expected
```

### Refactor Loop

Multi-step refactoring with verification at each step.

```
REFACTOR LOOP:
  targets = identify_refactoring_targets()
  max_iterations = min(len(targets), 10)

  for iteration, target in enumerate(targets, 1):
    if iteration > max_iterations:
      report "MAX TARGETS reached, {len(targets) - iteration} remaining"
      return PARTIAL

    report "Refactoring {iteration}/{len(targets)}: {target.description}"

    apply_refactoring(target)

    build_result = build_fix_loop(max_iterations=3)
    if build_result != PASS:
      report "Build failed after refactoring {target}. Reverting."
      revert_changes()
      return FAIL

    test_result = test_fix_loop(max_iterations=3)
    if test_result != PASS:
      report "Tests failed after refactoring {target}. Reverting."
      revert_changes()
      return FAIL

    report "Refactoring {iteration} complete. Build: PASS, Tests: PASS"

  return PASS
```

### Scaffold Loop

Generate a new feature end-to-end and verify everything compiles and tests pass.

```
SCAFFOLD LOOP:
  1. GENERATE source files
     → Create component file (<script setup lang="ts"> + template + styles)
     → Create Pinia store if shared state is needed
     → Create composable (useXxx.ts) if logic is reusable
     → Create API client file (features/{name}/api.ts) with typed interfaces

  2. BUILD VERIFICATION
     → Run build-fix loop (max 5 iterations): npm run build
     → Type-check: npx vue-tsc --noEmit
     → If FAIL: report and stop — generated code has fundamental issues

  3. GENERATE test files
     → Create unit tests for store actions and composables
     → Create component tests with @vue/test-utils if behaviour is complex
     → Match project's test conventions (via instinct-system)

  4. TEST VERIFICATION
     → Run test-fix loop (max 5 iterations): npx vitest run
     → If FAIL: report which tests fail and why

  5. QUALITY CHECK
     → Run analyze_vue_components MCP tool — zero new issues
     → Run validate_pinia_stores MCP tool — zero new issues
     → Verify naming matches project conventions

  FINAL REPORT:
  "Scaffold complete:
   - Source files: [list with paths]
   - Test files: [list with paths]
   - Build: PASS
   - Tests: [N/N] passing
   - Warnings: 0 new
   - Anti-patterns: 0 new"
```

### Progress Detection

```
PROGRESS METRICS:
  Build-Fix:    error_count[N] < error_count[N-1]
  Test-Fix:     failure_count[N] < failure_count[N-1]
  Refactor:     target_count[N] < target_count[N-1]
  Scaffold:     phase advances (generate → build → test → verify)

STUCK DETECTION:
  Same errors/failures after a fix attempt → STUCK
  Error count oscillates (3 → 2 → 3 → 2) → STUCK (after 2 oscillations)
  Fix introduces errors in previously passing code → REGRESSION

NO-PROGRESS RESPONSE:
  1. Report the stuck state clearly
  2. List the errors/failures that could not be fixed
  3. Suggest what a human should investigate
  4. Do NOT retry the same approach
```

### Emergency Exit Conditions

```
EMERGENCY EXITS:
  1. MORE ERRORS THAN BEFORE — an iteration introduced more errors than it fixed
     → Revert the iteration's changes
     → Report: "Fix attempt introduced {N} new errors. Reverted."

  2. CRITICAL ERROR — error indicates a fundamental problem (wrong SDK, missing
     project file, corrupted solution)
     → Stop immediately
     → Report: "Critical error detected: {description}. Human intervention needed."

  3. CASCADING FAILURES — fixing one error causes 3+ new errors repeatedly
     → Stop after 2 cascades
     → Report: "Cascading failure pattern detected. The fix approach is wrong."

  4. TEST INFRASTRUCTURE FAILURE — test runner itself fails (not test assertions)
     → Stop immediately
     → Report: "Test infrastructure error: {description}. Check test setup."

  5. USER INTERRUPTION — user sends any message during the loop
     → Complete current iteration
     → Report progress so far
     → Ask how to proceed
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
Iteration 1: Add `import { ref } from 'vue'` → TS2304 persists
Iteration 2: Add `import { ref } from 'vue'` → TS2304 persists
Iteration 3: Add `import { ref } from 'vue'` → TS2304 persists

# GOOD — detect no progress, try a different approach or exit
Iteration 1: Add `import { ref } from 'vue'` → TS2304 persists
Iteration 2: STUCK — same error after fix.
  → Read the file to understand why the symbol is still unresolved
```

### Fixing by Deletion

```
# BAD — making the build pass by removing functionality
Error: TS2304 'useOrderStore' is not defined
Fix: Delete the line that uses the store
*Builds successfully! ...but the feature is broken*

# GOOD — fix the root cause
Error: TS2304 'useOrderStore' is not defined
Fix: Add the missing import: import { useOrderStore } from '@/stores/orderStore'
```

## Decision Guide

| Scenario | Loop Type | Max Iterations | Notes |
|----------|-----------|---------------|-------|
| Build fails after code changes | Build-Fix | 5 | Categorize errors, fix systematically |
| Tests fail after code changes | Test-Fix | 5 | Diagnose test vs production bug first |
| Multi-file refactoring | Refactor | 10 (or target count) | Verify build+tests after each target |
| Generating a new feature | Scaffold | 1 (phases) | Build-fix and test-fix nested inside |
| Same error persists after fix | Exit with STUCK | N/A | Report error, suggest human investigation |
| Fix introduces more errors | Emergency exit | N/A | Revert changes, report regression |
| User says "keep going" | Extend by 3 iterations | Current + 3 | Never exceed hard cap of 10 |
| User says "stop" | Exit immediately | N/A | Report progress, preserve current state |
| 3+ cascading failures | Exit immediately | N/A | The approach is fundamentally wrong |

## Execution

Select the appropriate loop type (build-fix, test-fix, refactor, or scaffold), execute it with bounded iterations and progress tracking, and report the outcome clearly — stopping immediately on emergency exit conditions.

$ARGUMENTS
