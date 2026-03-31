---
name: memory-forget
description: >
  Deprecate or permanently delete memories. Supports single memory removal, bulk stale
  cleanup, and type-based purges. Load this skill when: "forget this", "delete memory",
  "/memory-forget", "memory-forget", "remove memory", "memory is wrong", "stale memory",
  "outdated memory", "purge memories", "clear project memories".
user-invocable: true
argument-hint: "[<name or query> | --type <type> | --stale | --all-project]"
allowed-tools: Read, Write, Glob, Grep
---

# Memory Forget

## Core Principles

1. **Always confirm before deleting** — Show what will be deleted and require explicit confirmation. No silent deletes.
2. **Prefer update over delete for feedback/user** — If content is outdated, update it. Only delete if the entire memory is invalid.
3. **Delete project memories freely** — Project memories are time-bound. Expired project contexts should be removed.
4. **Update the index after every delete** — Call `memory_sync_index` after any deletion to keep MEMORY.md accurate.
5. **Archive option for important memories** — Offer to move to an archive/ subdirectory rather than permanently deleting if unsure.

## Forget Modes

### Mode 1: Single Memory (by name or query)

```
INPUT: /memory-forget "testing database mocking"

FLOW:
1. Call memory_search("testing database mocking", limit=5)
2. Show results: "Found these matching memories:"
   [1] feedback_testing.md — "Integration tests must use real database"
   [2] feedback_db_queries.md — "Never use in-memory SQLite for integration tests"
3. Ask: "Delete which? [1/2/all/cancel]"
4. Confirm: "Delete '{name}'? This cannot be undone. [y/N]"
5. Call memory_delete(file_path)
6. Call memory_sync_index
7. "Deleted: {file_path}"
```

### Mode 2: Stale Cleanup (--stale)

```
INPUT: /memory-forget --stale

FLOW:
1. Call memory_health to get stale entries list
2. Show stale memories grouped by type:
   "Stale project memories (>30 days):"
   [1] project_sprint_q1.md — last updated 2026-01-15 (75 days ago)
   [2] project_migration_plan.md — last updated 2026-02-10 (48 days ago)
3. Ask: "Delete all stale project memories? [y/N] or enter numbers to select: "
4. Confirm per-item or bulk, then delete selected
5. Run memory_sync_index after bulk delete
```

### Mode 3: Type-Based Purge (--type)

```
INPUT: /memory-forget --type project

FLOW:
1. Call memory_list(type="project")
2. Show all project memories with dates
3. Ask: "Delete ALL {N} project memories? This clears the project context. [y/N]"
4. If confirmed → bulk delete + sync index
```

### Mode 4: Full Project Reset (--all-project)

```
INPUT: /memory-forget --all-project

FLOW:
1. Show total memory count for project
2. Warn: "This deletes ALL {N} memories for project '{id}' including user profile,
          feedback rules, and references. This action cannot be undone."
3. Ask: "Type the project ID to confirm deletion: "
4. Require exact match before proceeding
5. Delete all memory files + clear MEMORY.md
```

## What to Delete vs. Update

| Scenario | Recommended Action |
|----------|--------------------|
| Project deadline passed | Delete (expired project memory) |
| Team was restructured | Delete or update project memory |
| Feedback rule is still valid but imprecise | Update via /memory-capture, don't delete |
| User profile expertise has changed | Update, not delete |
| Reference URL changed | Update the URL in the existing memory |
| Reference system decommissioned | Delete |
| Feedback rule was wrong all along | Delete immediately |
| User says "forget this" | Delete immediately, no hesitation |

## Archive Option

Before deleting `feedback` or `user` memories, offer archiving:

```
"Before deleting '{name}' (feedback), archive it instead?
Archive moves it to memory/archive/ and removes it from active search.
[Delete] [Archive] [Cancel]"
```

Archived files are excluded from `memory_search` but kept for reference.

## Anti-patterns

### Deleting Without Showing

```
# BAD
/memory-forget "testing"
→ Silently deletes 3 memories matching "testing"

# GOOD
/memory-forget "testing"
→ "Found 3 memories matching 'testing'. Show them before deleting? [Y/n]"
→ Shows list → confirms per item
```

### Deleting Feedback Rules Without Checking Validity

```
# BAD — user says "remove the testing memory" and Claude deletes the anti-mocking rule
*Deletes feedback_testing.md which contains a valid, frequently-applied rule*

# GOOD — pause before deleting feedback rules
"This is a feedback rule (used frequently in this project).
 Is the rule no longer valid, or should it be updated instead? [Delete] [Update] [Cancel]"
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| User says "forget [X]" | Run mode 1 (single search + confirm) |
| User says "clean up old project memories" | Run mode 2 (stale cleanup) |
| User says "clear all project context" | Run mode 3 (type=project purge) |
| User starts fresh on a new project | Offer mode 4 (full reset) |
| Memory is feedback type | Offer archive option before delete |
| Memory is user type | Strong warning — deletes user profile context |
| Stale reference URL | Update, don't delete (keep the location pointer, update URL) |
| Memory was captured by mistake | Delete immediately, no archive needed |
| User unsure | Default to archive rather than delete |
