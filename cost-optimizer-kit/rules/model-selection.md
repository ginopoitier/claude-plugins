---
rule: model-selection
description: Always select the appropriate model tier for the task complexity
always-active: true
---

# Model Selection Rule

## DO

- **Use Haiku** for: simple Q&A, boilerplate generation, file lookup, grep/search tasks,
  syntax questions, format conversions, and any task with a clear template-based answer
- **Use Sonnet** for: code generation, debugging, refactoring, writing tests, PR review,
  implementing features, analyzing diffs, and most day-to-day development work
- **Use Opus** for: architecture design, security audits, ADR authoring, tech debt
  prioritization, complex trade-off analysis, and incident post-mortems
- **Default to Sonnet** when task complexity is unclear — it handles the majority of
  development tasks well at a reasonable cost
- **Escalate to Opus** only when the task genuinely requires multi-step strategic reasoning
  that Sonnet demonstrably struggles with
- **Degrade to Haiku** proactively for lookup-style tasks even if the user doesn't ask —
  it is significantly cheaper and fast enough for simple responses

## DON'T

- **Don't use Opus** for tasks that Haiku or Sonnet can handle — Opus costs ~5x Sonnet
  and ~19x Haiku per token
- **Don't use Opus** for: reading a file, generating boilerplate, fixing a typo,
  writing a simple utility function, or answering a factual lookup question
- **Don't default to Opus** "to be safe" — safety is about correctness, not model tier
- **Don't ignore model hints** in the project's CLAUDE.md — they exist for a reason
- **Don't use the same model for every task** regardless of complexity

## Model Tiers at a Glance

| Model | Use When | Approximate Cost |
|-------|----------|-----------------|
| claude-haiku-4-5 | Simple, template-based, lookup | $ (cheapest) |
| claude-sonnet-4-5 | Code generation, reasoning, review | $$ (standard) |
| claude-opus-4-5 | Architecture, security, deep analysis | $$$ (premium) |

## Task Classification Quick Reference

**→ Haiku:**
- "What does this function do?"
- "Generate a CRUD controller"
- "Find all files matching *.test.ts"
- "What's the syntax for X in C#?"
- "Format this JSON"
- "Add a docstring to this method"

**→ Sonnet:**
- "Implement feature X"
- "Debug why this test fails"
- "Refactor this class to use the repository pattern"
- "Write integration tests for this endpoint"
- "Review this PR for code quality issues"
- "Optimize this query"

**→ Opus:**
- "Design the data model for a multi-tenant SaaS system"
- "Audit this authentication flow for security vulnerabilities"
- "Should we use event sourcing or CRUD for this domain?"
- "Write an ADR comparing approach A vs B"
- "What are the systemic risks in this architecture?"
