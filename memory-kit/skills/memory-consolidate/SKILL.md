---
name: memory-consolidate
description: >
  Deduplicate and merge similar memories, resolve conflicts, rewrite for clarity.
  Load this skill when: "consolidate memories", "deduplicate memory", "/memory-consolidate",
  "merge memories", "memory-consolidate", "duplicate memories", "conflicting memories",
  "too many memories", "memory bloat", "clean up memories".
user-invocable: true
argument-hint: "[--threshold <0.0-1.0>]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# Memory Consolidate

## Core Principles

1. **Present before merging** — Always show the user what will be merged before doing it. Silent consolidation destroys information.
2. **Conflicts need human judgment** — When two memories disagree, never pick one silently. Show both and ask.
3. **Merge up, not down** — The merged result should be more precise and complete than either source. Never just delete one.
4. **Preserve provenance** — Note in the merged body that it consolidates previously separate memories.
5. **Update the index** — After any merge or delete, run `memory_sync_index` to keep MEMORY.md accurate.

## Consolidation Flow

```
1. SCAN
   Call memory_deduplicate (or memory_health for broader context)
   Show: "Found {N} duplicate groups:"
   Group by similarity score, highest first

2. REVIEW (per group)
   For each group:
     "Group {N} (similarity: {score}):"
     Show both memories side-by-side: name, description, body excerpt
     Ask: "Action? [M]erge / [K]eep both / [D]elete one / [S]kip"

3. MERGE (if selected)
   a. Synthesize a merged body that combines both memories' content
      - Lead with the most complete/recent rule
      - Include reasoning from both **Why:** sections (if different)
      - Include broader scope from **How to apply:** sections
   b. Use the better of the two names (or ask user for a new name)
   c. Write merged memory via memory_store (update primary file)
   d. Delete secondary file via memory_delete
   e. Run memory_sync_index

4. CONFLICT RESOLUTION
   When two memories contradict each other:
   "These memories conflict:
    [A] {memory_a.name}: {rule_a}
    [B] {memory_b.name}: {rule_b}
    Which is correct? [A] / [B] / [Both apply in different contexts] / [Neither]"
   Keep the selected one; archive or delete the other.

5. SUMMARY
   "Consolidation complete:
    - {N} merges performed
    - {N} duplicates removed
    - Index updated"
```

## Merge Synthesis Rules

When merging two memories of the same type:

### feedback (most common merge case)

```
SOURCE A:
name: avoid-console-log-in-payments
body: "Don't use console.log in the payments module."

SOURCE B:
name: use-structured-logger
body: "Always use the project's structured logger instead of console.log."

MERGED:
name: use-structured-logger-everywhere
description: Always use project structured logger; console.log is banned in production code
body: "Always use the project's structured logger instead of console.log anywhere in production code.
**Why:** console.log is not structured, not filterable, and will appear in production output.
        Previously captured as two separate rules covering payments module and general usage.
**How to apply:** Any new code that outputs debug/info/warn/error must use the project logger."
```

### reference (dedup by system name)

```
SOURCE A:
name: jira-link
body: "Bug tracking: company.atlassian.net/jira"

SOURCE B:
name: bugs-in-jira
body: "Bugs tracked in Jira. Check there first."

MERGED:
name: bug-tracking-jira
description: Bugs tracked in Jira at company.atlassian.net/jira — check before filing new issues
body: "Bug tracking: company.atlassian.net/jira
Check Jira before filing new issues — duplicates are common."
```

## Conflict Patterns

### Direct Contradiction

```
A: "Always use async/await, never callbacks"
B: "Some legacy endpoints still use callbacks — don't change them"

Resolution: Both can be true in different contexts.
Merge with scope distinction:
"New code: always async/await. Legacy endpoints: keep existing callbacks,
 don't mix styles in the same file."
```

### Version Drift

```
A (old): "Auth uses JWT tokens stored in localStorage"
B (new): "Auth moved to httpOnly cookies after security audit"

Resolution: B is more recent — keep B. Add context:
"Auth uses httpOnly cookies (migrated from localStorage in Q1 2026 after security audit)."
```

## Anti-patterns

### Silent Delete

```
# BAD — picks the newer one, deletes old silently
"Found duplicate. Keeping newer memory, deleting older."

# GOOD — show both, let user decide
"Found duplicate:
[A] (2026-01-15) avoid-console-log: 'Don't use console.log in payments'
[B] (2026-03-20) use-structured-logger: 'Use project logger everywhere'
These overlap. Merge into one comprehensive rule? [Y/n]"
```

### Destroying Detail During Merge

```
# BAD — merged version loses the 'why' from source A
MERGED: "Use structured logger."

# GOOD — merged version is richer than either source
MERGED: "Use structured logger. **Why:** Unstructured console.log leaks to production..."
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| User runs /memory-consolidate | Scan → review each group → merge/skip |
| /memory-health reports duplicates | Offer to run /memory-consolidate |
| Two memories directly contradict | Present both, ask user to resolve |
| Memories are complementary (different scope) | Merge into one with scope distinction |
| One memory is strictly superseded | Delete superseded, update surviving one |
| More than 10 duplicate groups | Prioritize by similarity score, handle highest first |
| User wants to bulk-merge all | Confirm first — bulk merge destroys nuance |
| Memory store exceeds 200 entries | Consolidate is mandatory before adding more |
