# Memory Lifecycle

Rules for when to create, update, and retire memories.

## Creation

**Create a new memory when:**
- User explicitly asks to remember something ("remember this", "don't forget")
- User gives behavioral correction that should persist across sessions
- A non-obvious bug root cause is discovered (log as `feedback` or `project`)
- An undocumented architectural decision is revealed in conversation
- A new external reference system is introduced (tool, board, issue tracker)
- An instinct reaches 0.9 confidence and is promoted from instincts.md

**Do NOT create a memory for:**
- Things already captured in another memory (update instead)
- Session-specific context (current task, open PR, what we're working on)
- Information derivable from reading the current code
- Obvious patterns any developer would follow

## Updates

**Update an existing memory when:**
- The captured fact changes (e.g., a project deadline shifts, a system URL changes)
- A broader or more accurate version of the rule emerges
- A correction contradicts an existing memory
- A user says "actually" or "no wait" after a previous capture

**How to update:**
Use `memory_store` with the same `file_name` — it overwrites in place and updates the `updated` timestamp.

## Retirement (memory_forget / memory_delete)

**Retire a memory when:**
- `project` type: deadline passed, initiative completed, team restructured
- `reference` type: system was decommissioned or URL changed (update instead of delete if replaced)
- Any type: user explicitly asks to forget it
- Any type: a higher-quality memory supersedes it after consolidation
- Any type: memory was incorrect and has been corrected

**Staleness thresholds (triggers `/memory-health` warning):**
- `project` memories: warn after 30 days without update
- `reference` memories: warn after 90 days
- `user` and `feedback` memories: no expiry (permanent unless explicitly removed)

## Promotion from Instinct System

When an instinct in `.claude/instincts.md` reaches 0.9 confidence:
1. Classify the instinct content using `memory_classify`
2. Write it as a permanent memory via `memory_store` with `source: "promoted-instinct"`
3. Remove it from instincts.md
4. Confirm to user: "Promoted instinct to permanent memory: [name]"

## Index Maintenance

The MEMORY.md index must stay in sync with actual memory files. If `/memory-health` reports `index_sync: "drift"`, run `memory_sync_index` immediately.

Never have more than 200 lines in MEMORY.md — if approaching that limit, run `/memory-consolidate` to prune and merge.
