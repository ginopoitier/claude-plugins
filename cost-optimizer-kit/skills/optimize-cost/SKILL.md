---
name: optimize-cost
description: >
  Audit and optimize Claude Code usage for token efficiency and cost reduction.
  Generates an optimized CLAUDE.md with smart kit routing, model selection hints,
  and context management patterns. Inspired by Ruflo's 30-50% token reduction
  techniques: ReasoningBank retrieval (-32%), smart routing, caching (-10%),
  batching (-20%), and model selection matrix.
  Load this skill when: "optimize cost", "reduce tokens", "too expensive", "cost too high",
  "token budget", "optimize claude", "cheaper", "reduce cost", "cost optimization",
  "token optimization", "kit efficiency", "claude cost", "billing", "token usage",
  "expensive", "cut costs", "lower bill", "api cost".
user-invocable: true
argument-hint: "[--audit|--generate|--model-guide|--all]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# Optimize Cost Skill

Audit the current project's Claude Code setup and apply token-efficiency optimizations.
Reduce API costs 30-50% through model selection, context discipline, caching, and batching.

---

## Argument Modes

| Argument | Action |
|----------|--------|
| `--audit` | Read existing CLAUDE.md, rules, and loaded kits; produce a scored efficiency report |
| `--generate` | Create a new optimized CLAUDE.md from scratch using the template |
| `--model-guide` | Print the model selection matrix and exit |
| `--all` (default) | Run audit, apply fixes inline, then print the model guide |

---

## Phase 1 — Discovery

When invoked, start by discovering the current setup:

```
1. Look for CLAUDE.md in the project root (current working directory)
2. Look for .claude/ directory and any project.config.md inside it
3. Glob for rules/ and knowledge/ directories
4. Count total loaded files and estimate token footprint
5. Check which kits are referenced via @-refs
```

### Discovery Checklist

- [ ] `CLAUDE.md` exists at project root?
- [ ] How many `@`-ref lines are in CLAUDE.md? (Each ref = file loaded into context)
- [ ] Are all `@`-refs pointing to files that exist?
- [ ] Are `rules/` loaded unconditionally (always-active)?
- [ ] Are `knowledge/` docs loaded unconditionally? (They should be on-demand only)
- [ ] Is there a model hint anywhere in CLAUDE.md?
- [ ] Is there a token budget directive?
- [ ] Are parallel task patterns documented?

---

## Phase 2 — Audit Report

Generate a scored report in this format:

```
## Cost Efficiency Audit
Project: [name from CLAUDE.md or directory name]
Date: [today]

### Context Loading                          Score: [A-F]
[findings]

### Model Selection Discipline               Score: [A-F]
[findings]

### Kit Selection (only what's needed)       Score: [A-F]
[findings]

### Prompt & Cache Efficiency                Score: [A-F]
[findings]

### Agent/Batching Patterns                  Score: [A-F]
[findings]

### Overall GPA: [0.0-4.0]
```

#### Scoring Rubric

**Context Loading**
- A: Only rules/ loaded always; knowledge/ behind @-refs called on demand; no duplicate content
- B: Rules loaded always; a few knowledge files unconditionally loaded
- C: Several large knowledge files always loaded
- D: Large knowledge + rule files always loaded with duplication
- F: Entire kits loaded unconditionally, massive context bloat

**Model Selection**
- A: Model hints present for each task category; Haiku used for simple tasks
- B: Some model hints present
- C: No model hints; default (Sonnet) used for everything
- D/F: Evidence of Opus used for tasks Haiku could handle

**Kit Selection**
- A: Only kits relevant to the project type are referenced
- B: One or two extra kits; low overlap
- C: 3+ irrelevant kits referenced
- F: All marketplace kits loaded regardless of stack

**Prompt & Cache Efficiency**
- A: System prompt stable (cache-friendly prefix); no dynamic content at top
- C: Dynamic timestamps/session data injected near top (cache busting)
- F: Large dynamic blocks at beginning of every context

**Agent/Batching**
- A: Parallel agent patterns documented; early-termination instructions present
- C: Sequential-only patterns; no batching guidance
- F: Explicit single-threaded instructions

---

## Phase 3 — Optimization Patterns

Apply these patterns when generating or editing the CLAUDE.md:

