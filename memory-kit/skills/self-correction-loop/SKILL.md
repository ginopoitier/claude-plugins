---
name: self-correction-loop
description: >
  Self-improving correction capture system. After ANY user correction, detect it,
  generalize the lesson, and store it as a reusable rule in the memory store. Ensures
  Claude's mistake rate drops over time by compounding corrections into permanent
  knowledge. Load this skill when a user corrects Claude's output, mentions
  "correction", "user corrected", "wrong approach", "don't do that", "don't do that again",
  "remember this", "learn from mistakes", "learn from mistake", "update memory",
  "permanent rule", "self-correct", or when starting a new session (to review existing rules).
user-invocable: false
allowed-tools: Read, Write
---

# Self-Correction Loop

## Core Principles

1. **Every correction is a compounding investment** — A correction costs the user 30 seconds today but saves hours across all future sessions. Treat every correction as high-priority knowledge capture, not a one-time fix.

2. **Generalize before storing** — "Use `TimeProvider` not `DateTime.Now` in the Orders module" becomes "Always use `TimeProvider` instead of `DateTime.Now/UtcNow` across all modules." Specific corrections become class-level rules.

3. **Use memory_store, not Write** — Feedback memories must go through the memory-mcp `memory_store` tool so the MEMORY.md index stays accurate and the memory is searchable.

4. **Deduplicate aggressively** — Before adding a rule, call `memory_search` for overlap. Update an existing memory rather than adding a near-duplicate.

5. **Review memory at session start** — The first thing Claude should do in a new session is check the injected memory context for project-specific rules. Knowledge captured but never reviewed is wasted effort.

## Patterns

### Correction Detection & Capture Flow

When a user corrects Claude's output, follow this exact sequence:

```
1. DETECT — User says something like:
   - "No, use X instead of Y"
   - "We don't do it that way here"
   - "That's wrong, it should be..."
   - "Always/Never do X in this project"
   - "Remember this for next time"

2. ACKNOWLEDGE — Confirm understanding of the correction
   "Got it — I'll use the project logger instead of console.log."

3. GENERALIZE — Extract the class-level rule
   Specific: "Don't use console.log in the payments module"
   General:  "Always use the project logger instead of console.log —
              keeps production output clean and structured."

4. CHECK — Call memory_search for existing related memories
   - If a related memory exists, UPDATE it via memory_store
   - If no related memory exists, CREATE via memory_store

5. STORE — Write via memory_store:
   type: "feedback"
   body structure: rule + **Why:** + **How to apply:**

6. CONFIRM — Tell the user what was captured
   "Captured to memory: use-structured-logger (feedback)"
```

### Feedback Memory Body Structure

Every correction stored as a feedback memory must have this structure:

```markdown
[Rule statement — one actionable sentence]

**Why:** [the reason the user gave, or the incident that caused it]

**How to apply:** [when/where this guidance kicks in — scope it correctly]
```

### Rule Generalization: Specific to Class

Transform specific corrections into broadly applicable rules:

```
SPECIFIC CORRECTION:
"Don't use console.log in the payments handler"

GENERALIZATION STEPS:
1. Is this specific to the payments handler? → No, applies everywhere
2. Is this specific to one type of output? → No, all debug output
3. What's the underlying principle? → Logger is structured; console.log is not
4. What's the broadest correct statement?

GENERALIZED RULE:
"Always use the project's structured logger instead of console.log/print —
logger is configurable, filterable, and production-safe."
```

### Periodic Memory Audit

Every 5-10 sessions (or when memory exceeds 50 rules), run `/memory-health`:

```
AUDIT CHECKLIST:
1. Remove rules that contradict each other — keep the most recent
2. Merge rules that overlap — run /memory-consolidate
3. Remove rules that are now obvious (captured early but now second nature)
4. Verify rules are still accurate
5. Check that coverage is balanced across types
```

## Anti-patterns

### Ignoring Corrections

```
// BAD — user corrects, Claude fixes but doesn't capture
User: "No, use the project logger here, not console.log"
Claude: "Fixed. Here's the updated code with logger..."
*Next session: makes the same mistake*

// GOOD — fix AND capture
User: "No, use the project logger here, not console.log"
Claude: "Fixed. Captured to memory: use-structured-logger (feedback)"
*Next session: checks injected memory, uses logger from the start*
```

### Storing Session-Specific Context

```
// BAD — temporary state saved as permanent memory
"Currently working on the Orders module refactor, file is at src/Orders/Handler.cs"

// GOOD — only permanent, reusable knowledge
"The Orders module uses VSA with one file per feature under Features/Orders/"
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| User explicitly corrects Claude's code | Capture generalized rule via memory_store |
| User says "remember this" or "always/never" | Capture exactly as stated, generalize |
| Same correction given twice | High priority — the rule wasn't captured or reviewed |
| Correction is project-specific | Store as feedback memory |
| Correction is universal (any project) | Store as feedback memory — it applies here too |
| Memory store exceeds 50 feedback entries | Trigger /memory-consolidate |
| Starting a new session | Review injected MEMORY.md context before writing code |
| Rule contradicts an existing memory | Keep the most recent correction, update the old one |
| Correction is about a one-time task | Don't store — only capture reusable patterns |
| User asks to forget a rule | Call memory_delete immediately |
| Pattern observed but not yet confirmed | Create an instinct via instinct-system (0.3) instead |
| Instinct reaches 0.9 confidence | Promote to memory store as a permanent feedback rule |
