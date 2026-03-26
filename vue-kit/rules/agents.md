# Rule: Claude Agent Usage

## DO
- Use **specialized agents** for tasks that match their descriptions in `~/.claude/agents/`
- Spawn agents in **parallel** when tasks are independent — maximize throughput
- Give agents **complete, self-contained prompts** — they don't share context with the parent
- Use agents to protect the **main context window** from verbose tool output
- Specify the **exact deliverable** you need back (not just "explore") so the agent can stay focused
- Use `subagent_type: Explore` for codebase exploration with `model: haiku` — cheap and fast
- Use `subagent_type: Plan` for architecture decisions before implementation begins
- Use named specialist agents (e.g., `code-reviewer`, `security-auditor`) for domain tasks

## DON'T
- Don't duplicate work that an agent is already doing — delegate once and use the result
- Don't use agents for targeted, known-location searches — `Grep`/`Glob` is faster
- Don't launch Opus-powered agents for file lookups or summarization — use Haiku
- Don't give agents ambiguous tasks that require back-and-forth — work must be self-contained
- Don't spawn agents just to avoid reading 1-2 files — agents have overhead

## Available Agents
- `vue-expert` — Vue 3 components, Pinia stores, SignalR, TypeScript, Vite config
- `code-reviewer` — full PR review: correctness, Vue patterns, security, performance
- `security-auditor` — XSS, CSRF, exposed secrets, vulnerable npm dependencies
- `devops-engineer` — CI/CD for Vue/TypeScript, Docker multi-stage builds, Nginx config
- `tech-lead` — Jira, Confluence, SDLC compliance, story refinement

## When to Use Agents vs. Main Context

| Task | Use Agent? |
|------|-----------|
| Explore unfamiliar codebase | Yes — Explore (Haiku) |
| Read 1-2 known files | No — Read directly |
| Broad search with verbose output | Yes — protect context |
| Making targeted edits | No — stay in main context |
| Architecture analysis | Yes — Plan agent (Opus) |
| Bug fix in known file | No — work directly |

## Examples

```
// GOOD — Haiku subagent for exploration, self-contained prompt with clear deliverable
Agent(model: haiku, type: Explore):
  "Read src/features/orders/ and summarize: store actions, composables used,
   any deviations from the Pinia Composition API pattern. Return a 200-token summary."

// BAD — doing broad exploration in main context, burning tokens
Read: src/features/orders/orderStore.ts   // then read 12 more files...
Read: src/features/orders/useOrders.ts
// context now filled with 8,000 tokens of file content

// GOOD — targeted edit stays in main context (no agent overhead)
Grep: "useOrders" → finds file + line
Edit: useOrders.ts (targeted change)

// BAD — spawning Opus agent for a deterministic, pattern-following task
Agent(model: opus): "Scaffold a standard Pinia store for Products"
// Sonnet knows the pattern — Opus is 20× more expensive with no benefit
```
