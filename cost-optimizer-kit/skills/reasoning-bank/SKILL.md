---
name: reasoning-bank
description: >
  Store and retrieve reasoning patterns to avoid re-deriving solutions from scratch.
  Captures the reasoning trace behind solved problems and retrieves matching patterns
  on future similar tasks — reducing reasoning tokens by ~32% on repeated task types.
  Load this skill when: "reasoning bank", "remember how we solved", "store this pattern",
  "retrieve pattern", "reasoning pattern", "cache this reasoning", "reuse solution",
  "remember this approach", "save this reasoning", "pattern bank", "solution bank",
  "avoid re-deriving", "prior reasoning".
user-invocable: true
argument-hint: "[store | retrieve <query> | list | clear <key>]"
allowed-tools: Read, Write, Edit, Glob, Grep, Bash
---

# Reasoning Bank Skill

Store and retrieve reasoning traces. When you solve a hard problem, capture the
*how* — not just the result. On subsequent similar tasks, retrieve the pattern
and apply it directly, skipping the expensive re-derivation.

---

## Why This Exists

Complex reasoning tasks can consume 5,000–15,000 tokens just in the thinking trace.
If Claude re-derives the same architectural pattern, debugging approach, or design
decision from scratch every session, you pay those tokens every time.

The ReasoningBank fixes this: after solving a hard problem, store the reasoning
pattern. On future tasks, retrieve the matching pattern and apply it directly.
**Measured saving: ~32% token reduction on repeated task types.**

---

## Storage Location

Patterns are stored as Markdown files in `~/.claude/reasoning-bank/`.

Each file: `~/.claude/reasoning-bank/{pattern-key}.md`

If `memory-kit` is installed: also store via `memory_store` with `namespace=reasoning-bank`.

---

## Argument Modes

| Argument | Action |
|----------|--------|
| `store` | Capture the current task's reasoning trace as a reusable pattern |
| `retrieve <query>` | Find and surface matching reasoning patterns for a task |
| `list` | Show all stored reasoning patterns with summaries |
| `clear <key>` | Delete a specific pattern by key |
| *(no argument)* | Auto-detect: if context contains a just-solved problem, prompt to store; otherwise prompt to retrieve |

---

## Phase 1 — Store Pattern

When `store` is passed (or the user says "save this reasoning" / "remember this approach"):

### 1.1 Elicit the Pattern

Ask the user (or infer from context):
- What was the task type? (architecture, debugging, refactoring, security, design, etc.)
- What was the core insight or decision?
- What were the key steps in the reasoning?
- What traps or dead-ends were encountered?
- What is the confidence level in this approach?

### 1.2 Generate Pattern Key

Create a kebab-case key that describes the pattern:
```
{task-type}--{core-concept}

Examples:
  architecture--multi-tenant-auth-boundary
  debugging--ef-core-lazy-loading-n+1
  design--cqrs-vs-crud-decision
  refactoring--repository-pattern-extraction
  security--jwt-refresh-token-rotation
```

### 1.3 Write the Pattern File

Write to `~/.claude/reasoning-bank/{pattern-key}.md`:

```markdown
---
key: {pattern-key}
task-type: {architecture | debugging | design | refactoring | security | testing | other}
confidence: {high | medium | low}
saved: {YYYY-MM-DD}
project: {project-name or "general"}
tags: [{tag1}, {tag2}]
---

# {Human-readable title}

## Context

{1-2 sentences: what problem triggered this reasoning}

## Core Insight

{The central insight or decision — the "aha moment"}

## Reasoning Steps

1. {First step or constraint discovered}
2. {Key decision point}
3. {Why alternative approaches were rejected}
4. {How the final approach resolves the constraints}

## Dead Ends Encountered

- {Approach tried that failed} → {why it failed}
- {Assumption that was wrong} → {correction}

## Application Pattern

When you see: {trigger description}
Do: {what to do}
Don't: {what to avoid}

## Code / Structure Reference

```{language}
{illustrative code if relevant — keep concise}
```

## Confidence Note

{Any caveats, prerequisites, or conditions where this pattern does NOT apply}
```

### 1.4 If memory-kit is available

Also store via MCP:
```
Use memory_store with:
  key: "reasoning-{pattern-key}"
  namespace: "reasoning-bank"
  value: {full pattern content}
  tags: ["reasoning-bank", "{task-type}", "{project}"]
```

### 1.5 Confirm

Report to user:
```
Reasoning pattern stored: {pattern-key}
Location: ~/.claude/reasoning-bank/{pattern-key}.md
Task type: {type}
Confidence: {level}

Retrieve later with: /reasoning-bank retrieve "{query}"
```

---

## Phase 2 — Retrieve Pattern

When `retrieve <query>` is passed (or user says "has this been solved before" / task starts):

### 2.1 Search Patterns

```bash
# List all patterns
ls ~/.claude/reasoning-bank/ 2>/dev/null

# Search content for matching terms
grep -rl "{query terms}" ~/.claude/reasoning-bank/ 2>/dev/null
```

If memory-kit is available, also search:
```
Use memory_search with query: "{query}", namespace: "reasoning-bank"
```

### 2.2 Score Relevance

