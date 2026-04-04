# Cost Optimization — Context Window Strategy for Kits

## Why This Matters

Claude Code sessions have a 200k token context window. Every file loaded costs tokens. A poorly designed kit loads everything every session, burning 20k tokens on context before any work starts. A well-designed kit loads ~2k tokens of rules and pulls in skills only when needed.

## The Loading Model

```
Always loaded (every session):
  CLAUDE.md                    ~200 tokens
  Rules (3–8 files × 500t)    ~1,500–4,000 tokens
  ─────────────────────────────────────────
  Total baseline:              ~3,000–6,000 tokens

Lazy loaded (on demand):
  Each skill (avg 1,000t)     loaded only when triggered
  Each knowledge doc (2,000t) loaded only when skill needs it
  ─────────────────────────────────────────
  Per-session overhead:        0 until the domain is mentioned
```

## Design Rules for Low Token Cost

### Rule 1: Keep rules short and specific

Rules load every session. A 2,000-token rule file costs 2,000 tokens in every conversation, even when the rule's domain isn't relevant.

```markdown
<!-- EXPENSIVE — long rule file -->
# Rule: EF Core
[500 lines of explanation, context, rationale, examples...]

<!-- CHEAP — concise rule with link -->
# Rule: EF Core
## DO
- Use AsNoTracking() on all read-only queries
- Use IEntityTypeConfiguration<T> per entity
- Project to DTOs with .Select() — never load full entity for reads

## DON'T
- Don't use .Include() in query handlers — project directly
- Don't call SaveChanges() — always SaveChangesAsync(ct)

## Deep Reference
For full patterns: @~/.claude/knowledge/kit-name/ef-core-patterns.md
```

The rule is ~200 tokens. The knowledge doc is 3,000 tokens — only loaded when needed.

### Rule 2: Trigger keywords must be specific

Vague trigger keywords cause skills to load when they aren't needed.

```yaml
# EXPENSIVE — loads for any HTTP discussion
description: >
  HTTP patterns. Load for web requests, APIs, HTTP.

# EFFICIENT — loads only for HttpClient-specific work
description: >
  IHttpClientFactory patterns.
  Load when: "IHttpClientFactory", "AddHttpClient", "socket exhaustion",
  "typed client", "named client", "DelegatingHandler".
```

Broad keywords → skill loads in many sessions it isn't relevant to → tokens wasted.

### Rule 3: Skills reference knowledge, don't duplicate it

```markdown
<!-- EXPENSIVE — skill contains full knowledge content -->
## Patterns
### DbContext Configuration
[500 lines of EF Core setup code with all options...]

<!-- EFFICIENT — skill has patterns, links for depth -->
## Patterns
### DbContext Configuration
```csharp
// Core setup (see ef-core-patterns.md for full options)
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
        => builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```
For full configuration reference: @~/.claude/knowledge/kit-name/ef-core-patterns.md
```

### Rule 4: Agents protect the main context from verbose output

When a task will produce lots of output (exploring files, running searches, analyzing code), delegate to a subagent. The subagent's output lives in a separate context — only the summary comes back.

```
# EXPENSIVE — doing research in main context
Main context: Read 20 files × 500 tokens = 10,000 tokens consumed

# EFFICIENT — delegate to Explore agent
Main context: "analyze project structure" → Explore agent (separate context) → summary back = ~500 tokens
```

In kit agent definitions, specify model: `haiku` for exploration tasks. Haiku is faster and cheaper for file reads and searches.

### Rule 5: Model selection is cost control

```
Haiku 4.5:  Fast, cheap — file reads, Glob/Grep, summarization, exploration
Sonnet 4.6: Mid-tier — implementation, code generation, pattern following
Opus 4.6:   Expensive — architecture, complex debugging, ambiguous requirements
```

A kit that uses Opus for everything costs 5–10x more than one that routes appropriately. Use dedicated routing and model-selection support from separate integration kits or hook-driven agents.

## Token Budget by Task Type

| Task | Budget | Notes |
|------|--------|-------|
| Full session (exploration + impl + verify) | 50k | Leave 150k for conversation |
| Codebase exploration | 5k | Use Explore agent (Haiku) |
| Implementation of known pattern | 15k | Sonnet, skill loaded |
| Architecture decision | 10k | Opus, plan agent |
| Verification (build + test) | 3k | Output only, no reasoning |
| Knowledge lookup | 2k | Load skill, read knowledge doc |

## What NOT to Put in CLAUDE.md

```markdown
<!-- BAD — loading everything upfront -->
@~/.claude/knowledge/kit-name/ef-core-patterns.md       # 3,000 tokens every session
@~/.claude/knowledge/kit-name/resilience-patterns.md    # 2,500 tokens every session
@~/.claude/knowledge/kit-name/caching-patterns.md       # 2,000 tokens every session

<!-- GOOD — load via skills on demand -->
# In ef-core skill's SKILL.md:
# "For full patterns: @~/.claude/knowledge/kit-name/ef-core-patterns.md"
# Only loads when EF Core work is happening
```

## Measuring Kit Efficiency

After a session, check:
- How many skills loaded? (check via session summary)
- Were they all relevant to the task?
- Did any knowledge docs load unnecessarily?

If 3+ skills loaded that weren't used, tighten their trigger keywords.
