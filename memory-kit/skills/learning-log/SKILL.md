---
name: learning-log
description: >
  Auto-document insights and discoveries during development sessions. Unlike
  feedback memories (corrective rules), the learning log captures organic discoveries:
  non-obvious bugs, undocumented architecture decisions, performance findings, workarounds,
  and gotchas. Stored at .claude/learning-log.md. Load this skill when Claude discovers
  something non-obvious, finds a workaround, uncovers an undocumented decision, or when
  the user asks about "learnings", "discoveries", "gotchas", "what did we learn",
  or "document this finding".
user-invocable: false
allowed-tools: Read, Write
---

# Learning Log

## Core Principles

1. **Log insights, not rules** — Feedback memories store corrective rules ("always use X instead of Y"). The learning log stores discoveries ("X behaves differently when Y is configured because Z"). Rules prescribe behavior; insights explain the world.

2. **Structure enables searchability** — Every entry has a date, category, title, description, and affected files. Consistent structure means you can search by category, scan titles, or find entries related to specific files.

3. **Log during work, not after** — Capture insights the moment they occur. A 2-line entry written immediately is worth more than a paragraph reconstructed from memory.

4. **Periodic review extracts patterns** — A monthly scan of the learning log reveals recurring themes. Three "gotcha" entries about the same subsystem suggests a systemic issue worth addressing as a feedback memory.

5. **Distinct from memory store** — The memory store (via memory_store) holds permanent rules. The learning log holds descriptive insights that may eventually be promoted.

## Patterns

### Log Entry Format

Each entry follows a consistent structure in `.claude/learning-log.md`:

```markdown
# Learning Log

## 2025-07-15 | Bug Root Cause | EF Core SaveChanges Silently Succeeds on Duplicate Keys
`SaveChangesAsync` with a duplicate PK throws on the *next* `SaveChangesAsync` call, not the insert.
**Files:** `src/Orders/Features/CreateOrder.cs:42`
**Resolution:** Call `SaveChangesAsync` immediately after `Add()`, before any other operations.

## 2025-07-12 | Gotcha | MassTransit Consumer Registration Order Matters
Multiple consumers for the same message type run in registration order. If the first throws,
subsequent consumers are skipped. Caused missed audit events.
**Files:** `src/Shared/Extensions/MassTransitConfig.cs:15-30`
**Resolution:** Configure independent consumer endpoints or use the retry filter.
```

### Auto-Logging Triggers

Log entries automatically when these situations occur:

```
TRIGGER 1: Non-Obvious Bug Root Cause
When debugging reveals the root cause is NOT where the error appeared.
→ Log the misdirection, the actual cause, and how to avoid confusion.

TRIGGER 2: Undocumented Architecture Decision
When you discover WHY something was built a certain way (not just HOW).
→ Log the decision, the alternatives considered, and the rationale.

TRIGGER 3: Workaround for Framework/Library Limitation
When the "correct" approach doesn't work and you need an alternative.
→ Log what didn't work, why, and what works instead.

TRIGGER 4: Performance Finding
When profiling or observation reveals unexpected performance behavior.
→ Log the finding, the measurement, and the optimization applied.

TRIGGER 5: External Service Behavior
When an external API/service behaves differently than documented.
→ Log the expected vs actual behavior and any workaround.

TRIGGER 6: Non-Obvious Configuration
When a setting or configuration has a surprising effect.
→ Log the configuration, the surprising behavior, and the correct setup.
```

### Category System

Use these 6 categories consistently:

```
Architecture Decision  — WHY something is structured a certain way
Bug Root Cause        — Non-obvious bugs where the error ≠ the cause
Performance Discovery — Unexpected performance behavior or optimization
Pattern Found         — Reusable pattern discovered in the codebase
Gotcha                — Surprising behavior in frameworks, libraries, or APIs
External Service      — Quirks of third-party services and APIs
```

### Log vs. Memory Store vs. Handoff

```
Memory store (feedback memories, via memory_store):
- Contains: Prescriptive rules ("always do X", "never do Y")
- Source: User corrections, promoted learning log entries
- Lifespan: Permanent until proven wrong

.claude/learning-log.md (this skill):
- Contains: Descriptive insights ("X happens because Y")
- Source: Organic discoveries during development
- Lifespan: 3-6 months, then archive or promote

.claude/handoff.md (via session-management):
- Contains: Session state (done/pending/learned)
- Source: End of each session
- Lifespan: Until next session overwrites it
```

## Anti-patterns

### Logging Everything

```
// BAD — low-value entries that add noise
## 2025-07-15 | Pattern Found | Used Primary Constructors
Used primary constructors for the OrderService class.

// GOOD — only log when it's non-obvious or surprising
## 2025-07-15 | Gotcha | Primary Constructor Parameters Captured as Fields
Primary constructor parameters are implicitly captured as fields. If you also
declare an explicit field with the same name, you get a compiler warning but
no error — and the two can silently diverge.
```

### Write-Only Log (Never Reviewed)

```
// BAD — 100 entries, never reviewed
.claude/learning-log.md grows to 500 lines
Same gotchas keep appearing because no one reads the log

// GOOD — monthly reviews extract value
Every 20 entries, scan for patterns
Promote recurring findings to memory store as preventive feedback rules
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Found a non-obvious bug root cause | Log it — category: Bug Root Cause |
| Discovered why code is structured a certain way | Log it — category: Architecture Decision |
| Framework behaved unexpectedly | Log it — category: Gotcha |
| Performance surprise (good or bad) | Log it — category: Performance Discovery |
| Found a reusable pattern in the codebase | Log it — category: Pattern Found |
| External API behaved differently than docs say | Log it — category: External Service |
| User corrected Claude's code | Don't log — use self-correction-loop → memory_store |
| Routine code change, nothing surprising | Don't log — only log insights |
| Same gotcha appeared 3+ times in the log | Promote to memory store as a preventive feedback rule |
| Learning log exceeds 50 entries | Monthly review — archive old, promote recurring |
| Starting a new session | Scan recent log entries for context on the working area |
