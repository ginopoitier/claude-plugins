# Token Efficiency Reference

Deep reference guide for understanding and reducing Claude API costs.
Covers pricing mechanics, context management, caching, batching, and Ruflo-inspired patterns.

---

## 1. How Claude's Token Pricing Works

### What Is a Token?

A token is roughly 4 characters of English text, or about 0.75 words. Common benchmarks:
- 1 line of code ≈ 10–20 tokens
- 1 paragraph of prose ≈ 75–100 tokens
- 1 page of documentation ≈ 400–600 tokens
- 1 typical CLAUDE.md file ≈ 200–2000 tokens
- 1 average source file (200 lines) ≈ 2000–4000 tokens

### Billing Model

Anthropic charges separately for:
- **Input tokens:** Everything in the context window when a request is made
  (system prompt + conversation history + user message + tool results)
- **Output tokens:** The tokens Claude generates in response
- **Cache write tokens:** First time a block is cached (slightly higher than read)
- **Cache read tokens:** Subsequent uses of a cached block (~90% discount)

### Model Pricing (approximate, as of 2025)

| Model | Input | Output | Cache Read |
|-------|-------|--------|------------|
| claude-haiku-4-5 | $0.80/MTok | $4/MTok | $0.08/MTok |
| claude-sonnet-4-5 | $3/MTok | $15/MTok | $0.30/MTok |
| claude-opus-4-5 | $15/MTok | $75/MTok | $1.50/MTok |

**Key insight:** Output tokens cost 5x input tokens. Verbose responses are expensive.
Prefer concise, structured output over lengthy prose explanations.

### The Multiplier Effect

Every token in the context window is paid on *every* request. If your CLAUDE.md loads
3000 tokens of rules and knowledge, you pay for those 3000 tokens on every single turn.
A 20-turn conversation costs: 20 × 3000 = 60,000 tokens just for the static context.

This is why context discipline is the highest-leverage optimization.

---

## 2. Context Window Management

### What Counts Toward Context?

Everything in the context window on each API call:

1. **System prompt** — CLAUDE.md + all @-refs loaded (rules, knowledge, config)
2. **Tool definitions** — Every MCP server tool adds tokens to each request
3. **Conversation history** — All prior turns in the current session
4. **User message** — The current request
5. **Tool call results** — Output from file reads, searches, bash commands
6. **Assistant responses** — Claude's prior replies in the session

### Context Accumulation in Long Sessions

A 30-turn debugging session might accumulate:
- System prompt: 2,000 tokens (stable)
- Initial file reads: 8,000 tokens
- Tool results from debugging: 15,000 tokens
- Conversation back-and-forth: 10,000 tokens
- **Total: ~35,000 tokens per turn by the end**

At Sonnet pricing, that's ~$0.105 per turn × 30 turns = $3.15 for one session.
With Opus it would be $15.75 for the same session.

### Strategies to Control Context Growth

**Strategy 1: Targeted reads**
```
# Bad: reads entire 500-line file
Read the file src/services/auth.service.ts

# Good: reads only relevant section
Read lines 45-90 of src/services/auth.service.ts
```

**Strategy 2: Grep before read**
Use Grep to find the relevant section first, then Read only those lines.
One Grep result is ~10 tokens. One full file read might be 4000 tokens.

**Strategy 3: Session compaction**
Use `/compact` to summarize and discard the detailed history when switching topics.
Compacted history is ~10x smaller than the full conversation.

**Strategy 4: Fresh sessions**
Start a new session for unrelated tasks. Don't carry forward context from
debugging session A into architecture session B.

**Strategy 5: Scope tool results**
When asking Claude to analyze multiple files, give it the list of relevant files
rather than "look at everything in src/". Guide the search to avoid over-reading.

---

## 3. Cache-Efficient Prompt Patterns

### How Anthropic's Prompt Caching Works

Anthropic caches repeated prefix blocks of ≥1024 tokens for up to 5 minutes (extendable).
If the same block appears at the start of the context, the cached version is used at ~10%
of the normal input token cost.

**Cache hit condition:** The first N tokens of the context must be identical to a prior request.
Any change to the prefix — including a single character — busts the cache.

### Anatomy of a Cache-Friendly Context

