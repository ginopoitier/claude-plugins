# dotnet-kit

Production-ready .NET Clean Architecture developer toolkit for Claude Code — CQRS, EF Core, Minimal APIs, Serilog+Seq, CI/CD pipeline generation, and full tech lead workflow.

## What's Included

### Skills (44 total)

| Category | Skills |
|----------|--------|
| **Code Generation** | `/scaffold`, `/ddd`, `/vertical-slice`, `/signalr-hub` |
| **Analysis & Quality** | `/verify`, `/dotnet-health-check`, `/health-check`, `/de-sloppify`, `/security-scan`, `/dependency-audit`, `/domain-event-map`, `/testing` |
| **Infrastructure** | `/caching`, `/configuration`, `/dependency-injection`, `/authentication`, `/messaging`, `/resilience`, `/httpclient-factory`, `/logging`, `/serilog`, `/opentelemetry`, `/aspire` |
| **API Design** | `/api-versioning`, `/minimal-api`, `/openapi`, `/scalar`, `/error-handling` |
| **Language** | `/modern-csharp` |
| **Database** | `/ef-core`, `/sqlserver`, `/migration-workflow` |
| **DevOps** | `/docker`, `/ci-cd`, `/container-publish` |
| **Project Setup** | `/clean-architecture`, `/dotnet-init`, `/project-setup`, `/project-structure`, `/architecture-advisor`, `/kit-setup` |
| **Tech Lead** | `/pr-prep`, `/seq-dig`, `/workflow-mastery` |

### Agents (15)

| Agent | Role |
|-------|------|
| `api-designer` | ASP.NET Core Minimal API endpoints — routing, OpenAPI, validation |
| `build-error-resolver` | .NET build errors — diagnose, fix, verify |
| `devops-engineer` | CI/CD, Docker, GitHub Actions, deployment scripts |
| `dotnet-architect` | Architecture decisions, CQRS structure, domain modeling |
| `ef-core-specialist` | EF Core queries, migrations, owned entities, interceptors |
| `performance-analyst` | N+1 queries, slow endpoints, caching, memory hotspots |
| `refactor-cleaner` | Code cleanup, modern C# idioms, technical debt |
| `security-auditor` | Security vulnerabilities, auth/authz, secrets, remediation |
| `test-engineer` | xUnit, Testcontainers, WebApplicationFactory, test data builders |
| + 6 workflow agents | tech-lead, pr-reviewer, sprint-planner, doc-writer, and others |

### Rules (always-active, 14)

`csharp` · `clean-architecture` · `cqrs` · `result-pattern` · `ef-core` · `logging` · `api-design` · `testing` · `packages` · `security` · `performance` · `agents` · `hooks` · `sdlc`

### MCP Server

`devkit-mcp` — Roslyn-powered code analysis, SQL Server schema inspection, Neo4j graph queries. Powers deep code inspection without prompt stuffing.

## Install

```
/plugin marketplace add ginopoitier/claude-plugins
/plugin install dotnet-kit@ginopoitier-plugins
```

## First-Time Setup

```
/kit-setup
```

Configures `~/.claude/kit.config.md` with CI/CD provider, documentation targets, and project defaults.

Install the MCP server:

```bash
dotnet tool install -g DevKit.Mcp
```

## Two-Level Config System

| Level | File | Contains |
|-------|------|----------|
| User / Device | `~/.claude/kit.config.md` | CI provider, CD provider, SEQ_URL, default namespace |
| Project | `.claude/project.config.md` | Project-specific identifiers and stack choices |

Run `/kit-setup` once per machine. Run `/project-setup` once per repo.

## Requirements

- Claude Code 1.0.0+
- .NET 10 SDK
- `devkit-mcp` global tool (optional but recommended)

## License

MIT
