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
Working on database queries? → @~/.claude/knowledge/{kit}/db-patterns.md
Configuring CI/CD? → @~/.claude/knowledge/{kit}/cicd-patterns.md
Building a skill? → @~/.claude/knowledge/kit-maker/skill-writing-guide.md

# BAD — preloading everything before work begins
@~/.claude/knowledge/{kit}/db-patterns.md          (4,000 tokens every session)
@~/.claude/knowledge/{kit}/cicd-patterns.md        (3,500 tokens every session)
@~/.claude/knowledge/{kit}/testing-patterns.md     (2,000 tokens every session)
... 20 more files loaded before any task is discussed
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
  - Glob project structure
  - Read 2–3 key files

Phase 2: Plan (~2k tokens)
  - Outline approach, identify files to touch

Phase 3: Implement (~15k tokens)
  - Write/edit files one at a time

Phase 4: Verify (~3k tokens)
  - dotnet build + dotnet test + diff review

If Phase 3 context is filling → delegate remaining exploration to subagent
```

## Anti-patterns

### Reading Full Files for Single Facts

```bash
# BAD — reads 400 lines to find one function
Read: src/services/payment-service.ts

# GOOD — targeted search, then read only the relevant lines
Grep: "function processPayment" → finds file + line number in 1 call
Read: payment-service.ts (offset: 45, limit: 30)
```

### Preloading Knowledge Base in CLAUDE.md

```markdown
# BAD — always-loaded block in CLAUDE.md
## Always Load
@~/.claude/knowledge/{kit}/db-patterns.md           # 4,000 tokens every session
@~/.claude/knowledge/{kit}/testing-patterns.md      # 3,500 tokens every session
# = 7,500 tokens wasted whenever these topics aren't relevant

# GOOD — referenced from the skill that needs them
# db-patterns.md loads only when handling database work
# testing-patterns.md loads only when writing tests
```

### Exploring in Main Context When Subagent Would Do

```markdown
# BAD — search produces 47 matches, fills main context
Grep "BaseController" → 47 matches across 23 files (fills context with noise)
Read each file individually...

# GOOD — delegate
Agent (Explore/Haiku): "Find all BaseController subclasses,
summarize the patterns used. Return a 200-token summary."
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Finding a class/method by name | `Grep` first — never `Read` the whole file |
| Understanding project structure | `Glob "**/*.csproj"` then read those files |
| First look at unfamiliar codebase | Spawn Explore subagent (Haiku) |
| Broad search producing lots of output | Subagent — protect main context |
| Result needed for immediate next step | Stay in main context |
| Context > 150k tokens deep into task | Delegate remaining exploration to subagent |
| Loading knowledge docs | Lazy — load only when task requires them |
| Same Grep called twice | Stop — reuse the first result |

## Deep Reference
For token cost tables and subagent budget patterns: @~/.claude/knowledge/kit-maker/cost-optimization.md
