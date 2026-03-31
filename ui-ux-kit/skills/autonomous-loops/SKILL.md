---
name: autonomous-loops
description: >
  Bounded iteration for multi-component generation and bulk design system tasks.
  Prevents runaway loops by enforcing a maximum iteration count and surfacing blockers.
  Load this skill when: "generate all components", "process all tokens", "loop over",
  "batch generate", "for each component", "iterate", "autonomous", "bounded loop".
user-invocable: false
allowed-tools: [Read, Write, Edit, Bash, Glob, Grep]
---

# Autonomous Loops (UI/UX)

For bulk generation tasks (many components, full token sets, style guide sections), use bounded iteration.

## Loop Protocol

```
Define scope → Set max iterations → Iterate → Surface blockers → Summarize
```

1. **Define scope** — list all items to process (components, token groups, pages)
2. **Set max** — default 20 iterations; state the limit at the start
3. **Iterate** — process one item per loop; verify each before continuing
4. **Surface blockers** — if an item fails twice, skip it and note it; don't spin forever
5. **Summarize** — report what was generated, what was skipped, and why

## Anti-patterns

- Don't loop more than 20 iterations without pausing to summarize
- Don't retry the same failing generation more than twice — skip and report
- Don't lose track of what was generated — maintain a running list

## Example: Generate Component Library

```
Scope: [DsButton, DsInput, DsCard, DsModal, DsBadge, DsAlert] (6 components)
Max iterations: 6
Loop:
  1. DsButton → generate → verify → ✓
  2. DsInput → generate → verify → ✓
  3. DsCard → generate → verify → ✓
  ...
Summary: Generated 6/6 components. All passed verification.
```

## Execution

Before starting a bulk task, announce the scope and max. After each iteration, verify and record. Surface any blockers. Summarize at the end.

$ARGUMENTS
