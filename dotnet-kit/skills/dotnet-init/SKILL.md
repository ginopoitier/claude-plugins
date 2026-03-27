---
name: dotnet-init
description: >
  Scaffold a new .NET Clean Architecture solution from scratch with full boilerplate —
  domain, application, infrastructure, API projects, docker-compose, and CLI setup commands.
  Load this skill when: "dotnet init", "new solution", "scaffold solution", "create project",
  "new dotnet project", "clean architecture solution", "bootstrap project",
  "new api project", "start new project", "initialize solution".
user-invocable: true
argument-hint: "[SolutionName]"
allowed-tools: Read, Write, Bash, Glob
---

# .NET Init — Clean Architecture Solution Scaffolder

## Core Principles

1. **Complete, not partial** — generate all boilerplate files so the developer can run `dotnet build` and get a green build on first try. No TODOs, no placeholder implementations.
2. **Config drives namespace and paths** — read `DEFAULT_NAMESPACE` and `NEW_PROJECT_BASE_PATH` from `~/.claude/kit.config.md`. Ask the user if not set.
3. **Show CLI commands, not just files** — always end with the `dotnet new sln`, `dotnet add reference`, and `docker compose up` commands. The files alone aren't enough.
4. **Template choice determines scope** — `web-api` is the default; `worker-service` skips endpoints; `modular-monolith` adds bounded context folders.
5. **Docker Compose for local dependencies** — always generate `docker-compose.yml` with SQL Server and Seq. Dev should be able to start with one command.

## Patterns

### Solution Structure

```
{SolutionName}/
  src/
    {Ns}.Domain/
      AssemblyMarker.cs
      Shared/Entity.cs
      Shared/IDomainEvent.cs
      Shared/Result.cs
      Shared/Result{T}.cs
      Shared/Error.cs
      Shared/ErrorType.cs
    {Ns}.Application/
      AssemblyMarker.cs
      DependencyInjection.cs
      Behaviors/LoggingBehavior.cs
      Behaviors/ValidationBehavior.cs
      Behaviors/TransactionBehavior.cs
    {Ns}.Infrastructure/
      AssemblyMarker.cs
      DependencyInjection.cs
      Persistence/AppDbContext.cs
      Persistence/Interceptors/AuditInterceptor.cs
    {Ns}.Api/
      Program.cs
      Endpoints/IEndpointGroup.cs
      Extensions/EndpointExtensions.cs
      Extensions/ResultExtensions.cs
      Middleware/GlobalExceptionHandler.cs
      appsettings.json
      appsettings.Development.json
  tests/
    {Ns}.Domain.Tests/
    {Ns}.Application.Tests/
      AppFactory.cs
  docker-compose.yml
  .gitignore
  Directory.Build.props
  Directory.Packages.props
  {SolutionName}.sln
```

### Generated Domain Shared Types

```csharp
// Domain/Shared/Result.cs
public class Result
{
    public Error? Error { get; }
    public bool IsSuccess => Error is null;
    public bool IsFailure => !IsSuccess;

    private Result() { }
    private Result(Error error) => Error = error;

    public static Result Success() => new();
    public static Result Failure(Error error) => new(error);
    public static implicit operator Result(Error error) => Failure(error);
}

// Domain/Shared/Result{T}.cs
public class Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;
    public bool IsFailure => !IsSuccess;

    private Result(T value) => Value = value;
    private Result(Error error) => Error = error;

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

// Domain/Shared/Error.cs
public record Error(string Code, string Description, ErrorType Type = ErrorType.Failure);

// Domain/Shared/ErrorType.cs
public enum ErrorType { Failure, NotFound, Validation, Conflict, Unauthorized }
```

### Generated Program.cs

```csharp
// Api/Program.cs
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId());

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddExceptionHandler<GlobalExceptionHandler>()
        .AddProblemDetails()
        .AddOpenApi();

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    app.MapAllEndpoints();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}
```

### Generated docker-compose.yml

```yaml
# docker-compose.yml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "Dev_Password123!"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql

  seq:
    image: datalust/seq:latest
    environment:
      ACCEPT_EULA: "Y"
    ports:
      - "5341:5341"
      - "8081:80"
    volumes:
      - seq-data:/data

volumes:
  sqlserver-data:
  seq-data:
```

