---
name: model-selection
description: >
  Guide for picking the right Claude model per task — Opus 4.6 for architecture and hard
  problems, Sonnet 4.6 for implementation, Haiku 4.5 for subagent work and quick lookups.
  Load this skill when: "which model", "model selection", "use Opus", "use Haiku", "use Sonnet",
  "step up model", "subagent model", "model cost", "Opus vs Sonnet", "model tier",
  "expensive model", "cheap model", "escalate to Opus".
user-invocable: false
allowed-tools: []
---

# Model Selection

Match model to task complexity, not perceived importance.

## Core Principles

1. **Complexity drives cost, not importance** — A critical bug with a clear fix is a Sonnet task. An ambiguous architectural trade-off is Opus even if it seems minor.
2. **Haiku for retrieval, Sonnet for execution, Opus for judgment** — These are not tiers of quality, they are tiers of reasoning depth.
3. **Subagents default to Haiku** — File reads, Glob/Grep searches, and script execution do not need reasoning. Reserve Sonnet/Opus for the orchestrating agent.
4. **Step up when you're stuck** — If Sonnet can't resolve an ambiguous root cause after two attempts, switch to Opus. Don't grind.

## Patterns

### Architecture and Trade-off Tasks (Opus)

```
# Use Opus when the task requires:
- Evaluating multiple valid approaches with real trade-offs
- Diagnosing production issues with incomplete information
- Reviewing code for subtle correctness or architectural compliance
- Designing domain models with complex invariants
```

### Implementation Tasks (Sonnet)

```
# Use Sonnet when the task requires:
- Implementing a feature that follows an established pattern in the codebase
- Writing xUnit tests for a known acceptance criterion
- Scaffolding from a template (CQRS handler, endpoint, migration)
- Fixing a bug with a clear root cause already identified
```

### Retrieval and Execution Tasks (Haiku)

```
# Use Haiku when the task requires:
- Searching for a file, class, or method name
- Running a dotnet build, dotnet test, or git command
- Summarizing a large log file or JSON response
- Spawning a subagent to explore a section of the codebase
```

## Decision Guide

| Task | Model | Why |
|------|-------|-----|
| Architecture design, trade-off analysis | Opus 4.6 | Needs deep reasoning, incomplete info |
| Feature implementation following patterns | Sonnet 4.6 | Pattern execution, well-defined |
| Bug fix with clear root cause | Sonnet 4.6 | Known solution space |
| Scaffolding / boilerplate generation | Sonnet 4.6 | Repetitive, pattern-following |
| Writing xUnit tests | Sonnet 4.6 | Systematic, template-based |
| File lookups, Glob/Grep searches | Haiku 4.5 | Simple retrieval |
| Running scripts, bash commands | Haiku 4.5 | Execution, no reasoning needed |
| Summarizing large output | Haiku 4.5 | Compression task |
| Subagent codebase exploration | Haiku 4.5 | Exploration + summary |
| Subtle debugging, unknown root cause | Opus 4.6 | Needs broad reasoning |
| Reviewing architectural compliance | Opus 4.6 | Judgment with trade-offs |

## Effective Workflow for Large Features

1. **Opus** — design the approach, define acceptance criteria, identify risks
2. **Sonnet** — execute the implementation plan
3. **Opus** — review for architectural compliance and edge cases

## Anti-patterns

- Don't use Opus for scaffolding a CRUD endpoint — Sonnet knows the pattern
- Don't use Haiku for architecture decisions — false economy
- Don't run all subagents on Opus — Haiku is sufficient for file reads and searches
- Don't use Sonnet for debugging production incidents with incomplete logs — step up to Opus

## Execution

Evaluate the current task against the decision table and select the appropriate model tier — Opus for reasoning-heavy tasks, Sonnet for implementation, Haiku for subagent work — then proceed accordingly.

$ARGUMENTS
