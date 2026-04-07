---
name: memory-recall
description: >
  Explicit context retrieval — search memories relevant to the current task or query
  and inject them into the working context. Load this skill when: "recall memories",
  "what do you know about", "relevant memories", "/memory-recall", "memory-recall",
  "check memory for", "surface memories", "what did we capture about".
user-invocable: true
argument-hint: "[<topic or query>]"
allowed-tools: Read, Glob, Grep
---

# Memory Recall

## Core Principles

1. **Search before answering** — When a task is contextually loaded, check memory first to avoid re-covering known ground.
2. **Surface, don't recite** — Show relevant memories as context to inform the response, not as a wall of text.
3. **Relevance threshold** — Only surface memories with score ≥ 0.3. Low-relevance memories add noise.
4. **Structured output** — Present recalled memories by type, linking to the file for user verification.
5. **Update stale hits** — If a recalled memory appears outdated, flag it for review rather than silently using stale data.

## Recall Flow

### When Invoked Directly (/memory-recall)

```
1. QUERY
   If $ARGUMENTS provided → use as search query
   Otherwise → ask "What would you like to recall memories about?"

2. SEARCH
   Call memory_search(query, limit=8, min_score=0.2)

3. DISPLAY
   Group results by type, show name + description + file link
   "Found {N} relevant memories:"
   [feedback] Avoid database mocking → memory/feedback_db_mocking.md
   [project] Merge freeze April 3 → memory/project_merge_freeze.md

4. OFFER NEXT STEPS
   "Use these to inform your current task, or:
    /memory-health — to audit the full store
    /memory-forget — to remove outdated entries"
```

### Auto-Recall at Task Start

When starting a complex task (feature implementation, bug investigation, architecture decision), Claude should proactively call `memory_search` with the task topic before beginning.

```
Starting task: "add payment processing endpoint"
→ memory_search("payment processing endpoint")
→ Surface: [feedback] payment module error handling style
            [reference] Stripe API docs location
→ Apply these before generating code
```

Auto-recall triggers:
- Implementing a feature in a domain that has existing memories
- Debugging an area the user has previously corrected
- Making an architectural decision about a system documented in reference memories

## Output Format

When surfacing recalled memories, use this compact format:

```
Recalled memories relevant to "{query}":

[feedback] {name}
  {description}
  → {file_path}

[project] {name}
  {description}
  → {file_path}

Applying these to the current task.
```

If no relevant memories found: "No memories found for '{query}'. Use /memory-capture to start building memory for this area."

## Staleness Detection

When displaying recalled memories, check the `updated` frontmatter field:

- `project` memories older than 30 days → flag: "(may be stale — last updated {date})"
- `reference` memories older than 90 days → flag: "(may be stale — verify this reference is still accurate)"
- `user`/`feedback` memories → no staleness flag (permanent unless incorrect)

If a stale memory is flagged, offer: "Run /memory-forget to remove or /memory-capture to update it."

## Anti-patterns

### Reciting Full Memory Bodies

```
# BAD — dumps entire memory content
"Here are all 12 memories I found..."
[pastes full body of each memory]

# GOOD — compact reference format
"Found 3 relevant memories:
[feedback] avoid-db-mocking — Integration tests must use real DB → feedback_testing.md
[reference] linear-ingest — Pipeline bugs tracked in Linear INGEST → reference_linear.md"
```

### Skipping Recall When It Would Help

```
# BAD — implements code that violates a known feedback rule because recall wasn't run
User: "Add a test for the payment handler"
Claude: *writes test with mocked database* ← should have recalled the anti-mocking rule

# GOOD — auto-recall before implementing in a known domain
memory_search("payment handler test") → surfaces anti-mocking feedback memory
Claude: *writes test using real database per the captured rule*
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| User invokes /memory-recall directly | Search with provided query or ask for one |
| Starting implementation in a known domain | Auto-recall with domain as query |
| User asks "what do you know about X" | Recall + display relevant memories |
| User starts debugging an area | Recall with module/feature name as query |
| No memories found for query | Tell user, offer /memory-capture |
| Recalled memory appears outdated | Flag it, offer to update or remove |
| Too many results (>8) | Show top 5 by score, offer to refine query |
| Multiple types returned | Group by type: feedback → user → project → reference |

## Execution

1. Parse `$ARGUMENTS` as the search query; if empty, ask "What would you like to recall memories about?"
2. Call `memory_search(query, limit=8, min_score=0.2)`
3. Filter out results below 0.3 relevance score
4. Check `updated` frontmatter on results: flag project memories >30 days old, reference memories >90 days old
5. Display results grouped by type (feedback → user → project → reference) using compact format
6. For stale hits: append "(may be stale — last updated {date})" and offer `/memory-forget` or `/memory-capture` to update
7. If no results: "No memories found for '{query}'. Use /memory-capture to start building memory for this area."
8. Apply recalled memories to inform the current task without reciting full body content

$ARGUMENTS
