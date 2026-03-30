---
name: context-discipline
description: >
  Token management rules Claude follows automatically — lazy loading, subagent delegation,
  targeted queries over full file reads. Guides how Claude uses its context window efficiently.
  Load this skill when: "context window", "token budget", "lazy load", "context efficiency",
  "subagent delegation", "reading too many files", "context filling up", "token cost".
user-invocable: false
allowed-tools: Read, Grep, Glob, Bash
---

# Context Discipline

## Core Principles

1. **Never read a file to answer a question a targeted tool call can answer** — `Grep` costs 20–80 tokens; reading a 200-line file costs 600–1,000 tokens. Use the cheap tool first.
2. **Load skills and knowledge lazily** — Only load `@` knowledge references when the current task requires them. Never preload the full knowledge base at session start.
3. **Protect the main context with subagents** — Broad searches with verbose output belong in subagents. The main context is for reasoning, not storing tool results.
4. **Cache tool results in working notes** — Don't call the same `Grep` or `Glob` twice. Write the result down and reuse it.
5. **Budget awareness on large tasks** — For features requiring 3+ files, rough-plan token phases upfront and delegate exploration when context is filling.

## Patterns

### Tool Cost Comparison

| Task | Expensive ❌ | Efficient ✅ | Savings |
|------|------------|-------------|---------|
| Find a class/function definition | Read entire file (~800 tokens) | `Grep` for the name (~30 tokens) | 27× |
| Understand project structure | Read all source files | `Glob` for manifests/config + read those | 10× |
| Find all usages of a function | Read every file | `Grep` for function name | 50× |
| Check build errors | Read all build output | Run build + read only error lines | 5× |
| Explore unfamiliar codebase | Explore in main context | Explore subagent (Haiku) | saves context |

### Lazy Loading Pattern

```
# GOOD — load only what the current task requires
Working on a query? → load relevant knowledge doc
Configuring a feature? → load relevant knowledge doc

# BAD — preloading everything before work begins
Loading all knowledge files before any task is discussed
```

### Subagent Delegation Rules

```
Spawn a subagent (Explore agent, model: haiku) when:
- Exploring an unfamiliar codebase for the first time
- Running a broad search that will produce > 100 lines of output
- Research that doesn't need to return to main context in full detail

Stay in main context when:
- Making targeted edits to 1–3 known files
- Quick lookup where you already know the file path
- The result must directly inform the next reasoning step
```

### Phase Budget for Large Tasks

```
Phase 1: Understand (~5k tokens)
Phase 2: Plan (~2k tokens)
Phase 3: Implement (~15k tokens)
Phase 4: Verify (~3k tokens)

If Phase 3 context is filling → delegate remaining exploration to subagent
```

## Anti-patterns

### Reading Full Files for Single Facts

```bash
# BAD — reads 400 lines to find one function
Read: src/some-service.ts

# GOOD — targeted search, then read only the relevant lines
Grep: "function processPayment" → finds file + line number in 1 call
Read: (offset: 45, limit: 30)
```

### Exploring in Main Context When Subagent Would Do

```markdown
# BAD — search produces 47 matches, fills main context
Grep "BaseClass" → 47 matches across 23 files (fills context with noise)

# GOOD — delegate
Agent (Explore/Haiku): "Find all BaseClass subclasses, summarize patterns. Return 200-token summary."
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Finding a class/method by name | `Grep` first — never `Read` the whole file |
| First look at unfamiliar codebase | Spawn Explore subagent (Haiku) |
| Broad search producing lots of output | Subagent — protect main context |
| Result needed for immediate next step | Stay in main context |
| Context > 150k tokens deep into task | Delegate remaining exploration to subagent |
| Loading knowledge docs | Lazy — load only when task requires them |
| Same Grep called twice | Stop — reuse the first result |