### CLI Commands Output

```bash
# Create solution and projects
dotnet new sln -n {SolutionName}
dotnet new classlib -n {Ns}.Domain -o src/{Ns}.Domain
dotnet new classlib -n {Ns}.Application -o src/{Ns}.Application
dotnet new classlib -n {Ns}.Infrastructure -o src/{Ns}.Infrastructure
dotnet new webapi -n {Ns}.Api -o src/{Ns}.Api --no-openapi
dotnet new xunit -n {Ns}.Domain.Tests -o tests/{Ns}.Domain.Tests
dotnet new xunit -n {Ns}.Application.Tests -o tests/{Ns}.Application.Tests

# Add to solution
dotnet sln add src/{Ns}.Domain
dotnet sln add src/{Ns}.Application
dotnet sln add src/{Ns}.Infrastructure
dotnet sln add src/{Ns}.Api
dotnet sln add tests/{Ns}.Domain.Tests
dotnet sln add tests/{Ns}.Application.Tests

# Wire project references (dependency direction: inward)
dotnet add src/{Ns}.Application reference src/{Ns}.Domain
dotnet add src/{Ns}.Infrastructure reference src/{Ns}.Application
dotnet add src/{Ns}.Api reference src/{Ns}.Application
dotnet add src/{Ns}.Api reference src/{Ns}.Infrastructure
dotnet add tests/{Ns}.Application.Tests reference src/{Ns}.Api

# Start local dependencies
docker compose up -d

# Run
cd src/{Ns}.Api && dotnet run
```

## Anti-patterns

### Generating Without Asking for Template Choice

```
# BAD — always generate web-api boilerplate
dotnet-init MySolution → generates IEndpointGroup, minimal API setup

# GOOD — ask first if not provided
"What template? web-api (default) / worker-service / modular-monolith"
→ worker-service: skip IEndpointGroup, Endpoints/, minimal API patterns
```

### Skipping CLI Commands

```
# BAD — output only files
Generated 23 files. Done.

# GOOD — include all setup commands
Output: files → dotnet sln add commands → dotnet add reference commands →
docker compose up -d → dotnet run instructions
```

### Using InMemoryDatabase in AppFactory

```csharp
// BAD — tests don't reflect real SQL Server behavior
services.AddDbContext<AppDbContext>(opts => opts.UseInMemoryDatabase("test"));

// GOOD — Testcontainers for real database
private readonly MsSqlContainer _db = new MsSqlBuilder()
    .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
    .Build();
// → override connection string in ConfigureWebHost
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Solution name provided | Start generating immediately |
| Solution name missing | Ask for it — it's required |
| Template not specified | Default to `web-api`, mention other options |
| Auth required | Ask: None / JWT Bearer / ASP.NET Core Identity |
| Neo4j requested | Add Neo4j driver + session factory to Infrastructure |
| DEFAULT_NAMESPACE not configured | Ask user for namespace prefix before generating |
| Existing directory found | Warn user — do not overwrite without confirmation |
| Modular monolith chosen | Add `Modules/` folder, generate one module as example |

## Execution

Load before starting:
@~/.claude/kit.config.md
@~/.claude/knowledge/dotnet/clean-architecture.md
@~/.claude/knowledge/dotnet/result-pattern.md
@~/.claude/knowledge/dotnet/cqrs-mediatr.md
@~/.claude/knowledge/dotnet/minimal-api-patterns.md
@~/.claude/knowledge/dotnet/logging-patterns.md

### Gather context

Ask for anything not in $ARGUMENTS:
1. **Solution name** — becomes root namespace (or use `DEFAULT_NAMESPACE` from config)
2. **Template** — `web-api` / `worker-service` / `modular-monolith` (default: `web-api`)
3. **Initial features** — e.g. "Users, Products" (scaffold first vertical slice for each)
4. **Auth** — None / JWT Bearer / ASP.NET Core Identity
5. **Neo4j?** — Include Neo4j infrastructure setup?

### Output order

1. All files with full path as `###` heading
2. `dotnet new sln` + project creation CLI commands
3. `dotnet add reference` wiring commands
4. `docker compose up -d` to start dependencies
5. How to run locally

$ARGUMENTS
