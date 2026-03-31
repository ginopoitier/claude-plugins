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
allowed-tools: []
---

# Context Discipline

Manage the 200k token context window as a finite resource. Every token spent reading an unnecessary file is a token not available for reasoning.

## Core Principle

**Never read a file to answer a question that a targeted tool call can answer.**

| Task | Expensive ❌ | Efficient ✅ |
|------|------------|-------------|
| Find a component definition | Read every .vue file | `Grep` for component name |
| Understand token structure | Read all CSS files | `Grep` for `@theme` block |
| Find token usages | Read every template | `Grep` for `var(--color-` |
| Check Figma MCP availability | Read plugin.json | Check MCP tool list in context |

**Rough costs:**
- Reading a 200-line Vue file ≈ 800–1,200 tokens
- A Glob or Grep call ≈ 20–80 tokens
- An MCP tool call ≈ 50–200 tokens

## Load Skills Lazily

Only load knowledge files when the current task requires them.

```
Working on design tokens? Load knowledge/design-tokens-guide.md
Building a component? Load knowledge/vue-component-patterns.md
Setting up Tailwind? Load knowledge/tailwind-v4-patterns.md
```

Never load all knowledge files at the start of a session.

## Subagent Delegation Rules

Spawn a subagent (Explore agent) when:
- Scanning a large component library for violations
- Running broad design consistency analysis across many files
- Doing Figma API research that doesn't need to return full details

Stay in main context when:
- Editing 1–3 specific component files
- Generating a single component from a spec
- Running a targeted audit on known files

## Execution

Apply context discipline automatically: prefer targeted tool calls, load knowledge lazily, delegate broad searches to subagents.

$ARGUMENTS