```
[SYSTEM PROMPT — stable prefix — THIS GETS CACHED]
  Kit: cost-optimizer-kit v0.2.0
  Rules: model-selection.md (loaded inline)
  Project: MyApp, stack: .NET + Vue
  Always-active conventions...
  (1000+ tokens, never changes session-to-session)

[USER TURN — dynamic suffix — NOT cached]
  Current task: fix bug in AuthController.cs
  File contents: [pasted or read dynamically]
  Specific question
```

### Patterns That Bust the Cache

| Anti-Pattern | Why It's Expensive | Fix |
|---|---|---|
| Session timestamp in system prompt | Changes every request | Move to user turn |
| User name or session ID in CLAUDE.md | Unique per session | Remove entirely |
| Dynamic project status in CLAUDE.md | Changes as work progresses | Move to user turn |
| Editing CLAUDE.md each session | New content = new cache | Keep CLAUDE.md stable |
| Appending notes to CLAUDE.md | Shifts suffix into prefix | Use separate notes file |

### Patterns That Preserve Cache

| Pattern | Benefit |
|---|---|
| Static CLAUDE.md across sessions | Stable cache prefix |
| Rules before dynamic content | Maximizes cached prefix length |
| Stable @-ref order | Order changes bust cache |
| No dynamic injections into system prompt | Full cache hit every turn |

### Measuring Cache Efficiency

The API response includes token usage with cache breakdown:
```json
{
  "usage": {
    "input_tokens": 2000,
    "cache_creation_input_tokens": 1500,
    "cache_read_input_tokens": 500
  }
}
```
`cache_read_input_tokens` are billed at ~10% of normal. High `cache_read` = good.
High `cache_creation` on every request = cache busting (investigate why).

---

## 4. Kit Loading Cost Analysis

### What Loads When a Kit Is Active?

When a Claude Code kit is installed and active, the following files load into context:

1. **CLAUDE.md** — always loaded, every context
2. **@-refs in CLAUDE.md** — each `@path` loads that file
3. **Always-active rules** — every file listed in the "Always-Active Rules" section
4. **Skills** — loaded ONLY when the skill is invoked via `/command`
5. **Knowledge docs** — loaded only if @-ref'd from a skill or explicitly requested
6. **Agent definitions** — loaded only when that agent is spawned

### Token Cost by File Type

