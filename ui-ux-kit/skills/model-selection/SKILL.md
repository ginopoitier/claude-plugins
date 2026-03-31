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
| Design system architecture decisions | Opus 4.6 | Trade-offs, incomplete requirements |
| Token naming strategy, semantic mapping | Opus 4.6 | Judgment with long-term consequences |
| Scaffolding a Vue component | Sonnet 4.6 | Pattern-following, well-defined |
| Generating Tailwind theme from tokens | Sonnet 4.6 | Systematic transformation |
| Running a11y or design audit | Sonnet 4.6 | Rule application over many files |
| Figma file exploration, token extraction | Sonnet 4.6 | Structured extraction task |
| File lookups, Glob/Grep searches | Haiku 4.5 | Simple retrieval |
| Listing components or token names | Haiku 4.5 | Enumeration, no reasoning |
| Checking if a file exists | Haiku 4.5 | Trivial lookup |
| Reviewing full design system for consistency | Opus 4.6 | Broad judgment, many trade-offs |

## Workflow for Large Design System Tasks

1. **Opus** — design the token architecture, define semantic layers, identify component tiers
2. **Sonnet** — execute: generate tokens, scaffold components, build style guide
3. **Opus** — review for consistency, accessibility gaps, and naming issues

## Anti-patterns

- Don't use Opus to scaffold a Button component — Sonnet knows the pattern
- Don't use Haiku for design system architecture — false economy
- Don't run all subagents on Opus — Haiku is sufficient for file reads

## Execution

Evaluate the current task and select: Opus for design decisions and architecture, Sonnet for implementation, Haiku for subagent lookups.

$ARGUMENTS
