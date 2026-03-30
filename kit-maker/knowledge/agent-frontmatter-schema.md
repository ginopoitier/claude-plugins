# Agent Frontmatter Schema

Complete reference for all fields in agent `.md` frontmatter. Agents live in `{kit}/agents/` and install to `~/.claude/agents/`.

## Minimal Agent

```yaml
---
name: my-agent
description: What this agent does and when to invoke it.
---
```

## Full Schema

```yaml
---
name: agent-name                # Required. Kebab-case, unique across all installed agents.
description: >                  # Required. Role definition + spawning trigger.
  One-line role.
  Spawned by: /command or user.
model: claude-sonnet-4-6        # Optional. sonnet|opus|haiku|full-model-id|inherit (default: inherit)
tools: Read, Write, Edit, Bash, Glob, Grep  # Optional allowlist — inherits all if omitted
disallowedTools: WebSearch      # Optional denylist — alternative to allowlist
color: green                    # Optional. Visual identifier in UI.
permissionMode: acceptEdits     # Optional. default|acceptEdits|dontAsk|bypassPermissions|plan
maxTurns: 20                    # Optional. Max agentic turns before stopping.
skills:                         # Optional. Preload skill content into agent context at startup.
  - skill-name-1
  - skill-name-2
mcpServers:                     # Optional. MCP servers scoped to this agent.
  - server-name
memory: user                    # Optional. Persistent memory scope: user|project|local
background: false               # Optional. true = run as background task (default: false)
effort: high                    # Optional. low|medium|high|max — overrides session effort
isolation: worktree             # Optional. Run in isolated git worktree copy
initialPrompt: "Read CLAUDE.md" # Optional. Auto-submitted as first user turn when agent starts
# hooks:                        # Optional. Scoped to this agent only. Ship commented-out.
#   PostToolUse:
#     - matcher: "Write|Edit"
#       hooks:
#         - type: command
#           command: "npm run lint --fix $FILE 2>/dev/null || true"
---
```

## Fields Reference

### `name`
Kebab-case. Must be unique across all installed agents (flat namespace: `~/.claude/agents/`). Two kits cannot ship agents with the same name.

### `description`
Two parts in one field:
1. What the agent does (role definition)
2. When it is spawned (trigger context)

```yaml
# Good — covers role AND trigger
description: >
  Reviews staged changes for security vulnerabilities.
  Spawned by /security-scan and code-review-workflow agent.

# Bad — role only, no trigger
description: Reviews code for security issues.
```

### `model`
| Value | Use When |
|-------|----------|
| `haiku` | Fast lookups, summarization, index building, exploration |
| `sonnet` | Most agents — implementation, writing, multi-step reasoning |
| `opus` | Architecture review, security audit, novel judgment calls |
| `inherit` | (default) Uses whatever model is active in the main session |
| full ID | Pin to a specific release: `claude-sonnet-4-6` |

### `tools` vs `disallowedTools`
Use one or the other — not both.

```yaml
# Allowlist — declare exactly what the agent needs
tools: Read, Glob, Grep, Bash

# Denylist — inherit everything except these
disallowedTools: Bash, WebSearch

# Restrict which subagents this agent can spawn
tools: Agent(worker, researcher), Read, Bash
```

Unused tools waste context. An agent that only reads files should not have `Write`.

### `color`
Visual role indicator in the agent UI:

| Color | Convention |
|-------|------------|
| `green` | Planner / creator |
| `yellow` | Executor / builder |
| `blue` | Researcher / analyst |
| `purple` | Reviewer / auditor |
| `red` | Debugger / fixer |
| `orange` | Coordinator / orchestrator |

Always set `color` on agents in multi-agent systems so users can track which agent is active.

### `permissionMode`
| Mode | Behavior |
|------|----------|
| `default` | Standard permission prompts |
| `acceptEdits` | Auto-accept file writes/edits |
| `dontAsk` | Auto-deny unapproved tool prompts |
| `bypassPermissions` | Skip all permission prompts |
| `plan` | Read-only plan mode |

Use `acceptEdits` for executor agents that write many files. Never use `bypassPermissions` in kit agents shipped to users.

### `skills`
Preloads skill content into the agent's system prompt at startup. Use to inject domain rules without requiring the agent to discover them:

```yaml
skills:
  - error-handling-patterns
  - api-conventions
```

### `memory`
Gives the agent persistent memory across invocations:
| Scope | Path | Notes |
|-------|------|-------|
| `user` | `~/.claude/agent-memory/{name}/` | Cross-project, not version-controlled |
| `project` | `.claude/agent-memory/{name}/` | Project-specific, committed to repo |
| `local` | `.claude/agent-memory-local/{name}/` | Project-specific, gitignored |

### `effort`
Overrides the session-level effort for this agent's work. Use `high` or `max` for complex analysis agents, `low` for fast lookup agents.

### `isolation`
`isolation: worktree` — agent runs in an isolated git worktree copy of the repo. Worktree is cleaned up automatically if no changes are made. Useful for parallel work or risky refactors.

### `initialPrompt`
Auto-submitted as the first user turn when the agent starts. Use to give the agent immediate context without the parent needing to repeat it:

```yaml
initialPrompt: "Begin by reading CLAUDE.md and the files in the diff provided."
```

### `hooks` (inline, commented out)
Agent-level hooks fire only while the agent is active. Ship them commented out — users opt in:

```yaml
# hooks:
#   PostToolUse:
#     - matcher: "Write|Edit"
#       hooks:
#         - type: command
#           command: "npx eslint --fix $FILE 2>/dev/null || true"
#   Stop:
#     - hooks:
#         - type: prompt
#           prompt: "Verify all tests still pass. Respond {\"ok\": false, \"reason\": \"...\"} if not."
```

## Agent Scopes (Priority Order)

| Location | Scope | Priority |
|----------|-------|----------|
| `--agents` CLI flag | Current session only | 1 (highest) |
| `.claude/agents/` | Current project | 2 |
| `~/.claude/agents/` | All projects | 3 |
| `{plugin}/agents/` | Where plugin enabled | 4 (lowest) |

## Anti-patterns

```yaml
# BAD — all tools, unfocused
tools: Read, Write, Edit, Bash, Glob, Grep, WebFetch, WebSearch, Agent

# GOOD — only what the agent needs
tools: Read, Glob, Grep

# BAD — no spawning context in description
description: Reviews code for quality.

# GOOD — explicit trigger
description: >
  Reviews staged changes for quality and security.
  Spawned by /code-review-workflow or user @-mention.

# BAD — Opus for simple file reading
model: opus
tools: Read, Glob
# agent just reads and summarizes files

# GOOD — Haiku for lookups
model: haiku
tools: Read, Glob
```
