# Cost Optimizer Kit

Reduce Claude API token costs 30-50% through model selection, context discipline,
prompt caching, and batching. Inspired by Ruflo's optimization techniques.

## Always-Active Rules

@~/.claude/rules/cost-optimizer-kit/model-selection.md

## Core Principles

1. **Right model for the task** — Haiku for lookup/boilerplate, Sonnet for code/debug,
   Opus for architecture/security. Never use Opus for tasks Haiku can handle.
2. **Context window discipline** — Load only what's needed. Skills over rules.
   @-refs over inline text. knowledge/ docs on demand, not always-loaded.
3. **Cache warming** — Keep CLAUDE.md stable across sessions. No dynamic content
   (timestamps, session IDs) in the system prompt.
4. **Batch parallel work** — Issue independent subtasks in one message.
   Parallel agents beat sequential round-trips.
5. **Early termination** — Stop when the answer is found. Don't exhaust context.

## Skills Available

### Optimization
- `/optimize-cost` — audit CLAUDE.md for inefficiencies, generate model selection table,
  apply token budget rules, produce an efficiency report with letter grades

  Arguments:
  - `--audit` — score the current setup only, no edits
  - `--generate` — create a new optimized CLAUDE.md from template
  - `--model-guide` — print the model selection matrix
  - `--all` (default) — audit + apply fixes + print model guide

### ReasoningBank
- `/reasoning-bank` — store and retrieve reasoning patterns to avoid re-derivation (~32% token saving)

  Arguments:
  - `store` — capture the current task's reasoning trace as a reusable pattern
  - `retrieve <query>` — find matching patterns before starting a complex task
  - `list` — show all stored patterns with summaries
  - `clear <key>` — delete a specific pattern

## Specialist Agents

- `model-router` — classify any task and recommend the cheapest viable model tier (Haiku/Sonnet/Opus) with justification; runs on Haiku itself
- `token-analyzer` — comprehensive token footprint audit: discovery scan, per-category estimates, efficiency scoring, prioritized savings report

## Knowledge (on-demand)

- `token-efficiency.md` — deep reference: pricing mechanics, caching patterns,
  kit loading costs, Ruflo patterns, benchmarks, and advanced techniques
  Load with: "Load the token efficiency reference"

## Quick Reference

### Model Selection

| Task | Model |
|------|-------|
| Lookup, grep, boilerplate | claude-haiku-4-5 |
| Code gen, debug, review | claude-sonnet-4-5 |
| Architecture, security audit | claude-opus-4-5 |

### Top 5 Cost Wins

1. Use Haiku for simple tasks (up to 19x cheaper than Opus)
2. Prune irrelevant kits from CLAUDE.md (saves 2000-8000 tokens/turn)
3. Keep CLAUDE.md stable — don't edit per-session (preserves cache prefix)
4. Move knowledge/ docs to on-demand loading (saves 1000-8000 tokens/turn)
5. Compress long conversations before topic switches (/compact)
