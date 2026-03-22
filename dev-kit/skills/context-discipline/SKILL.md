---
name: context-discipline
description: >
  Token management rules Claude follows automatically — lazy loading, subagent delegation,
  targeted queries over full file reads. Guides how Claude uses its context window efficiently
  across all sessions.
  Load this skill when: "context window", "token budget", "lazy loading", "subagent delegation",
  "file reads", "context filling", "Grep vs Read", "context discipline", "token cost",
  "context management", "avoid reading files", "context overflow".
user-invocable: false
---

# Context Discipline

Manage the 200k token context window as a finite resource. Every token spent reading an unnecessary file is a token not available for reasoning.

## Core Principle

**Never read a file to answer a question that a targeted tool call can answer.**

| Task | Expensive ❌ | Efficient ✅ |
|------|------------|-------------|
| Find a class definition | Read entire file | `Grep` for class name |
| Understand project structure | Read all `.cs` files | `Glob` for `*.csproj` + read those |
| Find all usages of a method | Read every file | Roslyn `find_references` (15 tokens) |
| Get a method signature | Read the class file | Roslyn `get_symbol_detail` (30 tokens) |
| Check for compilation errors | Run build and read output | Roslyn `get_diagnostics` (50 tokens) |

**Rough costs:**
- Reading a 200-line file ≈ 600–1,000 tokens
- A Glob or Grep call ≈ 20–80 tokens
- A Roslyn MCP query ≈ 30–150 tokens

## Load Skills Lazily

Only load knowledge files when the current task requires them. Do not preload everything.

```
Working on an EF Core query? Load ef-core-patterns.md
Writing an endpoint? Load minimal-api-patterns.md
```

Never load all knowledge files at the start of a session.

## Subagent Delegation Rules

Spawn a subagent (Explore agent) when:
- Exploring an unfamiliar codebase for the first time
- Running a broad search that will produce verbose output
- Doing research that doesn't need to come back to the main context in full detail

Stay in main context when:
- Making targeted edits to 1–3 known files
- Quick lookups where you already know the file

## Budget Awareness

For large implementation tasks, rough phase budgets:
- Understand the codebase: ~5k tokens
- Plan the approach: ~2k tokens
- Implement: ~15k tokens
- Verify (build + test): ~3k tokens

If you're deep into implementation and context is filling up, delegate remaining exploration to subagents and summarize findings back into the main thread.

## Anti-patterns

- Don't `Read` an entire 500-line file to find one method — use `Grep` first
- Don't load the full knowledge base upfront — load on demand
- Don't explore in the main thread when an Explore subagent would do
- Don't repeat tool calls — cache results in your working notes
