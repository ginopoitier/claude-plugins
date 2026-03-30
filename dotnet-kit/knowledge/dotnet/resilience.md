# Resilience — Polly v8 Reference

## Overview

Polly v8 ships as a first-class part of .NET 9 via `Microsoft.Extensions.Http.Resilience` and `Microsoft.Extensions.Resilience`. It replaces the old policy-per-call API with typed `ResiliencePipeline<T>` objects composed from named strategies. Pipelines are registered in DI and injected where needed — they are reusable, testable, and observable via OpenTelemetry out of the box.

## Setup: Standard Resilience Pipeline for HttpClient

The `AddStandardResilienceHandler()` extension from `Microsoft.Extensions.Http.Resilience` wires up rate limiting, total request timeout, retry, circuit breaker, and attempt timeout in a single call — each with battle-tested defaults.

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration config)
{
    services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>(client =>
    {
        client.BaseAddress = new Uri(config["PaymentGateway:BaseUrl"]!);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    // Adds: RateLimiter → TotalRequestTimeout → Retry → CircuitBreaker → AttemptTimeout
    // Defaults: 3 retries, 30s total timeout, 5-of-10 failure threshold for CB
    .AddStandardResilienceHandler();

    return services;
}
```

## Pattern: Custom ResiliencePipeline in DI

Use a named pipeline when you need non-standard configuration or want to share a pipeline across multiple callers.

```csharp
// Infrastructure/DependencyInjection.cs
services.AddResiliencePipeline<string, HttpResponseMessage>("payment-gateway", (builder, ctx) =>
{
    builder
        // Timeout per attempt — prevents a single slow upstream from blocking a thread indefinitely
        .AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
            Name = "AttemptTimeout"
        })

        // Retry: exponential back-off with jitter prevents retry storms when many clients fail together
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = 3,
            Delay             = TimeSpan.FromMilliseconds(200),
            BackoffType       = DelayBackoffType.Exponential,
            UseJitter         = true,   // adds ±25 % random jitter to the delay
            Name              = "Retry",

            // Only retry on transient HTTP failures, not 4xx client errors
            ShouldHandle = args => args.Outcome switch
            {
                { Exception: HttpRequestException }         => PredicateResult.True(),
                { Result.StatusCode: HttpStatusCode.ServiceUnavailable } => PredicateResult.True(),
                { Result.StatusCode: HttpStatusCode.TooManyRequests }    => PredicateResult.True(),
                _ => PredicateResult.False()
            },

            // Log each retry attempt for observability
            OnRetry = args =>
            {
                var logger = ctx.ServiceProvider.GetRequiredService<ILogger<ResiliencePipelineBuilder>>();
                logger.LogWarning(
                    "Retry {AttemptNumber} after {Delay:N0}ms due to {Outcome}",
                    args.AttemptNumber,
                    args.RetryDelay.TotalMilliseconds,
                    args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                return ValueTask.CompletedTask;
            }
        })

        // Circuit breaker: opens after 50% failure rate over 30 s in a 100-request window
        // While open, calls fail immediately without hitting the upstream — protects a struggling dependency
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            Name                       = "CircuitBreaker",
            FailureRatio               = 0.5,
            SamplingDuration           = TimeSpan.FromSeconds(30),
            MinimumThroughput          = 10,            // need at least 10 calls before the CB can trip
            BreakDuration              = TimeSpan.FromSeconds(15),  // half-open after 15 s

            ShouldHandle = args => args.Outcome switch
            {
                { Exception: HttpRequestException } => PredicateResult.True(),
                { Result.StatusCode: >= HttpStatusCode.InternalServerError } => PredicateResult.True(),
                _ => PredicateResult.False()
            },

            OnOpened = args =>
            {
                var logger = ctx.ServiceProvider.GetRequiredService<ILogger<ResiliencePipelineBuilder>>();
                logger.LogError(
                    "Circuit breaker opened for {BreakDuration:N0}s. Last outcome: {Outcome}",
                    args.BreakDuration.TotalSeconds,
                    args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                return ValueTask.CompletedTask;
            }
        })

        // Total timeout: caps the entire pipeline including all retries
        .AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(30),
            Name    = "TotalTimeout"
        });
});
```

## Pattern: Using a Named Pipeline

```csharp
// Infrastructure/Http/PaymentGatewayClient.cs
public sealed class PaymentGatewayClient(
    HttpClient http,
    ResiliencePipelineProvider<string> pipelines,
    ILogger<PaymentGatewayClient> logger)
    : IPaymentGatewayClient
{
    // Resolve the pipeline once and reuse — pipelines are thread-safe
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline =
        pipelines.GetPipeline<HttpResponseMessage>("payment-gateway");

    public async Task<Result<PaymentResult>> ChargeAsync(
        ChargeRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await _pipeline.ExecuteAsync(
                async innerCt =>
                {
                    var httpResponse = await http.PostAsJsonAsync("/v1/charges", request, innerCt);
                    return httpResponse;
                },
                ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Payment gateway returned {StatusCode}", response.StatusCode);
                return PaymentErrors.GatewayRejected;
            }

            var result = await response.Content.ReadFromJsonAsync<PaymentResult>(ct);
            return result!;
        }
        catch (BrokenCircuitException ex)
        {
            // Circuit is open — fail fast, return a degraded result instead of hanging
            logger.LogError(ex, "Payment circuit breaker is open");
            return PaymentErrors.ServiceUnavailable;
        }
        catch (TimeoutRejectedException ex)
        {
            logger.LogError(ex, "Payment gateway timed out");
            return PaymentErrors.Timeout;
        }
    }
}
```

## Pattern: Hedging (Speculative Execution)

Hedging fires a second parallel request if the first doesn't respond within a threshold — useful for p99 latency reduction on idempotent read operations.

```csharp
// Only appropriate for idempotent reads — never use hedging on non-idempotent writes
services.AddResiliencePipeline<string, ProductDto?>("product-lookup-hedged", (builder, ctx) =>
{
    builder.AddHedging(new HedgingStrategyOptions<ProductDto?>
    {
        // If the first attempt hasn't responded in 300 ms, fire a second request in parallel
        Delay          = TimeSpan.FromMilliseconds(300),
        MaxHedgedAttempts = 2,      // 1 original + 2 hedged = 3 max in-flight

        // Only hedge on delay; don't hedge immediately after an exception
        ShouldHandle = args => args.Outcome switch
        {
            { Exception: null } => PredicateResult.False(),   // succeeded — no hedge
            _                   => PredicateResult.True()
        },

        ActionGenerator = static args => () =>
            // Each hedged attempt is an independent call to the same operation
            args.Callback(args.ActionContext)
    });
});
```

## Pattern: Non-HTTP Resilience (Database, File I/O)

Pipelines work on any async operation, not just HTTP.

```csharp
// Infrastructure/Resilience/DatabaseResiliencePipeline.cs
// Register a pipeline for transient SQL Server failures (deadlocks, connection pool exhaustion)
services.AddResiliencePipeline("sql-transient", builder =>
{
    builder.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 4,
        Delay            = TimeSpan.FromMilliseconds(100),
        BackoffType      = DelayBackoffType.Exponential,
        UseJitter        = true,

        // SqlException error numbers for transient SQL Server errors
        ShouldHandle = args => args.Outcome.Exception is SqlException sqlEx
            && IsTransient(sqlEx.Number)
            ? PredicateResult.True()
            : PredicateResult.False()
    });
});

