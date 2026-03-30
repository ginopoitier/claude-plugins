---
name: scaffold-agent
description: >
  Interactive wizard for creating a new specialized Claude Code agent definition.
  Agents are scoped subprocesses with specific tools, models, and task boundaries.
  Load this skill when: "create an agent", "new agent", "scaffold agent", "add agent",
  "agent definition", "subagent", "specialist agent", "autonomous agent",
  "XML sections agent", "structured agent", "complex agent", "orchestrator agent",
  "color field", "tools field".
user-invocable: true
argument-hint: "[agent role or domain]"
allowed-tools: Read, Write, Glob
---

# Scaffold Agent

## Core Principles

1. **Agents are specialists, not generalists** — An agent must have a clearly defined task scope: what it does AND what it explicitly does not do. An agent with no boundaries will be used incorrectly.
2. **Tool scope is security** — Only grant the tools the agent actually needs. An agent that only reads files should not have `Write` or `Bash`.
3. **Output format is a contract** — The agent must declare exactly what it returns to the calling context. Undefined output leads to unparseable responses.
4. **Cost-match model to complexity** — Most agents should use `model: haiku` (exploration, lookups, summaries). Reserve `model: sonnet` for implementation agents and `model: opus` for architecture review.
5. **Self-contained prompt design** — Agents don't share context with the parent. The agent definition must be completable with only what the parent passes in the prompt.

## Patterns

### Agent Definition Structure

```markdown
---
name: {agent-name}
description: >
  One paragraph: what this agent does, when to use it vs. staying in main context.
  Spawned by: /command, orchestrator, or user.
model: haiku|sonnet|opus       # or inherit (default)
tools: [allowlist]             # or use disallowedTools for denylist
color: green|yellow|blue|purple|red|orange
# permissionMode: acceptEdits  # uncomment for executor agents
# maxTurns: 20                 # uncomment to cap agentic turns
# skills:                      # uncomment to preload skill content
#   - skill-name
# memory: user                 # uncomment for persistent memory (user|project|local)
# effort: high                 # uncomment to override session effort
# isolation: worktree          # uncomment to run in isolated git worktree
# initialPrompt: "..."         # uncomment to auto-submit first turn
---

# {Agent Name}

## Task Scope
**Does:** [specific tasks this agent performs]
**Does not:** [explicit exclusions — what stays in main context]

## Input Format
[What the calling context must pass in the prompt]

## Output Format
[Exactly what this agent returns — structure, format, length]

## Execution Steps
1. ...
2. ...

## When to Use vs. Stay in Main Context
| Use this agent | Stay in main context |
|---|---|
| ... | ... |
```

### Wizard Flow

**Step 1 — Define the role**
Ask: "What is the agent's specialized domain? What one task should it do exceptionally well?"

Good agent roles:
- `code-reviewer` — reviews changed files for issues
- `skill-writer` — writes complete SKILL.md from a description
- `kit-auditor` — audits a kit directory for quality
- `security-auditor` — scans for vulnerabilities
- `performance-analyst` — finds bottlenecks

Bad agent roles (too broad):
- `helper` — what does it help with?
- `codebase-agent` — does what, exactly?

**Step 2 — Define exact scope boundaries**

```markdown
## Task Scope
**Does:**
- Read all SKILL.md files in a given kit directory
- Score each skill on the 7-dimension rubric
- Produce a ranked list with letter grades

**Does not:**
- Write or modify any files
- Run build commands
- Make architectural decisions
```

**Step 3 — Choose the model**

```markdown
# Haiku — file reading, summarizing, lookups, exploration
model: haiku

# Sonnet — implementation, code generation, multi-step writing
model: sonnet

# Opus — architecture review, security audit, subtle judgment calls
model: opus
```

**Step 4 — Scope the tools**

```markdown
# Read-only agent (exploration, audit, review)
allowed-tools: Read, Glob, Grep, Bash

# Writing agent (generates files)
allowed-tools: Read, Write, Edit, Glob, Grep

# Full-capability agent (build, test, fix)
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
```

**Step 5 — Define input and output format**

```markdown
## Input Format
The calling context passes a prompt containing:
- Absolute path to the kit directory to audit
- (Optional) Specific dimension to focus on

## Output Format
Returns a single structured report:
\```
Kit Audit: {kit-name}
=========================
[Dimension scores]
Overall GPA: X.X (Grade)
Priority Fixes: [list]
\```
Maximum response length: ~800 tokens.
```

**Step 6 — Write execution steps**

```markdown
## Execution Steps
1. List all directories under `{kit-path}/skills/`
2. For each skill directory, read `SKILL.md`
3. Score on 7 dimensions using the rubric in quality-standards.md
4. Calculate GPA and assign letter grade
5. Rank by score, list bottom 3 for priority fixes
6. Return structured report
```