### 3.1 Model Selection Matrix

Add this section to CLAUDE.md:

```markdown
## Model Selection

| Task Type | Model | Reason |
|-----------|-------|--------|
| Simple Q&A, boilerplate lookup, grep/search | claude-haiku-4-5 | Fast, cheap, sufficient |
| Code generation, debugging, refactoring | claude-sonnet-4-5 | Balanced quality/cost |
| PR review, test coverage analysis | claude-sonnet-4-5 | Multi-file reasoning |
| Architecture review, security audit | claude-opus-4-5 | Deep reasoning required |
| ADR / design document authoring | claude-opus-4-5 | Nuanced trade-off analysis |
| One-liner fixes, typo corrections | claude-haiku-4-5 | Trivial; Opus wasteful |

**Rule:** Default to Sonnet. Escalate to Opus only when the task requires
multi-step architectural reasoning. Degrade to Haiku for lookup/boilerplate.
```

### 3.2 Context Window Discipline

```markdown
## Context Discipline

- NEVER load knowledge/ docs unconditionally — reference them with @-refs in skills only
- Load rules/ always (they're small and always relevant)
- Use skill-scoped @-refs: load a knowledge doc inside the skill that needs it
- NEVER copy-paste long reference text into CLAUDE.md — use @-refs
- Keep CLAUDE.md itself under 200 lines (it loads on every context)
- Remove any section that repeats information available in a loaded rule
```

### 3.3 Cache-Efficient Prompt Structure

Anthropic's prompt caching activates on repeated prefix blocks of 1024+ tokens.
Structure prompts to maximize cache hits:

```
[SYSTEM PROMPT — stable, never changes]      ← cache anchor
  Kit rules (always-active)
  Project conventions
  Tool definitions

[USER TURN — varies per request]             ← after cache boundary
  Task description
  Specific file contents
  Dynamic context
```

**DO:**
- Put stable instructions at the top of system/CLAUDE.md
- Put dynamic content (file contents, timestamps) in the user turn
- Reuse the same CLAUDE.md across sessions without editing it per-session

**DON'T:**
- Inject session IDs, timestamps, or user-specific data into CLAUDE.md
- Prepend dynamic content before stable rules
- Edit CLAUDE.md every session (each edit busts the cache)

### 3.4 Kit Selection Guide

Only include kits that match the project's actual technology stack:

```markdown
## Kit Selection by Project Type

### .NET / C# Project
Required: dotnet-kit, git-kit
Optional: github-kit OR bitbucket-kit (not both), jira-kit, confluence-kit
Skip: vue-kit, obsidian-kit

### Vue / TypeScript Project
Required: vue-kit, git-kit
Optional: github-kit OR bitbucket-kit (not both), jira-kit
Skip: dotnet-kit, obsidian-kit

### Documentation / Notes Project
Required: obsidian-kit, git-kit
Optional: confluence-kit
Skip: dotnet-kit, vue-kit

### Full-Stack (.NET + Vue)
Required: dotnet-kit, vue-kit, git-kit
Optional: jira-kit, github-kit OR bitbucket-kit
Skip: obsidian-kit (unless you document in Obsidian)

### Any Project
Always consider: cost-optimizer-kit (this kit)
Never load: kits whose stack doesn't appear in the project
```

### 3.5 Token Budget Rules

Add a token budget section to enforce discipline:

```markdown
## Token Budget

- **Prefer skills over rules:** Skills load only when invoked. Rules load always.
  Move seldom-used guidance into skills.
- **Prefer @-refs over inline text:** An @-ref costs ~1 token until dereferenced.
  Inline copy costs its full token count every context.
- **Early termination:** When an agent finds the answer, stop. Don't exhaust the
  context window collecting extra evidence.
- **Batch parallel work:** Issue all independent sub-tasks in one message.
  Sequential round-trips multiply cost.
- **Compress history:** Long conversations accumulate context. Use `/compact`
  or start a fresh session for unrelated tasks.
- **Scope file reads:** Read only the lines you need. Avoid reading entire large
  files when a targeted grep would find the answer.
```

### 3.6 Ruflo-Inspired Patterns

These patterns are derived from Ruflo's 30-50% token reduction approach:

#### ReasoningBank Pattern (-32% tokens)

