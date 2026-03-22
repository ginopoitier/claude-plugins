---
name: httpclient-factory
description: >
  IHttpClientFactory and typed HTTP clients for .NET applications. Covers
  named/typed/keyed clients, DelegatingHandlers, resilience with
  Microsoft.Extensions.Http.Resilience, and testing patterns.
  Load this skill when configuring HTTP clients, adding retry/circuit breaker
  policies, or when the user mentions "HttpClient", "IHttpClientFactory",
  "AddHttpClient", "typed client", "named client", "DelegatingHandler",
  "resilience", "retry", "circuit breaker", "hedging", "Polly",
  "AddStandardResilienceHandler", "socket exhaustion", or "Refit".
user-invocable: true
argument-hint: "[service name or external API to configure]"
allowed-tools: Read, Write, Edit, Bash, Grep, Glob
---

# HttpClient Factory

## Core Principles

1. **Never `new HttpClient()` per request** — Raw `HttpClient` creation causes socket exhaustion under load and ignores DNS changes. Use `IHttpClientFactory` to manage handler lifetimes.
2. **Keyed clients over typed clients** — Keyed DI (`.AddAsKeyed()`) is the recommended pattern in .NET 10. Typed clients captured in singletons silently break handler rotation.
3. **Resilience is not optional** — Every external HTTP call needs retry, circuit breaker, and timeout. `AddStandardResilienceHandler()` provides sensible defaults in one line.
4. **DelegatingHandlers for cross-cutting concerns** — Auth tokens, correlation IDs, and logging belong in the handler pipeline.

## Patterns

### Named Client with Resilience

```csharp
builder.Services.AddHttpClient("github", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp/1.0");
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
})
.AddStandardResilienceHandler();

// Usage via factory
public sealed class GitHubService(IHttpClientFactory factory)
{
    public async Task<Repo?> GetRepoAsync(string owner, string name, CancellationToken ct)
    {
        var client = factory.CreateClient("github");
        return await client.GetFromJsonAsync<Repo>($"repos/{owner}/{name}", ct);
    }
}
```

### Keyed Client (Recommended in .NET 10)

```csharp
builder.Services.AddHttpClient("payments", client =>
{
    client.BaseAddress = new Uri("https://api.payments.example.com/");
})
.AddStandardResilienceHandler()
.AddAsKeyed();  // Register as keyed scoped service

// Inject directly — no IHttpClientFactory needed
app.MapPost("/charge", async (
    [FromKeyedServices("payments")] HttpClient httpClient,
    ChargeRequest request,
    CancellationToken ct) =>
{
    var response = await httpClient.PostAsJsonAsync("charges", request, ct);
    return response.IsSuccessStatusCode
        ? TypedResults.Ok()
        : TypedResults.Problem("Payment failed");
});
```

### Standard Resilience Handler

`AddStandardResilienceHandler()` chains 5 strategies:

| Strategy | Default |
|----------|---------|
| Rate limiter | 1000 concurrent requests |
| Total timeout | 30 seconds |
| Retry | 3 retries, exponential backoff with jitter |
| Circuit breaker | Opens at 10% failure rate |
| Attempt timeout | 10 seconds per attempt |

```csharp
builder.Services.AddHttpClient("api")
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 5;
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);

        // Disable retries for non-idempotent methods
        options.Retry.DisableForUnsafeHttpMethods();
    });
```

### DelegatingHandler for Auth Token Injection

```csharp
public sealed class AuthenticationHandler(ITokenService tokenService)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenService.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}

// Registration
builder.Services.AddTransient<AuthenticationHandler>();
builder.Services.AddHttpClient("api")
    .AddHttpMessageHandler<AuthenticationHandler>()
    .AddStandardResilienceHandler();
```

## Anti-patterns

### Don't Create HttpClient Per Request

```csharp
// BAD — socket exhaustion under load
public async Task<string> GetDataAsync()
{
    using var client = new HttpClient();
    return await client.GetStringAsync("https://api.example.com/data");
}

// GOOD — factory-managed
public async Task<string> GetDataAsync(CancellationToken ct)
{
    var client = factory.CreateClient("api");
    return await client.GetStringAsync("https://api.example.com/data", ct);
}
```

### Don't Capture Typed Clients in Singletons

```csharp
// BAD — transient HttpClient captured by singleton defeats handler rotation
services.AddSingleton<MySingletonService>();
services.AddHttpClient<MySingletonService>();

// GOOD — use keyed client or IHttpClientFactory in singletons
services.AddSingleton<MySingletonService>();
services.AddHttpClient("myservice").AddAsKeyed(ServiceLifetime.Singleton);
```

### Don't Forget CancellationToken

```csharp
// BAD — no cancellation support
var result = await httpClient.GetFromJsonAsync<Order>("/orders/1");

// GOOD — always pass CancellationToken
var result = await httpClient.GetFromJsonAsync<Order>("/orders/1", cancellationToken);
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| New .NET 10 project | Keyed clients with `AddAsKeyed()` |
| External API calls | `AddStandardResilienceHandler()` on every client |
| Auth token injection | `DelegatingHandler` with `AddHttpMessageHandler` |
| Hedging (parallel requests) | `AddStandardHedgingHandler()` |
| Non-idempotent methods | `DisableForUnsafeHttpMethods()` on retry options |
| API client generation | Refit with `AddRefitClient<T>()` |