private static bool IsTransient(int errorNumber) => errorNumber switch
{
    -2 or 20 or 64 or 233 or 10053 or 10054 or 10060 or 40197 or 40501 or 40613 => true,
    _ => false
};

// Usage in a repository
public sealed class OrderRepository(
    AppDbContext db,
    ResiliencePipelineProvider<string> pipelines)
    : IOrderRepository
{
    private readonly ResiliencePipeline _sql = pipelines.GetPipeline("sql-transient");

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct) =>
        await _sql.ExecuteAsync(
            innerCt => db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, innerCt),
            ct);
}
```

## Anti-patterns

### Don't use retry without jitter

```csharp
// BAD — fixed-interval retry causes a synchronized thundering herd: all clients
//       retry at exactly the same time, amplifying load on the already-struggling upstream
builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
{
    MaxRetryAttempts = 3,
    Delay            = TimeSpan.FromSeconds(1),
    BackoffType      = DelayBackoffType.Constant   // every client waits exactly 1 s
});

// GOOD — exponential backoff + jitter spreads retries across a time window
builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
{
    MaxRetryAttempts = 3,
    Delay            = TimeSpan.FromMilliseconds(200),
    BackoffType      = DelayBackoffType.Exponential,
    UseJitter        = true    // ±25 % random offset prevents synchronized retry storms
});
```

### Don't catch BrokenCircuitException at the call site and retry manually

```csharp
// BAD — wrapping an already-resilient pipeline in another retry loop defeats the circuit breaker;
//       when the circuit is open it should fail fast, not keep hammering the upstream
for (int i = 0; i < 5; i++)
{
    try { return await _pipeline.ExecuteAsync(...); }
    catch (BrokenCircuitException) { await Task.Delay(1000); }
}

// GOOD — handle BrokenCircuitException as a definitive failure; return a degraded result
try { return await _pipeline.ExecuteAsync(...); }
catch (BrokenCircuitException)
{
    return Result.Failure(Error.ServiceUnavailable("Payment gateway unavailable"));
}
```

### Don't apply retry to non-idempotent writes without deduplication

```csharp
// BAD — retrying a payment charge without idempotency key can result in double charges
builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
{
    MaxRetryAttempts = 3,
    ShouldHandle = _ => PredicateResult.True()   // retries ALL failures including network errors on POST
});

// GOOD — use an idempotency key so the payment gateway deduplicates retried charges
var idempotencyKey = Guid.NewGuid().ToString();  // generated once per business operation
var response = await _pipeline.ExecuteAsync(innerCt =>
{
    var request = new HttpRequestMessage(HttpMethod.Post, "/v1/charges");
    request.Headers.Add("Idempotency-Key", idempotencyKey);  // same key on every retry
    request.Content = JsonContent.Create(charge);
    return http.SendAsync(request, innerCt);
}, ct);
```

## Reference

**NuGet Packages:**
```
Microsoft.Extensions.Http.Resilience   9.0.*
Microsoft.Extensions.Resilience        9.0.*
Polly                                  8.*
Polly.Extensions.Http                  3.*
```

**OpenTelemetry integration — automatic with Polly v8:**
Polly v8 emits metrics (`resilience.polly.*`) and traces automatically when `AddOpenTelemetry()` is configured. No additional setup required.
