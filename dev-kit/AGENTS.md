# Agents — Dev Kit

This file defines the specialist agents available in the dev-kit, their domains, and routing rules.

## Agent Roster

| Agent | Model | Domain | When to Use |
|-------|-------|--------|-------------|
| `dotnet-architect` | Opus | Architecture decisions, solution design, DDD | "How should I structure this?", "What architecture fits?" |
| `api-designer` | Sonnet | Minimal API endpoints, OpenAPI, request/response shapes | "Design this endpoint", "Add OpenAPI docs" |
| `ef-core-specialist` | Sonnet | Entity config, migrations, query optimization | "Fix this query", "Add migration", "Why is this slow?" |
| `test-engineer` | Sonnet | xUnit, WebApplicationFactory, Testcontainers, test strategy | "Write tests for this", "Set up test infrastructure" |
| `vue-expert` | Sonnet | Vue 3 components, Pinia stores, SignalR, TypeScript | Frontend questions, component design, state management |
| `code-reviewer` | Opus | Full PR review: correctness, architecture, security, perf | Before merging any feature branch |
| `build-error-resolver` | Sonnet | Compilation errors, MSBuild issues, missing references | Build is broken |
| `devops-engineer` | Sonnet | CI/CD, Docker, GitHub Actions, deployment | Setting up pipelines, containerizing |
| `performance-analyst` | Opus | N+1 queries, missing indexes, caching, memory hotspots | Profiling results show bottlenecks |
| `refactor-cleaner` | Sonnet | Technical debt, code cleanup, modern C# idioms | "Clean up this file", "Reduce complexity" |
| `security-auditor` | Opus | OWASP audit, auth/authz review, secrets, CVEs | Pre-release security review |

## Routing Table

| User Intent | Agent |
|-------------|-------|
| "Design the architecture for X" | `dotnet-architect` |
| "Should I use VSA or Clean Architecture?" | `dotnet-architect` |
| "Add endpoint for X" | `api-designer` |
| "Write tests for X" | `test-engineer` |
| "Set up Testcontainers" | `test-engineer` |
| "EF Core query is slow" | `ef-core-specialist` + `performance-analyst` |
| "Add migration" | `ef-core-specialist` |
| "Vue component not rendering" | `vue-expert` |
| "SignalR connection issues" | `vue-expert` |
| "Review this PR" | `code-reviewer` |
| "Build is broken" | `build-error-resolver` |
| "Set up GitHub Actions" | `devops-engineer` |
| "Containerize this app" | `devops-engineer` |
| "App is slow under load" | `performance-analyst` |
| "Clean up this code" | `refactor-cleaner` |
| "Security audit before release" | `security-auditor` |
| "Check for vulnerabilities" | `security-auditor` |

## Meta Skill Routing

These skills are auto-active and apply across all agents:

| Skill | When It Activates |
|-------|------------------|
| `context-discipline` | Always — controls token budget and subagent delegation |
| `model-selection` | Always — routes tasks to Haiku/Sonnet/Opus appropriately |
| `verification-loop` | After any implementation task |
| `instinct-system` | Automatically learns project-specific patterns |
| `self-correction-loop` | On any user correction |
| `learning-log` | During sessions — captures discoveries |

## Slash Command → Agent Mapping

| Command | Primary Skill | Agent |
|---------|--------------|-------|
| `/scaffold` | scaffold | `dotnet-architect` |
| `/ddd` | ddd | `dotnet-architect` |
| `/vertical-slice` | vertical-slice | `dotnet-architect` |
| `/verify` | verification-loop | — |
| `/health-check` | health-check | `code-reviewer` |
| `/dotnet-health-check` | dotnet-health-check | `code-reviewer` |
| `/80-20-review` | 80-20-review | `code-reviewer` |
| `/security-scan` | security-scan | `security-auditor` |
| `/testing` | testing | `test-engineer` |
| `/migration-workflow` | migration-workflow | `ef-core-specialist` |
| `/docker` | docker | `devops-engineer` |
| `/ci-cd` | ci-cd | `devops-engineer` |
| `/architecture-advisor` | architecture-advisor | `dotnet-architect` |
| `/dotnet-init` | dotnet-init | `dotnet-architect` |
| `/wrap-up-ritual` | wrap-up-ritual | — |
| `/seq-dig` | seq-dig | — |

## Conflict Resolution

When multiple agents could handle a request:
1. **Domain experts > generalists** — `ef-core-specialist` over `dotnet-architect` for EF queries
2. **Opus for judgment calls** — Architecture decisions and security reviews always use Opus agents
3. **Sonnet for execution** — If the pattern is known, use Sonnet-backed agents
4. **Ask first for ambiguous scope** — If a task could be a refactor OR an architecture change, ask which the user wants

## Token Budget Guidance

| Task | Approach |
|------|----------|
| Quick question (known answer) | Main context, no agent |
| Feature implementation | Main context + relevant skill loaded |
| Codebase exploration | Spawn Explore subagent (Haiku) |
| PR review | `code-reviewer` agent (Opus, separate context) |
| Security audit | `security-auditor` agent (Opus, separate context) |
| Build fix | `build-error-resolver` agent (Sonnet, focused context) |
