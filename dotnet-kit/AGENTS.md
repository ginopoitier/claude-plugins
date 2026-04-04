# Agents — Backend Kit

Specialist agents available in dotnet-kit and their routing rules.

## Agent Roster

| Agent | Model | Domain | When to Use |
|-------|-------|--------|-------------|
| `dotnet-architect` | Opus | Architecture decisions, solution design, DDD | "How should I structure this?", "What architecture fits?" |
| `api-designer` | Sonnet | Minimal API endpoints, OpenAPI, request/response shapes | "Design this endpoint", "Add OpenAPI docs" |
| `ef-core-specialist` | Sonnet | Entity config, migrations, query optimization | "Fix this query", "Add migration", "Why is this slow?" |
| `test-engineer` | Sonnet | xUnit, WebApplicationFactory, Testcontainers, test strategy | "Write tests for this", "Set up test infrastructure" |
| `build-error-resolver` | Sonnet | Compilation errors, MSBuild issues, missing references | Build is broken |
| `devops-engineer` | Sonnet | CI/CD, Docker, GitHub Actions, deployment | Setting up pipelines, containerizing |
| `performance-analyst` | Opus | N+1 queries, missing indexes, caching, memory hotspots | Profiling results show bottlenecks |
| `refactor-cleaner` | Sonnet | Technical debt, code cleanup, modern C# idioms | "Clean up this file", "Reduce complexity" |
| `security-auditor` | Opus | OWASP audit, auth/authz review, secrets, CVEs | Pre-release security review |

## Routing Table

| User Intent | Agent |
|-------------|-------|
| "Design the architecture for X" | `dotnet-architect` |
| "Add endpoint for X" | `api-designer` |
| "Write tests for X" | `test-engineer` |
| "EF Core query is slow" | `ef-core-specialist` + `performance-analyst` |
| "Add migration" | `ef-core-specialist` |
| "Build is broken" | `build-error-resolver` |
| "Set up GitHub Actions" | `devops-engineer` |
| "App is slow under load" | `performance-analyst` |
| "Clean up this code" | `refactor-cleaner` |
| "Security audit before release" | `security-auditor` |

## Conflict Resolution

1. **Domain experts > generalists** — `ef-core-specialist` over `dotnet-architect` for EF queries
2. **Opus for judgment calls** — Architecture decisions and security reviews always use Opus agents
3. **Sonnet for execution** — If the pattern is known, use Sonnet-backed agents
4. **Ask first for ambiguous scope** — If a task could be a refactor OR an architecture change, ask which the user wants
