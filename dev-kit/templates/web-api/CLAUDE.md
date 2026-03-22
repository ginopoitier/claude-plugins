# Web API Project

Clean Architecture · MediatR CQRS · Minimal APIs · EF Core (SQL Server) · Result Pattern · Serilog + Seq

## Project Layout

```
src/
  {Ns}.Domain/           # Entities, value objects, domain events, errors, Result types
  {Ns}.Application/      # Handlers, validators, DTOs, pipeline behaviors, interfaces
  {Ns}.Infrastructure/   # DbContext, EF configs, migrations, external services
  {Ns}.Api/              # Endpoints, middleware, DI wiring, Program.cs
tests/
  {Ns}.Domain.Tests/     # Pure domain logic — no infrastructure
  {Ns}.Application.Tests/ # WebApplicationFactory + Testcontainers integration tests
```

## Key Conventions

- All business ops via MediatR — `ISender.Send()` from endpoints
- Handlers return `Result<T>` or `Result` — no business exceptions
- Errors mapped to RFC 7807 ProblemDetails via `ToProblemDetails()` extension
- `IEndpointGroup` auto-discovered — no inline routes in Program.cs
- `AsNoTracking()` on all query handlers
- `CancellationToken ct` on every async call
- Serilog structured logging → Seq

## Scaffolding

Run `/scaffold {FeatureName}` to generate a complete vertical slice.
Run `/dotnet-health-check` to audit the project against all conventions.
Run `/review` before merging to catch violations.

## Agents Available

- `@dotnet-architect` — architecture decisions and layer placement
- `@api-designer` — endpoint design and OpenAPI
- `@ef-core-specialist` — queries, configurations, migrations
- `@test-engineer` — integration tests and coverage