**Step 7 — Place the file**
```
{kit-name}/agents/{agent-name}.md
```

Agents install to `~/.claude/agents/` (flat namespace, shared across kits).

### Example: kit-auditor Agent

```markdown
---
name: kit-auditor
description: >
  Comprehensive kit quality reviewer. Audits a Claude Code kit directory for structure,
  skill coverage, rule quality, manifest validity, and installability.
  Use when running /kit-health-check or reviewing a kit before publishing.
model: haiku
allowed-tools: Read, Glob, Grep, Bash
---

# Kit Auditor

## Task Scope
**Does:** Full 8-dimension audit of a kit directory. Returns graded report card.
**Does not:** Modify any files, make architecture decisions, or run build commands.

## Input Format
Calling context provides the absolute path to the kit directory and optionally a
focus dimension (structure | skills | installability | all).

## Output Format
Structured markdown report card with:
- Letter grade per dimension
- Overall GPA
- Prioritized fix list (top 5 issues)
Maximum: ~1,000 tokens.

## Execution Steps
1. `Glob` the kit root for required structure (CLAUDE.md, rules/, skills/, knowledge/)
2. Read CLAUDE.md, validate all `@` references resolve to existing files
3. For each skill dir: read SKILL.md, check frontmatter completeness and section presence
5. Check hooks for executability via `ls -la hooks/`
6. Produce graded report
```

### Pattern B — Structured XML Sections (Complex Agents)

Use for agents over 200 lines, orchestrators, or agents needing strong behavioral guardrails.

```markdown
---
name: {agent-name}
description: >
  {Role definition}.
  Spawned by: {trigger}.
tools: Read, Write, Edit, Bash, Glob, Grep
color: {green|yellow|blue|purple|red|orange}
---

<role>
You are a {role}. You {main responsibility}.

Spawned by:
- `/{command}` (primary trigger)
- `/{command} --{flag}` (variant)

Your job: {one-sentence job description}.
</role>

<project_context>
Before acting, discover project context:

1. Read `./CLAUDE.md` if it exists — follow all project guidelines
2. Check `.claude/skills/` for available skills
3. Do NOT load full AGENTS.md files (100KB+ context cost)
</project_context>

<philosophy>
## {Agent Worldview Title}

{2-3 paragraphs defining how this agent makes decisions}

## Quality Rule
{One key quality constraint — e.g., "Plans complete within 50% context"}
</philosophy>

<context_fidelity>
## User Decision Fidelity

Locked decisions in `<user_decisions>` are NON-NEGOTIABLE:
- If user said "use X" → use X, not an alternative

Self-check before returning:
- [ ] All locked decisions are implemented
- [ ] No deferred ideas appear in output
</context_fidelity>
```

**When to use Pattern A vs Pattern B:**

| Use Pattern A (prose) | Use Pattern B (XML) |
|----------------------|---------------------|
| Single responsibility | Multiple responsibilities |
| Under 200 lines | Over 200 lines |
| Simple task execution | Complex decision logic |
| Utility/helper agents | Orchestrators |
| Clear, linear workflow | Branching behavior with constraints |

## Anti-patterns

### Agent With No Boundaries

```markdown
# BAD — no scope definition
## Task Scope
This agent helps with code quality and can fix things.

# GOOD — explicit does/does not
## Task Scope
**Does:** Review staged changes in the provided diff for security vulnerabilities.
**Does not:** Fix issues, run tests, or review files not in the diff.
```

### Wrong Model for the Task

```markdown
# BAD — Opus for file lookups (expensive, no benefit)
model: opus
allowed-tools: Read, Glob
# This agent reads files and summarizes them

# GOOD — Haiku for read-and-summarize
model: haiku
allowed-tools: Read, Glob
```

### Undefined Output Format

```markdown
# BAD — caller doesn't know what to expect
## Output Format
Returns findings from the audit.

# GOOD — structured contract
## Output Format
Returns a single markdown table: | File | Issue | Severity |
followed by a one-paragraph summary.
Maximum: 500 tokens. No additional prose.
```

## Decision Guide

| Scenario | Agent model |
|----------|-------------|
| Read files + summarize findings | Haiku |
| Explore unfamiliar codebase | Haiku (Explore subagent type) |
| Write complete SKILL.md files | Sonnet |
| Full feature implementation | Sonnet |
| Architecture review with trade-offs | Opus |
| Security audit requiring judgment | Opus |
| Kit health check (read + grade) | Haiku |
| Code review of a PR | Sonnet |
| Agent needs < 3 distinct tools | Restrict to only those tools |
| Agent reads files it might modify | Separate read and write agents |
