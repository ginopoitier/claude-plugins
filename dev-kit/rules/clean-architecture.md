# Rule: Clean Architecture

## Layer Dependency Direction
```
Domain ← Application ← Infrastructure
                     ← Api (Presentation)
```
Dependencies **only point inward**. Never reference Infrastructure from Application or Domain.

## DO
- Keep **Domain** free of all NuGet dependencies except primitives
- Put **business rules and entity behavior** in Domain — not in handlers or services
- Put **use case orchestration** (handlers, validators, DTOs) in Application
- Put **EF Core, external API clients, file I/O** in Infrastructure
- Put **endpoints, middleware, DI wiring** in Api/Presentation
- Define interfaces in Application, implement them in Infrastructure
- Use assembly markers (`internal sealed class AssemblyMarker`) per project for MediatR/FluentValidation registration
- Every project has a `DependencyInjection.cs` extension method (`AddApplication`, `AddInfrastructure`)

## DON'T
- Don't put business logic in endpoints — endpoints only call `ISender.Send()` and map results
- Don't inject `DbContext` into Domain or Application constructors directly — use it in handlers (Application is fine) or abstract it
- Don't reference `Microsoft.AspNetCore.*` from Application or Domain
- Don't use `static` helper classes in Domain that contain business logic — put it on the entity
- Don't create "service" classes that just delegate to a repository — use handlers directly

## Deep Reference
For full patterns and code examples: @~/.claude/knowledge/dotnet/clean-architecture.md