Store previously-solved reasoning patterns and retrieve them instead of re-reasoning:

```markdown
## ReasoningBank Usage

When approaching a task Claude has done before in this project:
1. Check memory for prior reasoning: "Has this type of task been solved before?"
2. If yes, retrieve the pattern and apply it directly — skip re-derivation
3. Store new reasoning patterns after completing novel tasks

Use memory-kit's /memory-capture after solving complex problems.
This reuse can cut reasoning tokens by ~32% on repeated task types.
```

#### Smart Routing Pattern

Route tasks to the cheapest model that can handle them:

```markdown
## Smart Task Routing

Before starting any task, classify it:

SIMPLE (→ Haiku):
- "What does this function do?"
- "Generate boilerplate for X"
- "Find all files matching pattern Y"
- "Format this JSON"
- "What's the syntax for X?"

MEDIUM (→ Sonnet):
- "Implement feature X"
- "Debug why test Y fails"
- "Refactor this class"
- "Write tests for module Z"
- "Review this PR diff"

COMPLEX (→ Opus):
- "Design the architecture for X"
- "Audit this codebase for security vulnerabilities"
- "Should we use approach A or B? Analyze trade-offs."
- "Review this ADR and identify risks"
```

#### Batching Pattern (-20% tokens)

```markdown
## Batching Rules

- Issue all independent reads in one tool call batch, not sequentially
- When multiple files need analysis, request analysis of all of them together
- Use parallel agent spawns for independent sub-investigations
- Combine related questions into one message rather than separate turns
- Example: "Read files A, B, C and tell me how they interact" beats three separate reads
```

---

## Phase 4 — Generate Optimized CLAUDE.md

When `--generate` is passed or no CLAUDE.md exists, produce this template:

```markdown
# [Project Name]

> **Cost Optimizer:** Token-efficient configuration. Last optimized: [date].

## Stack
- [Language/Framework]
- [Database]
- [Key libraries]

## Model Selection

| Task Type | Use Model |
|-----------|-----------|
| Search, lookup, boilerplate | claude-haiku-4-5 |
| Code generation, debugging | claude-sonnet-4-5 |
| Architecture, security audit | claude-opus-4-5 |

**Default:** Sonnet. Escalate to Opus only for deep architectural reasoning.

## Always-Active Rules

<!-- Only reference small, always-relevant rule files here -->
<!-- Each @-ref loads the full file into every context — keep this list tight -->
@~/.claude/rules/[kit-name]/[rule].md

## Token Budget

- Prefer skills over always-active rules for seldom-used guidance
- Use @-refs, never inline long reference text
- Batch independent tool calls in one message
- Stop agents early when the answer is found
- Compress long conversations before starting new topics

## Kit Selection

<!-- List only kits relevant to this project's stack -->
Active kits: [kit-1], [kit-2]
Excluded: [kits not relevant to this stack]

## Skills Available

<!-- Document skills here so Claude knows what tools exist -->
[List skills and their /command triggers]

## Context Discipline

- knowledge/ docs are on-demand — reference via @-refs inside skills, not here
- CLAUDE.md stays under 200 lines
- No session-specific or dynamic content in this file (preserves cache)
```

---

## Phase 5 — Inline Editing

When `--all` mode is active and CLAUDE.md already exists:

1. Read the existing file
2. Identify inefficiencies from the audit
3. Apply edits:
   - Add model selection table if missing
   - Add token budget section if missing
   - Convert any inline knowledge text to @-ref suggestions (comment with TODO)
   - Add batching/parallel guidance if missing
   - Remove or flag duplicate content
4. Write the updated file
5. Print a summary of changes made

### Edit Safety Rules

- NEVER delete user content without showing what will be removed first
- NEVER change the kit's functional instructions — only add optimization sections
- ALWAYS preserve existing @-refs and skill lists
- Comment deprecated patterns with `<!-- COST: consider removing — [reason] -->` rather than deleting

---

## Phase 6 — Output Report

After completing all phases, print:

```
## Cost Optimization Complete

### Changes Made
- [list of edits applied]

### Estimated Savings
- Model routing: [X]% (if model hints added)
- Context reduction: [X] tokens removed from always-loaded content
- Cache efficiency: [improved/unchanged/degraded]

### Recommended Next Steps
1. [highest-impact remaining action]
2. [second action]
3. [third action]

### Model Guide Summary
[print the model selection table]
```