| File | Typical Size | Load Trigger |
|------|-------------|--------------|
| Kit CLAUDE.md | 500–2000 tokens | Always |
| Rule file | 200–800 tokens | Always (if @-ref'd in CLAUDE.md) |
| Skill SKILL.md | 200–1500 tokens | On /command invocation |
| Knowledge doc | 1000–8000 tokens | On demand only |
| Agent definition | 300–1000 tokens | On agent spawn |
| Config template | 100–500 tokens | On /kit-setup or /project-setup |
| MCP tool definitions | 50–200 tokens each | Always (per MCP server) |

### Kit Loading Cost Comparison

Estimated tokens loaded unconditionally per kit:

| Kit | Always-Loaded Tokens (approx) | Notes |
|-----|-------------------------------|-------|
| cost-optimizer-kit | ~400 | Minimal — just model-selection rule |
| git-kit | ~1500 | 4 rule files, moderate CLAUDE.md |
| dotnet-kit | ~3000 | 13 rule files, large CLAUDE.md |
| vue-kit | ~2500 | Similar to dotnet-kit |
| memory-kit | ~800 | Light rules, focused CLAUDE.md |
| jira-kit | ~700 | Focused scope |
| obsidian-kit | ~600 | Focused scope |

**Loading all kits unconditionally: ~12,000–15,000 tokens added to every context.**
At Sonnet pricing, that's $0.036 per turn just for static context.
Over 100 turns: $3.60 for context alone.

### Recommendation by Project Type

**Lean .NET backend:**
```
dotnet-kit + git-kit + cost-optimizer-kit = ~5000 tokens
```

**Full-stack .NET + Vue:**
```
dotnet-kit + vue-kit + git-kit + cost-optimizer-kit = ~7500 tokens
```

**Avoiding irrelevant kits saves 2000-8000 tokens per turn.**

---

## 5. Ruflo-Inspired Optimization Patterns

Ruflo (https://github.com/ruvnet/ruflo) achieves 30-50% token reduction through
five key techniques. Here's how to apply each in a Claude Code kit context:

### 5.1 ReasoningBank (-32% token reduction)

**Concept:** Store reasoning patterns from solved problems. Retrieve them instead
of re-reasoning from scratch when a similar problem appears.

**Mechanism:**
- When Claude solves a complex problem (architecture decision, debugging pattern,
  refactoring approach), the reasoning chain is 500-3000 tokens
- If a similar problem recurs, Claude re-reasons from scratch — same cost again
- A ReasoningBank stores the conclusion + key reasoning steps
- On recurrence, retrieval costs ~100 tokens; full re-reasoning costs ~2000 tokens
- **Net saving: ~1900 tokens per reuse = ~32% of a typical reasoning chain**

**Implementation with memory-kit:**
```
After solving: /memory-capture "Resolved N+1 query in OrderRepository by adding
Include(o => o.Items) — pattern: add Include() for navigation properties accessed
in foreach loops"

Before similar task: /memory-recall "N+1 query pattern"
→ retrieves the reasoning pattern, skipping re-derivation
```

**When to capture:**
- Architectural decisions with rationale
- Debugging patterns that took multiple steps to find
- Refactoring recipes applied to this codebase
- Configuration patterns that required research to get right

### 5.2 Smart Task Routing

**Concept:** Route each task to the cheapest model tier that can handle it.
A task that Haiku handles costs 19x less than the same task on Opus.

**Classification heuristic:**

```
IF task requires: pattern matching, template application, factual lookup
THEN use: Haiku ($ tier)

IF task requires: multi-step reasoning, code generation, debugging
THEN use: Sonnet ($$ tier)

IF task requires: strategic analysis, security reasoning, architectural judgment
THEN use: Opus ($$$ tier)
```

**Routing keywords:**
- "what is", "find", "list", "format", "generate boilerplate" → Haiku
- "implement", "fix", "refactor", "write tests", "review" → Sonnet
- "design", "audit", "analyze trade-offs", "should we", "architecture" → Opus

### 5.3 Prompt Caching (-10% cost)

**Concept:** Structure prompts so the stable prefix is as long as possible,
maximizing cache hit rate.

**Target cache hit rate:** >80% of input tokens should be cache reads.

**Calculation:**
- System prompt: 2000 tokens (stable, cached after first request)
- User turn: 500 tokens (varies, never cached)
- Cache hit rate: 2000 / (2000 + 500) = 80%
- Effective cost: 2000 × 0.1 + 500 × 1.0 = 700 token-equivalents
- Vs. no caching: 2500 × 1.0 = 2500 token-equivalents
- **Savings: 72%** for this session after the first request

**Critical:** Cache only activates after the first request. Long sessions benefit most.

### 5.4 Request Batching (-20% cost)

**Concept:** Combine multiple related requests into a single API call.
Each API call has overhead: network latency, request initialization, and a baseline
of tool-definition tokens loaded regardless of task size.

**Examples of batchable work:**

```
# Unbatched (3 turns):
Turn 1: "Read AuthController.cs"
Turn 2: "Read UserService.cs"
Turn 3: "How do these two files interact?"

# Batched (1 turn):
"Read AuthController.cs and UserService.cs, then explain how they interact"
```

**Parallel agent batching:**
```
# Sequential (3 full agent runs):
Agent 1: analyze auth layer
(wait for result)
Agent 2: analyze data layer
(wait for result)
Agent 3: analyze API layer

# Parallel (1/3 the wall time, same token cost but 33% lower overhead):
Spawn agents 1, 2, 3 simultaneously with Task tool
Collect results when all complete
```

**What batching saves:**
- Eliminates per-turn context overhead for repeated tool definitions
- Reduces conversation history accumulation (fewer turns = shorter history)
- Reduces network round-trips in latency-sensitive workflows

### 5.5 Early Termination

**Concept:** Stop agents and tool chains as soon as the answer is found.
Continuing to gather evidence after the answer is clear wastes tokens.

**Patterns:**

```markdown
# In skills, include:
"Stop reading files once the pattern is identified — don't read every file
to confirm what one file already showed."

"When searching for a bug, stop after finding the root cause.
Don't continue gathering related context."

"Use Grep to find candidates first. Read only the candidates that match,
not all files in the directory."
```

**Anti-patterns to avoid:**
- Reading every file in a directory when only one is relevant
- Continuing to search after an answer is found "to be thorough"
- Loading all context "just in case" rather than on demand
- Running multiple verification passes when one is sufficient

---

## 6. CLAUDE.md Optimization Patterns

### Anatomy of an Over-Loaded CLAUDE.md

This is what a poorly optimized CLAUDE.md looks like and why each section is expensive:

```markdown
# My Project (costs 5 tokens)

Session started: 2026-04-05T14:32:00Z  ← CACHE BUSTER (50 tokens, changes every session)

## Developer Info                        ← UNNECESSARY (50 tokens)
Developer: John Smith
Machine: MacBook Pro
Slack: @johnsmith

## Project Overview                      ← PROBABLY FINE (100 tokens)
...

## Full API Documentation                ← EXPENSIVE (2000 tokens — use @-ref instead)
[2000 tokens of API spec pasted inline]

## All Kits Loaded                       ← EXPENSIVE (loads 15,000 tokens of kit context)
@dotnet-kit/CLAUDE.md
@vue-kit/CLAUDE.md
@git-kit/CLAUDE.md
@github-kit/CLAUDE.md
@bitbucket-kit/CLAUDE.md  ← why are both loaded?
@jira-kit/CLAUDE.md
@confluence-kit/CLAUDE.md
@obsidian-kit/CLAUDE.md   ← irrelevant to code project

## Everything I've Learned This Week     ← VERY EXPENSIVE (accumulates unboundedly)
[growing block of session notes]
```

**Problems:**
1. Timestamp busts cache on every request
2. Developer info never used by Claude
3. API docs should be in knowledge/ and loaded on demand
4. 8 kits for a project that uses 3
5. Accumulating notes bloat context unboundedly

### Optimized CLAUDE.md Pattern

```markdown
# My Project

## Stack
- .NET 9 / C# — Clean Architecture · MediatR · EF Core · SQL Server
- Vue 3 + TypeScript — Pinia · Vue Router · Vitest

## Model Selection

| Task | Model |
|------|-------|
| Lookup, boilerplate | claude-haiku-4-5 |
| Code, debug, review | claude-sonnet-4-5 |
| Architecture, security | claude-opus-4-5 |

## Always-Active Rules

@~/.claude/rules/dotnet-kit/csharp.md
@~/.claude/rules/dotnet-kit/clean-architecture.md
@~/.claude/rules/cost-optimizer-kit/model-selection.md

## Token Budget

- Prefer targeted reads over full-file reads
- Batch independent tool calls
- Compress context before switching topics

## Skills Available

- /scaffold — generate vertical slice (command + handler + endpoint + test)
- /verify — 7-phase verification pass
- /optimize-cost — audit and optimize this CLAUDE.md
```

**This loads ~800 tokens vs ~17,000 tokens for the bloated version.**
**That's a 95% reduction in always-loaded context.**

---

## 7. Advanced Techniques

### 7.1 Tiered Knowledge Loading

Structure knowledge access in three tiers:

**Tier 1 — Always loaded (rules):** Small, always-relevant guidance. <500 tokens each.
**Tier 2 — Skill-loaded (skill knowledge):** Loaded when a skill is invoked.
**Tier 3 — On-demand (deep reference):** Explicitly requested when needed.

Example for dotnet-kit:
```
Tier 1 (always): csharp.md, clean-architecture.md, result-pattern.md
Tier 2 (skill): When /scaffold is run → load scaffold-patterns.md
Tier 3 (explicit): "Load the EF Core migration reference" → ef-core.md
```

### 7.2 Conversation Structure for Cache Efficiency

Design conversation flows to maximize cache hits:

```
Turn 1 (cold start, cache write):
  System: [full CLAUDE.md + rules] = 2000 tokens (cache write)
  User: "Implement the CreateOrder command"

Turn 2 (cache hit):
  System: [same 2000 tokens] = 200 token-equivalents (cache read, 90% discount)
  User: "Now add validation"

Turn 3 (cache hit):
  System: [same 2000 tokens] = 200 token-equivalents
  User: "Write tests for it"
```

Without caching: 3 × 2000 = 6000 input tokens for system content.
With caching: 2000 + 200 + 200 = 2400 effective token-equivalents.
**Savings: 60% on system prompt costs for this session.**

### 7.3 Output Token Discipline

Output tokens cost 5x input tokens. Reduce output verbosity:

```markdown
# Add to CLAUDE.md:
## Response Format

- Use bullet lists and tables, not paragraph prose, for factual answers
- Show code, not explanations of code when code is requested
- Skip preamble: don't explain what you're about to do, just do it
- Skip postamble: don't summarize what you just did
- If I ask a yes/no question, answer yes/no first, then explain if needed
```

This can reduce output token count by 20-40% for typical responses.

### 7.4 Tool Definition Optimization

Each MCP server's tools are included in the context on every request.
A server with 20 tools at 100 tokens each = 2000 tokens per request.

**Optimization:** Only register MCP servers relevant to the current task.
- During .NET development: keep devkit-mcp active
- During documentation work: devkit-mcp costs 2000 tokens for nothing

Check your MCP server registrations in `.claude/settings.json` and only
register servers you actively use in this project.

### 7.5 The 80/20 Rule for Context

80% of your context cost usually comes from 20% of your loaded content.
Find the expensive 20%:

1. Check total tokens per @-ref by noting file sizes
2. Identify which knowledge/ files are loaded unconditionally
3. Find any inline content blocks over 500 tokens
4. Audit MCP tool definitions for unused servers

Fix the top three offenders — you'll typically cut context by 40-60%.

---

## 8. Benchmarks and Real-World Data

### Typical Session Costs by Workflow Type

| Workflow | Turns | Avg Tokens/Turn | Sonnet Cost | Opus Cost |
|----------|-------|----------------|-------------|-----------|
| Quick Q&A | 3 | 1,500 | $0.014 | $0.068 |
| Feature implementation | 15 | 5,000 | $0.225 | $1.125 |
| Debugging session | 20 | 8,000 | $0.480 | $2.400 |
| Architecture review | 8 | 12,000 | $0.288 | $1.440 |
| Full project audit | 30 | 15,000 | $1.350 | $6.750 |

### Savings from Each Optimization

| Optimization | Typical Saving | Complexity |
|---|---|---|
| Right model for task | 10-80% cost per task | Low — just pick Haiku/Haiku |
| Context caching | 50-70% on long sessions | Low — keep CLAUDE.md stable |
| Prune irrelevant kits | 20-40% token reduction | Low — remove @-refs |
| Move knowledge to on-demand | 30-60% always-loaded tokens | Medium |
| Output verbosity reduction | 20-40% output tokens | Low — format instructions |
| Request batching | 15-25% overhead reduction | Medium |
| Conversation compaction | 30-70% on long sessions | Low — use /compact |
| ReasoningBank reuse | 20-32% on repeated patterns | Medium — requires memory-kit |

### Combined Effect (Ruflo-style full optimization)

Starting from a typical unoptimized project:
- Model routing: -35% (using Haiku/Sonnet instead of defaulting to Opus)
- Context caching: -15% (stable CLAUDE.md, long sessions)
- Pruned context: -20% (removed irrelevant kits and knowledge)
- Batching: -10% (fewer turns)
- **Combined: ~55% cost reduction**

These numbers align with Ruflo's claimed 30-50% reduction range, with full
optimization potentially exceeding it.

---

## 9. Monitoring and Iteration

### Tracking Your Costs

Enable usage tracking in Claude Code settings to monitor token consumption.
Review periodically:
- Which sessions are most expensive?
- Which workflows accumulate the most context?
- Are cache hit rates high?

### Signs You Need to Re-Optimize

- Sessions routinely exceed 50,000 tokens
- CLAUDE.md has grown to over 200 lines
- You're loading knowledge docs in CLAUDE.md directly
- You're using Opus for every task
- Cache hit rate is below 50%
- Multiple kits loaded that aren't relevant to the current project

### Re-Optimization Workflow

Run `/optimize-cost --audit` to get a scored efficiency report.
Address items in order of impact (highest GPA improvement per effort).
Re-run audit after changes to confirm improvement.
