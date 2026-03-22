# Worker Service Project

Clean Architecture · MediatR CQRS · EF Core (SQL Server) · Result Pattern · Serilog + Seq · Background Processing

## Project Layout

```
src/
  {Ns}.Domain/           # Entities, value objects, domain events, errors, Result types
  {Ns}.Application/      # Handlers, validators, DTOs, pipeline behaviors, interfaces
  {Ns}.Infrastructure/   # DbContext, EF configs, migrations, external services, message handlers
  {Ns}.Worker/           # IHostedService workers, DI wiring, Program.cs
tests/
  {Ns}.Domain.Tests/
  {Ns}.Application.Tests/
```

## Key Conventions

- Workers are thin — they only trigger MediatR commands
- All business logic lives in Application handlers
- Use `PeriodicTimer` over `Task.Delay` in loop-based workers
- `IHostedService` for background work, `BackgroundService` base class for looping work
- Graceful shutdown via `stoppingToken` (CancellationToken) on every async call
- Serilog structured logging → Seq
- No HTTP endpoints — if needed, add `{Ns}.Api` project

## Worker Pattern

```csharp
// Always inject ISender, not handlers directly
public sealed class OrderProcessingWorker(ISender sender, ILogger<OrderProcessingWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var result = await sender.Send(new ProcessPendingOrdersCommand(), stoppingToken);
            if (result.IsFailure)
                logger.LogWarning("Order processing failed: {Error}", result.Error!.Description);
        }
    }
}
```

## Agents Available

- `@dotnet-architect` — architecture and feature design
- `@ef-core-specialist` — data access patterns
- `@test-engineer` — worker and handler tests