---

## Reference: Full Model Selection Matrix

| Task | Recommended Model | Cost Tier | Why |
|------|-------------------|-----------|-----|
| Read a file and summarize | Haiku | $ | Simple comprehension |
| Find a function / symbol | Haiku | $ | Pattern matching |
| Generate CRUD boilerplate | Haiku | $ | Template substitution |
| Fix a typo / formatting | Haiku | $ | Trivial edit |
| Implement a new feature | Sonnet | $$ | Multi-step generation |
| Debug a failing test | Sonnet | $$ | Reasoning + code |
| Write unit tests | Sonnet | $$ | Pattern + code gen |
| Refactor a class | Sonnet | $$ | Structured transformation |
| PR review (code quality) | Sonnet | $$ | Multi-file reasoning |
| Performance optimization | Sonnet | $$ | Analysis + rewrite |
| Security vulnerability scan | Opus | $$$ | Deep adversarial reasoning |
| Architecture design | Opus | $$$ | System-level trade-offs |
| ADR authoring | Opus | $$$ | Nuanced analysis |
| Tech debt prioritization | Opus | $$$ | Strategic reasoning |
| Incident post-mortem | Opus | $$$ | Root cause + systemic view |

**Cost tiers (approximate, input tokens):**
- Haiku ($): ~$0.80/MTok input
- Sonnet ($$): ~$3/MTok input
- Opus ($$$): ~$15/MTok input

Using Haiku instead of Opus for simple tasks = **~19x cost reduction per token**.

---

## Reference: Token Cost by Content Type

| Content | Approx Tokens | Notes |
|---------|---------------|-------|
| Empty CLAUDE.md | ~0 | Nothing loaded |
| Minimal CLAUDE.md (rules only) | ~200-500 | Ideal |
| Average CLAUDE.md with @-refs | ~500-1000 | Good |
| CLAUDE.md with inlined knowledge | ~2000-5000 | Expensive |
| Full kit CLAUDE.md (dotnet-kit) | ~1500 | Acceptable (it's the main kit) |
| knowledge/ doc (detailed reference) | ~2000-8000 | Load on-demand only |
| All kits loaded unconditionally | ~15000+ | Very expensive |
| Tool definitions (MCP server) | ~500-2000 | Fixed per MCP server |
| Conversation history (long session) | ~5000-50000 | Compress or reset |

---

## Reference: Quick Wins Checklist

Run through this list and fix each item that applies:

- [ ] **Model hints present** — Add a model selection table to CLAUDE.md
- [ ] **Knowledge docs on-demand** — Move any `knowledge/*.md` @-refs from CLAUDE.md into the skills that need them
- [ ] **Kit list pruned** — Remove kits not relevant to the project stack
- [ ] **No dynamic content in CLAUDE.md** — Remove session IDs, timestamps, user names
- [ ] **Token budget section present** — Add if missing
- [ ] **Parallel task patterns documented** — Note where to batch vs. sequence
- [ ] **Early termination enabled** — Instruct agents to stop when the answer is found
- [ ] **memory-kit in use** — Use ReasoningBank pattern for repeated task types
- [ ] **Conversation compacted** — Long conversations should be reset for new topics
- [ ] **@-refs preferred over inline text** — Any block >50 tokens that's referenced repeatedly should be extracted to a file

## Execution

1. Parse `$ARGUMENTS` — detect mode: `--audit`, `--generate`, `--model-guide`, `--all` (default)
2. **--model-guide**: print the model selection matrix and exit
3. **--audit / --all**: run Phase 1 Discovery (find CLAUDE.md, @-refs, rules/, knowledge/, kit list)
4. Run Phase 2 Audit Report — score each dimension (Context Loading, Model Selection, Kit Selection, Prompt & Cache, Agent/Batching), compute GPA
5. **--generate**: produce optimized CLAUDE.md from Phase 4 template in the current project root
6. **--all**: apply Phase 5 inline edits to existing CLAUDE.md (add model table, token budget section, flag deprecated patterns with comments)
7. Print Phase 6 Output Report: changes made, estimated savings, recommended next steps

$ARGUMENTS
