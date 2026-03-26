# Agents — Vue Kit

Specialist agents available in vue-kit and their routing rules.

## Agent Roster

| Agent | Model | Domain | When to Use |
|-------|-------|--------|-------------|
| `vue-expert` | Sonnet | Vue 3 components, Pinia stores, SignalR, TypeScript | Frontend questions, component design, state management |
| `code-reviewer` | Opus | Full PR review: correctness, architecture, security, perf | Before merging any feature branch |
| `devops-engineer` | Sonnet | Vite build config, CI/CD for frontend, deployment | Setting up build pipelines, containerizing frontend |
| `security-auditor` | Opus | XSS, CSRF, exposed secrets, dependency CVEs | Pre-release security review |
| `tech-lead` | Sonnet | Jira, Confluence, SDLC compliance | Sprint workflow, story refinement |

## Routing Table

| User Intent | Agent |
|-------------|-------|
| "Vue component not rendering" | `vue-expert` |
| "SignalR connection issues" | `vue-expert` |
| "Design Pinia store for X" | `vue-expert` |
| "TypeScript types for this API response" | `vue-expert` |
| "Review this PR" | `code-reviewer` |
| "Set up Vite build for production" | `devops-engineer` |
| "Security audit before release" | `security-auditor` |
| "Check for CVEs in npm packages" | `security-auditor` |

## Meta Skill Routing

| Skill | When It Activates |
|-------|------------------|
| `context-discipline` | Always — controls token budget and subagent delegation |
| `model-selection` | Always — routes tasks to Haiku/Sonnet/Opus appropriately |
| `instinct-system` | Automatically learns project-specific patterns |
| `self-correction-loop` | On any user correction |
| `learning-log` | During sessions — captures discoveries |
