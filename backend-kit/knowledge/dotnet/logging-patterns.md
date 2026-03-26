# Logging — Serilog + Seq Reference

## NuGet Packages

```xml
<PackageReference Include="Serilog.AspNetCore" Version="*" />
<PackageReference Include="Serilog.Sinks.Seq" Version="*" />
<PackageReference Include="Serilog.Enrichers.Environment" Version="*" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="*" />
<PackageReference Include="Serilog.Enrichers.Process" Version="*" />
```

## Program.cs Bootstrap

```csharp
// Bootstrap logger catches startup errors
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

    // ... rest of setup

    var app = builder.Build();
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        opts.EnrichDiagnosticContext = (diag, httpCtx) =>
        {
            diag.Set("RequestHost", httpCtx.Request.Host.Value);
            diag.Set("UserAgent", httpCtx.Request.Headers.UserAgent);
        };
    });

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

## appsettings.json Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341",
          "apiKey": ""
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

## Usage in Handlers / Services

```csharp
// Always use structured properties, never string interpolation
internal sealed class CreateOrderHandler(AppDbContext db, ILogger<CreateOrderHandler> logger)
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // CORRECT — structured logging
        logger.LogInformation("Creating order for customer {CustomerId} with amount {Amount}",
            request.CustomerId, request.Amount);

        // WRONG — never do this
        // logger.LogInformation($"Creating order for customer {request.CustomerId}");

        var order = Order.Create(...);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Order {OrderId} created successfully", order.Id.Value);
        return order.Id.Value;
    }
}
```

## Log Scopes for Correlation

```csharp
// Add correlation ID middleware
app.Use(async (ctx, next) =>
{
    var correlationId = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        ctx.Response.Headers["X-Correlation-Id"] = correlationId;
        await next();
    }
});
```

## Log Levels Reference

| Level | When to use |
|-------|-------------|
| `Verbose` | Very detailed — disable in prod |
| `Debug` | Dev diagnostics — disable in prod |
| `Information` | Normal business events (order placed, user registered) |
| `Warning` | Recoverable issues, unexpected but non-fatal state |
| `Error` | Failures that need attention but don't crash the app |
| `Fatal` | App cannot continue — startup failures, critical failures |

## MediatR Logging Pipeline

The `LoggingBehavior<TRequest, TResponse>` in `cqrs-mediatr.md` automatically logs every command and query with its name and payload at `Information` level. **Do not duplicate this logging in handlers** — only log business-significant events inside handlers (e.g., "Order shipped", "Payment failed").

## Seq Docker (local dev)

```yaml
# docker-compose.yml
services:
  seq:
    image: datalust/seq:latest
    environment:
      - ACCEPT_EULA=Y
    ports:
      - "5341:5341"
      - "8081:80"
    volumes:
      - seq-data:/data

volumes:
  seq-data:
```

Access at: http://localhost:8081
