---
name: resilience
description: >
  Resilience patterns for .NET applications using Polly v8.
  Covers retry, circuit breaker, timeout, fallback, rate limiter, hedging,
  and composing resilience pipelines.
  Load this skill when implementing retry logic, circuit breakers, handling
  transient failures, or when the user mentions "Polly", "resilience",
  "retry", "circuit breaker", "timeout", "fallback", "rate limit",
  "hedging", "transient fault", "HttpClient resilience", or "resilience pipeline".
user-invocable: true
argument-hint: "[service or HTTP client to add resilience to]"
allowed-tools: Read, Write, Edit, Bash, Grep, Glob
---

# Resilience

## Core Principles

1. **Polly v8 resilience pipelines, not v7 policies** — Polly v8 replaced `Policy` with `ResiliencePipeline`. Never use `PolicyBuilder`, `Policy.Handle<>()`, or `ISyncPolicy`.
2. **Configure via `AddResilienceHandler`, not manual wrapping** — For HTTP calls, use `Microsoft.Extensions.Http.Resilience` which adds pipelines directly to `HttpClient` via DI.
3. **Always set timeouts** — Every external call needs a timeout. Use Polly's `AddTimeout()` as the innermost strategy so it applies per-attempt.
4. **Instrument everything** — Polly v8 emits `Metering` events for OpenTelemetry. Monitor retry rates, circuit breaker state, and timeout frequency.

## Patterns

### HTTP Client Resilience (Recommended Default)

```csharp
// Standard resilience handler covers 90% of use cases
builder.Services.AddHttpClient<IPaymentGateway, PaymentGatewayClient>(client =>
{
    client.BaseAddress = new Uri("https://api.payments.example.com");
})
.AddStandardResilienceHandler();

// The standard handler configures:
// - Retry: 3 attempts, exponential backoff, jitter
// - Circuit breaker: 10% failure ratio over 30s sampling, 30s break
// - Attempt timeout: 10s per attempt
// - Total request timeout: 30s
```

### Custom HTTP Resilience Configuration

```csharp
builder.Services.AddHttpClient<ICatalogService, CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri("https://api.catalog.example.com");
})
.AddResilienceHandler("catalog", builder =>
{
    // Total timeout — outermost, caps total elapsed time
    builder.AddTimeout(TimeSpan.FromSeconds(15));

    // Retry — exponential backoff with jitter
    builder.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        Delay = TimeSpan.FromMilliseconds(500),
        ShouldHandle = static args => ValueTask.FromResult(
            args.Outcome.Result?.StatusCode is HttpStatusCode.RequestTimeout
                or HttpStatusCode.TooManyRequests
                or HttpStatusCode.ServiceUnavailable
                || args.Outcome.Exception is HttpRequestException)
    });

    // Circuit breaker
    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(10),
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromSeconds(30)
    });

    // Per-attempt timeout — innermost
    builder.AddTimeout(TimeSpan.FromSeconds(5));
});
```

### Non-HTTP Resilience Pipeline

```csharp
// For database calls, message queues, or any non-HTTP operation
builder.Services.AddResiliencePipeline("database", builder =>
{
    builder
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromMilliseconds(200),
            ShouldHandle = new PredicateBuilder()
                .Handle<TimeoutException>()
                .Handle<InvalidOperationException>(ex =>
                    ex.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase))
        })
        .AddTimeout(TimeSpan.FromSeconds(10));
});

// Inject and use
public sealed class OrderRepository(
    AppDbContext db,
    [FromKeyedServices("database")] ResiliencePipeline pipeline)
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await pipeline.ExecuteAsync(
            async token => await db.Orders.FindAsync([id], token),
            ct);
    }
}
```

### Rate Limiting (.NET Built-in)

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromSeconds(60);
        opt.QueueLimit = 0;
    });

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Title = "Too many requests", Status = 429 }, ct);
    };
});

app.UseRateLimiter();
app.MapGet("/api/orders", ListOrders).RequireRateLimiting("fixed");
```

## Anti-patterns

### Don't Use Polly v7 API

```csharp
// BAD — v7 policy syntax
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

// GOOD — v8 pipeline via DI
builder.Services.AddHttpClient<IDataService, DataServiceClient>()
    .AddStandardResilienceHandler();
```

### Don't Retry Non-Idempotent Operations Without Idempotency Keys

```csharp
// BAD — retrying a POST that creates a resource risks duplicates
builder.AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 5 });

// GOOD — use idempotency key header for non-idempotent operations
httpClient.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
```

## Decision Guide

| Scenario | Strategy | Configuration |
|----------|----------|---------------|
| HTTP calls to external APIs | `AddStandardResilienceHandler()` | Use defaults |
| HTTP with custom thresholds | `AddResilienceHandler("name", ...)` | Named handler |
| Database / EF Core calls | `AddResiliencePipeline("db", ...)` | Retry on deadlock/timeout |
| Latency-sensitive reads | `AddHedging(...)` | Parallel request after delay threshold |
| Graceful degradation | `AddFallback(...)` | Return cached/default value |
| API rate limiting | `AddRateLimiter()` + `RequireRateLimiting()` | Fixed, sliding, or token bucket |
| Non-idempotent writes | Retry with idempotency key | Or no retry — fail fast |

## Execution

Add the appropriate Polly v8 resilience strategy — standard or custom HTTP resilience handler, non-HTTP pipeline, rate limiter — to the target service or HTTP client using the patterns and decision guide above.

$ARGUMENTS