For each matching pattern file:
- Read the file
- Score match on: task-type match, tag overlap, keyword matches in Context and Core Insight
- Filter to confidence >= `REASONING_BANK_MIN_CONFIDENCE` (from config, default 0.75)

### 2.3 Surface Results

If 1+ matching patterns found with confidence ≥ threshold:

```
Reasoning Pattern Match Found
==============================
Pattern: {pattern-key}
Task type: {type}  |  Confidence: {high/medium/low}  |  Saved: {date}

Core Insight:
{core insight text}

Application:
{application pattern text}

This pattern was derived for: {project context}
Applying it now saves re-derivation (~{estimated_tokens} tokens).

Proceed with this approach? [yes / no / show-full]
```

If no matches found:
```
No matching reasoning patterns found for: "{query}"
This appears to be a novel task for this project — reasoning from scratch.
After solving, run: /reasoning-bank store
```

### 2.4 Apply Pattern

If the user confirms, surface the full pattern context and proceed using it as
the starting assumption rather than re-deriving from first principles.

**Key: present the pattern as a starting point, not a rigid constraint.**
If the current context differs, adapt the pattern and update it after.

---

## Phase 3 — List Patterns

When `list` is passed:

```bash
ls ~/.claude/reasoning-bank/ 2>/dev/null | sed 's/\.md$//'
```

For each file, read the frontmatter and print a summary table:

```
Stored Reasoning Patterns
=========================
Pattern Key                           Type          Confidence  Saved
------------------------------------  ------------  ----------  ----------
architecture--multi-tenant-auth       architecture  high        2025-03-15
debugging--ef-core-n-plus-1           debugging     medium      2025-03-20
design--cqrs-event-sourcing           design        high        2025-04-01
...

Total: {N} patterns
To retrieve: /reasoning-bank retrieve "query"
```

---

## Phase 4 — Clear Pattern

When `clear <key>` is passed:

```bash
rm ~/.claude/reasoning-bank/{key}.md
```

If memory-kit is available:
```
Use memory_store to mark the entry deleted, or use memory-kit's /memory-forget
```

Confirm: `Deleted pattern: {key}`

---

## Auto-Detection (No Argument)

When invoked with no argument, check context:

**If a complex task was just completed** (context shows long reasoning):
```
It looks like you just solved a complex problem.
Store the reasoning pattern for future reuse? (saves ~32% tokens on similar tasks)
Run: /reasoning-bank store
```

**At the start of a complex task**:
```
Before reasoning from scratch, check for matching patterns:
Run: /reasoning-bank retrieve "{task description}"
```

---

## Integration with /optimize-cost

The reasoning-bank pattern is one of the Ruflo-inspired optimizations tracked by
the `/optimize-cost` skill. When auditing:
- Check if `~/.claude/reasoning-bank/` exists and has entries
- Score `REASONING_BANK_ENABLED` in the config
- Include ReasoningBank usage in the efficiency report

---

## Config Reference

From `~/.claude/cost-optimizer-kit.config.md`:

```
REASONING_BANK_ENABLED=true
REASONING_BANK_PATH=~/.claude/reasoning-bank
REASONING_BANK_MIN_CONFIDENCE=0.75
```

If config is missing, use defaults above.

---

## Anti-Patterns

### Storing Every Task

```
# BAD — storing trivial boilerplate reasoning
"Store: 'I added a null check to the service method'"

# GOOD — storing non-obvious insights worth reusing
"Store: 'Multi-tenant auth requires row-level security enforced at the DB layer,
not the API layer, to prevent tenant bleed via direct queries'"
```

### Retrieving Without Adapting

```
# BAD — applying pattern blindly without checking fit
[Task: "Add caching to this service"]
[Pattern retrieved: "Redis caching for hot lookup tables"]
→ Applies Redis without considering if this service even has a DB

# GOOD — pattern as starting hypothesis
"The caching pattern suggests Redis for hot data. Checking: yes, this is a
high-read low-write lookup service → Redis approach fits. Adapting for
in-memory L1 + Redis L2 given the volume."
```

### Patterns That Rot

Patterns older than 90 days that reference specific library versions should be
reviewed before applying — APIs change. Flag with:
```
Note: This pattern was saved {N} months ago — verify library versions still apply.
```

## Execution

1. Parse `$ARGUMENTS` — detect mode: `store`, `retrieve <query>`, `list`, `clear <key>`, or auto-detect
2. **auto-detect**: if context shows a just-completed complex task, prompt to `store`; at task start, prompt to `retrieve`
3. **store**: elicit task type, core insight, reasoning steps, dead-ends from context; generate kebab-case pattern key; write to `~/.claude/reasoning-bank/{key}.md`; also store via `memory_store` if memory-kit is available
4. **retrieve `<query>`**: grep `~/.claude/reasoning-bank/` for matching terms; if memory-kit available, also call `memory_search(query, namespace="reasoning-bank")`; score matches and surface results above confidence threshold
5. **list**: read all frontmatter from `~/.claude/reasoning-bank/*.md`, print summary table (key, type, confidence, date)
6. **clear `<key>`**: confirm, then delete `~/.claude/reasoning-bank/{key}.md`; also remove from memory-kit if installed
7. For retrieved patterns: present as a starting hypothesis, not a rigid constraint — adapt if context differs

$ARGUMENTS
