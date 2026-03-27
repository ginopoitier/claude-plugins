# Rule: Logging with Serilog + Seq

## DO
- Use `ILogger<T>` injected via primary constructor
- Always use **structured properties**: `logger.LogInformation("Order {OrderId} placed by {UserId}", orderId, userId)`
- Log at the correct level: `Debug` dev noise · `Information` business events · `Warning` recoverable issues · `Error` failures
- Log significant **business events** in handlers (order placed, payment failed, user registered)
- Use `LogContext.PushProperty` for correlation IDs and request-scoped properties
- Configure Serilog with bootstrap logger in `Program.cs` to catch startup errors
- Use `app.UseSerilogRequestLogging()` for HTTP request logs — disable default ASP.NET logging
- Push to Seq in all environments (local: http://localhost:5341, use `SEQ_URL` from config)

## DON'T
- Don't use **string interpolation** in log messages: ~~`$"Order {id} placed"`~~
- Don't duplicate the MediatR `LoggingBehavior` logs inside handlers — only log business-significant events
- Don't log sensitive data: passwords, tokens, PII
- Don't use `Console.WriteLine` or `Debug.WriteLine` — use `ILogger`
- Don't log at `Error` for expected failures (NotFound, Validation) — use `Warning` or `Information`
- Don't configure Serilog in `appsettings.json` only — bootstrap logger must be in `Program.cs`

## Example

```csharp
// GOOD — structured properties, correct level, business event only
logger.LogInformation("Order {OrderId} placed by customer {CustomerId} for {Amount:C}",
    order.Id.Value, request.CustomerId, order.TotalAmount.Amount);

// BAD — string interpolation (loses structured properties), wrong level for expected failure
logger.LogError($"Order {order.Id} not found");      // interpolation loses queryability
logger.LogError("Customer {Id} not found", customerId); // Error for a 404 — use Warning
// BAD — Console.WriteLine instead of ILogger
Console.WriteLine($"Processing order {id}");
```

## Deep Reference
For full setup and docker-compose: @~/.claude/knowledge/dotnet/logging-patterns.md
