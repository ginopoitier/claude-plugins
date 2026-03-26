# Backend Kit

> **Config:** @~/.claude/kit.config.md — run `/kit-setup` if missing.

## Stack
- **Language:** C# / .NET — Clean Architecture · MediatR CQRS · Minimal APIs · EF Core · Serilog + Seq
- **Databases:** SQL Server · Neo4j
- **MCP:** `devkit-mcp` — Roslyn-powered code analysis, SQL Server diagnostics, Neo4j graph queries

## Always-Active Rules

@~/.claude/rules/csharp.md
@~/.claude/rules/clean-architecture.md
@~/.claude/rules/cqrs.md
@~/.claude/rules/result-pattern.md
@~/.claude/rules/ef-core.md
@~/.claude/rules/logging.md
@~/.claude/rules/api-design.md
@~/.claude/rules/testing.md
@~/.claude/rules/packages.md
@~/.claude/rules/security.md
@~/.claude/rules/performance.md
@~/.claude/rules/agents.md
@~/.claude/rules/hooks.md
@~/.claude/rules/sdlc.md
@~/.claude/rules/git-workflow.md

## Meta — Always Apply

@~/.claude/skills/context-discipline/SKILL.md
@~/.claude/skills/model-selection/SKILL.md
@~/.claude/skills/verification-loop/SKILL.md

## Two-Level Config System

Config is split into two levels — **never hardcode values**:

### User / Device Level — `~/.claude/kit.config.md`
Device-specific toolchain. Different on each machine:
- **Home:** `CI_PROVIDER=github-actions`, `DOCS_PRIMARY=obsidian`
- **Work:** `CI_PROVIDER=teamcity`, `CD_PROVIDER=octopus`, `PM_PROVIDER=jira`

Run `/kit-setup` to configure. Key values: `CI_PROVIDER` · `CD_PROVIDER` · `TEAMCITY_BASE_URL` · `OCTOPUS_URL` · `SEQ_URL` · `DEFAULT_NAMESPACE`

### Project Level — `.claude/project.config.md` (in each repo)
Project-specific identifiers and stack choices. Committed to version control.

Run `/project-setup` to generate. Project config **overrides** user config where values overlap.

When a skill needs config and `~/.claude/kit.config.md` is missing → tell user to run `/kit-setup`.
When a skill needs project config and `.claude/project.config.md` is missing → tell user to run `/project-setup`.

## Skills Available

### Code Generation
- `/scaffold` — full vertical slice (command/query + handler + endpoint + test)
- `/ddd` — DDD building blocks (aggregate, value objects, events, errors)
- `/vertical-slice` — same as scaffold, feature-first naming
- `/signalr-hub` — strongly-typed hub + domain event handler + Vue composable

### Analysis & Quality
- `/verify` — 7-phase verification (build → diagnostics → antipatterns → tests → security → format → diff)
- `/dotnet-health-check` — full project audit
- `/health-check` — 8-dimension health report with letter grades (A-F) and GPA
- `/80-20-review` — blast-radius-scored code review
- `/de-sloppify` — find/fix quality issues, dead code, TODOs
- `/convention-learner` — detect and enforce project-specific coding conventions
- `/security-scan` — comprehensive security audit
- `/dependency-audit` — vulnerable/outdated NuGet packages
- `/domain-event-map` — visualize event flows
- `/testing` — xUnit v3, WebApplicationFactory, Testcontainers, snapshot testing, AAA pattern

### Infrastructure & Patterns
- `/caching` — HybridCache, output caching, distributed cache patterns
- `/configuration` — Options pattern, secrets, environment-based config
- `/dependency-injection` — keyed services, decorator pattern, factory pattern
- `/authentication` — JWT bearer, OIDC, policy-based authorization
- `/messaging` — Wolverine/MassTransit, outbox pattern, sagas
- `/resilience` — Polly v8 retry, circuit breaker, timeout, hedging
- `/httpclient-factory` — named/keyed HTTP clients, DelegatingHandlers
- `/opentelemetry` — traces, metrics, logs with OTLP export
- `/aspire` — .NET Aspire orchestration, service discovery, dashboard

### API Design
- `/openapi` — built-in OpenAPI, document transformers, TypedResults (no Swashbuckle)
- `/scalar` — modern API docs UI (Swagger UI replacement)
- `/error-handling` — Result pattern, ProblemDetails RFC 9457, global exception handler

### Language
- `/modern-csharp` — C# 14 features: field keyword, extension members, collection expressions

### Database
- `/sqlserver` — SQL Server diagnostics (query, schema, indexes, blocking, migrations)
- `/migration-workflow` — EF Core migration management

### Tech Lead Workflow
- `/sdlc-check` — validate work against company SDLC
- `/pr-prep` — prepare a PR description from the diff + Jira ticket ACs

### DevOps & Environment
- `/docker` — Docker Compose management and scaffolding
- `/ci-cd` — generates pipeline config files for GitHub Actions, TeamCity, Octopus, Azure DevOps
- `/container-publish` — Dockerfile-less SDK container publishing (chiseled images)

### Diagnostics
- `/seq-dig` — Seq log investigation

### Project Setup
- `/dotnet-init` — scaffold new Clean Architecture solution
- `/project-setup` — interactive project init, health check, CLAUDE.md generation
- `/project-structure` — .slnx format, Directory.Build.props, central package management
- `/architecture-advisor` — structured questionnaire → recommends VSA/CA/DDD/Modular Monolith
- `/kit-setup` — configure kit settings

### Session & Workflow
- `/session-management` — start/end/resume development sessions
- `/workflow-mastery` — plan and track multi-session epics
- `/wrap-up-ritual` — structured session ending with handoff note

### Meta (auto-active, not user-invoked)
- `instinct-system` — project-specific pattern learning with confidence scores
- `self-correction-loop` — captures user corrections → permanent MEMORY.md rules
- `autonomous-loops` — bounded build-fix/test-fix/refactor/scaffold iteration loops
- `learning-log` — organic discoveries, gotchas, architecture decisions per session
- `convention-learner` — detects and enforces project-specific coding conventions
