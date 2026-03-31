---
name: autonomous-loops
description: >
  Bounded iteration loops for memory operations: bulk capture, health-fix, consolidation,
  and sync loops. Each loop has bounded iterations, progress detection, and fail-safe guards.
  Load this skill when: "bulk memory", "fix all memory issues", "consolidate everything",
  "keep going until", "autonomous", "loop", "batch memory operations".
user-invocable: false
allowed-tools: Read, Write, Edit, Bash, Grep, Glob
---

# Autonomous Loops

## Core Principles

1. **Bounded iteration, always** — Every loop has a maximum iteration count. Default is 5, hard cap is 10. No loop runs forever.
2. **Progress tracking or exit** — Each iteration must make measurable progress. If an iteration produces the same state, the loop exits with STUCK status.
3. **Fail-safe guards are non-negotiable** — Loops exit on: max iterations reached, no progress detected, more issues introduced than fixed, or user interruption.
4. **Transparency at every iteration** — Report what changed after each iteration.
5. **Confirmation before bulk destructive operations** — Always confirm before bulk deletes.

## Memory-Specific Loops

### Memory Health-Fix Loop

Repeatedly run health check and fix issues until the store is healthy or max iterations reached.

```
HEALTH-FIX LOOP:
  max_iterations = 5
  previous_issue_count = ∞

  for iteration in 1..max_iterations:
    result = memory_health()

    if result.issues.length == 0:
      report "Memory store healthy after {iteration} iteration(s)"
      return PASS

    if result.issues.length >= previous_issue_count and iteration > 1:
      report "STUCK — {result.issues.length} issues remain after fix attempt"
      return STUCK

    report "Iteration {iteration}: {result.issues.length} issues"

    # Fix errors first
    for issue in result.issues where severity == "error":
      apply_fix(issue)
      report "  Fixed [{issue.severity}]: {issue.file} — {issue.issue}"

    # Then warnings
    for issue in result.issues where severity == "warning":
      apply_fix(issue)

    # Always sync index
    memory_sync_index()

    previous_issue_count = result.issues.length

  report "MAX ITERATIONS reached with {result.issues.length} issues remaining"
  return PARTIAL
```

### Memory Consolidation Loop

Deduplicate and merge until no duplicate groups remain above threshold.

```
CONSOLIDATION LOOP:
  max_iterations = 3
  threshold = 0.7

  for iteration in 1..max_iterations:
    groups = memory_deduplicate(threshold=threshold)

    if groups.length == 0:
      report "No duplicates found after {iteration} iteration(s)"
      return CLEAN

    report "Iteration {iteration}: {groups.length} duplicate groups found"
    for group in groups:
      # Present each group to user (or auto-merge if run in autonomous mode)
      handle_group(group)

    memory_sync_index()

  report "Consolidation complete. Remaining groups: {groups.length}"
  return DONE
```

### Bulk Capture Loop

Capture multiple memories from a list without interruption.

```
BULK CAPTURE LOOP:
  items = parse_capture_list($ARGUMENTS)
  success = 0; failed = 0

  for item in items:
    try:
      classified = memory_classify(item.content)
      memory_store(classified.type, classified.suggested_name, item.content)
      success++
      report "  Captured: {classified.suggested_name} ({classified.type})"
    catch:
      failed++
      report "  Failed: {item.content[:50]}... — {error}"

  memory_sync_index()
  report "Bulk capture: {success} captured, {failed} failed"
```

## Progress Detection

```
PROGRESS METRICS:
  Health-Fix:    issue_count[N] < issue_count[N-1]
  Consolidation: group_count[N] < group_count[N-1]
  Bulk Capture:  items_remaining[N] < items_remaining[N-1]

STUCK DETECTION:
  Same count after a fix attempt → STUCK
  Count oscillates → STUCK after 2 oscillations

NO-PROGRESS RESPONSE:
  1. Report the stuck state clearly
  2. List the issues that could not be fixed
  3. Suggest what a human should investigate
  4. Do NOT retry the same approach
```

## Decision Guide

| Scenario | Loop Type | Max Iterations |
|----------|-----------|----------------|
| Memory health score < 50 | Health-Fix | 5 |
| Bulk duplicate cleanup | Consolidation | 3 |
| Multiple memories to capture at once | Bulk Capture | 1 (per item) |
| Same issue persists after fix | Exit STUCK | N/A |
| Fix introduces new issues | Emergency exit | N/A |
| User says "keep going" | Extend by 2 | Current + 2 (never exceed 10) |
| User says "stop" | Exit immediately | N/A |
