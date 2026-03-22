---
name: model-selection
description: >
  Guide for picking the right Claude model per task — cost-matches model capability
  to task complexity. Opus 4.6 for architecture and judgment, Sonnet 4.6 for implementation,
  Haiku 4.5 for subagent work and lookups. Load this skill when: "which model", "model choice",
  "cost optimization", "agent model", "haiku vs sonnet", "opus vs sonnet", "subagent model".
user-invocable: false
allowed-tools: Read
---

# Model Selection

## Core Principles

1. **Match model to complexity, not perceived importance** — Scaffolding a CRUD endpoint is "important" but Sonnet knows the pattern. Architecture design for a distributed system needs Opus. Cost follows complexity.
2. **Haiku for all subagents that explore or summarize** — If a subagent reads files and returns a summary, Haiku is sufficient. Using Opus for file lookups is a 20× cost premium with no benefit.
3. **Opus only when judgment with incomplete information is required** — Architectural trade-offs, subtle debugging with partial logs, security audits requiring holistic reasoning. If the answer is deterministic from the code, Sonnet handles it.
4. **Step up when stuck** — If Sonnet produces a wrong answer twice on the same problem, escalate to Opus. Don't retry the same model expecting different results.
5. **Effective large features use all three** — Design (Opus) → Implement (Sonnet) → Explore subagents (Haiku). This is the cost-optimal workflow.

## Patterns

### Model Decision Table

| Task | Model | Why |
|------|-------|-----|
| Architecture design, trade-off analysis | Opus 4.6 | Incomplete info, judgment required |
| Feature implementation following patterns | Sonnet 4.6 | Known solution space, pattern execution |
| Bug fix with clear root cause | Sonnet 4.6 | Deterministic once root cause is known |
| Scaffolding / boilerplate generation | Sonnet 4.6 | Repetitive, template-following |
| Writing tests (unit, integration) | Sonnet 4.6 | Systematic, known patterns |
| File lookups, Glob/Grep searches | Haiku 4.5 | Simple retrieval, no reasoning |
| Running scripts, bash commands | Haiku 4.5 | Execution, no reasoning |
| Summarizing large tool output | Haiku 4.5 | Compression task |
| Subagent codebase exploration | Haiku 4.5 | Exploration + summary |
| Subtle debugging, unknown root cause | Opus 4.6 | Broad reasoning across many factors |
| Security audit | Opus 4.6 | Holistic judgment across codebase |
| Reviewing architectural compliance | Opus 4.6 | Trade-off judgment required |
| Refactoring with clear target | Sonnet 4.6 | Mechanical transformation |
| Writing documentation | Sonnet 4.6 | Pattern-following with known structure |

### Cost-Optimal Workflow for Large Features

```
1. Opus — design the approach
   - What architecture fits this feature?
   - What are the risks and trade-offs?
   - Define acceptance criteria

2. Sonnet — execute the plan
   - Write commands, handlers, endpoints, tests

3. Haiku subagents — during implementation
   - "Read and summarize these 5 files" → 200-token summary
   - "Find all usages of this pattern"
   - "Check current config values"

4. Opus (optional) — post-implementation review
   - Does this match the architecture?
   - Security and edge case review
```

### Subagent Model Selection

```yaml
# Haiku — read, summarize, lookup, grep, glob
subagent_type: Explore
model: haiku   # default for Explore agents

# Sonnet — write files, implement features
subagent_type: general-purpose
model: sonnet

# Opus — architecture, security, subtle judgment
subagent_type: Plan
model: opus    # default for Plan agents
```

### Step-Up Protocol

```
Attempt 1 (Sonnet): wrong answer
Attempt 2 (Sonnet): same wrong pattern
→ STOP. Step up to Opus.

Signs you need Opus:
- Multiple failed Sonnet attempts on same problem
- Problem requires reasoning about incomplete information
- Answer depends on subtle trade-offs between 3+ factors
- Debugging with only partial logs or no reproduction case
```

## Anti-patterns

### Opus for Deterministic Tasks

```markdown
# BAD — 20× cost, 0× benefit for a known pattern
subagent_type: general-purpose
model: opus
prompt: "Scaffold a standard REST endpoint using the project's existing patterns"

# GOOD — Sonnet knows the pattern
subagent_type: general-purpose
model: sonnet
prompt: "Scaffold a standard REST endpoint using the project's existing patterns"
```

### Haiku for Architecture Decisions

```markdown
# BAD — produces surface-level answer, misses critical nuances
model: haiku
prompt: "Should we use modular monolith or microservices for this multi-tenant SaaS
with 3 teams and 5 bounded contexts?"

# GOOD
model: opus
prompt: "Should we use modular monolith or microservices..."
```

### All Subagents on Opus

```markdown
# BAD — $0.15/session for tasks costing $0.001 with Haiku
Agent(model: opus): "Read 15 files in src/Application/ and summarize patterns"
Agent(model: opus): "Find all *.csproj files"
Agent(model: opus): "List ~/.claude/skills/ directories"

# GOOD — cost-match each subagent to its task
Agent(model: haiku): all of the above → 100× cheaper
```

## Decision Guide

| Scenario | Model |
|----------|-------|
| "Design the architecture for X" | Opus |
| "Implement feature X following existing pattern" | Sonnet |
| "Read these files and summarize" | Haiku |
| "Why is this test flaking? Here's partial log" | Opus |
| "Write xUnit tests for this handler" | Sonnet |
| "Summarize this 500-line build log" | Haiku |
| "Is this code secure?" | Opus |
| "Refactor to use the Result pattern" | Sonnet |
| Subagent: explore codebase | Haiku |
| Subagent: write a complete feature | Sonnet |
| Subagent: audit for compliance | Opus |
| Stuck after 2 Sonnet attempts | Step up to Opus |
