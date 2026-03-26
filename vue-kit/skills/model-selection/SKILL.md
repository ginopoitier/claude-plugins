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

## Decision Table

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
