# Caching — .NET 9 Reference

## Overview

.NET 9 introduces `HybridCache`, which replaces the awkward `GetOrCreateAsync` + `IDistributedCache` combo with a single API that layers an in-process L1 cache (MemoryCache) in front of a distributed L2 cache (Redis, SQL Server). Output caching sits at the HTTP layer and is appropriate for responses that are identical for many users. This document covers all three tiers and when to reach for each.

## Setup: HybridCache (Recommended Default)

HybridCache is the right choice for application-layer caching of business data. It eliminates the cache stampede problem (multiple concurrent misses all hitting the DB) via built-in call coalescing.

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration config)
{
    // L2: Redis distributed cache — required for HybridCache to cache across instances
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration       = config.GetConnectionString("Redis");
        options.InstanceName        = "myapp:";  // namespace prefix — prevents key collisions with other apps
    });

    // HybridCache: wraps the L1 (MemoryCache) and the L2 (Redis) registered above
    services.AddHybridCache(options =>
    {
        // L1 (in-process) limits — keep small to avoid GC pressure
        options.MaximumPayloadBytes   = 1024 * 1024;     // 1 MB max per entry
        options.MaximumKeyLength      = 512;

        // Default expiry: applies when GetOrCreateAsync is called without explicit options
        options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            Expiration          = TimeSpan.FromMinutes(5),   // L2 TTL
            LocalCacheExpiration = TimeSpan.FromMinutes(1)   // L1 TTL — shorter keeps L1 fresh
        };
    });

    return services;
}
```

## Pattern: HybridCache in a Query Handler

```csharp
// Application/Products/Queries/GetProductHandler.cs
internal sealed class GetProductHandler(
    AppDbContext db,
    HybridCache cache)
    : IRequestHandler<GetProductQuery, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(
        GetProductQuery query,
        CancellationToken ct)
    {
        // HybridCache.GetOrCreateAsync coalesces concurrent misses:
        // only ONE database call runs even if 50 requests arrive simultaneously
        var product = await cache.GetOrCreateAsync(
            // Cache key: namespaced so invalidation is scoped and predictable
            key: $"product:{query.ProductId}",

            // Factory: called only on a cache miss
            factory: async innerCt => await db.Products
                .AsNoTracking()
                .Where(p => p.Id == new ProductId(query.ProductId))
                .Select(p => new ProductResponse(p.Id.Value, p.Name, p.Price.Amount))
                .FirstOrDefaultAsync(innerCt),

            options: new HybridCacheEntryOptions
            {
                Expiration           = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            },
            cancellationToken: ct);

        return product is null ? ProductErrors.NotFound : product;
    }
}
```

## Pattern: Cache Invalidation on Write

Invalidate the specific entry (or a tag-based group) when the underlying data changes. HybridCache invalidates both L1 and L2 atomically when called via `RemoveAsync`.

```csharp
// Application/Products/Commands/UpdateProductHandler.cs
internal sealed class UpdateProductHandler(
    AppDbContext db,
    HybridCache cache)
    : IRequestHandler<UpdateProductCommand, Result>
{
    public async Task<Result> Handle(UpdateProductCommand cmd, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([new ProductId(cmd.ProductId)], ct);
        if (product is null) return ProductErrors.NotFound;

        product.UpdatePrice(new Money(cmd.NewPrice, product.Price.Currency));
        await db.SaveChangesAsync(ct);

        // Remove the cached entry so the next read fetches fresh data
        // HybridCache evicts from both L1 (in-process) and L2 (Redis)
        await cache.RemoveAsync($"product:{cmd.ProductId}", ct);

        return Result.Success();
    }
}
```

## Pattern: Tag-Based Invalidation

Tags let you invalidate a group of related entries without knowing each key individually.

```csharp
// On write: store cache entries with a tag
var product = await cache.GetOrCreateAsync(
    key:  $"product:{id}",
    factory: async ct => await LoadFromDb(id, ct),
    options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) },
    tags: ["products", $"product-category:{categoryId}"],  // tags for group invalidation
    cancellationToken: ct);

// On bulk update: invalidate all products in a category without enumerating keys
await cache.RemoveByTagAsync("product-category:electronics", ct);

// On full cache flush (e.g., after a bulk import):
await cache.RemoveByTagAsync("products", ct);
```

## Pattern: IDistributedCache for Raw Bytes / Custom Serialization

Use `IDistributedCache` directly when you need binary serialization, custom TTL logic per entry, or when HybridCache's overhead is not warranted (e.g., very short-lived tokens).

```csharp
// Infrastructure/Auth/RefreshTokenStore.cs
public sealed class RefreshTokenStore(IDistributedCache cache) : IRefreshTokenStore
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task StoreAsync(
        string token,
        RefreshTokenData data,
        CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions);

        // Absolute expiry: token is invalid after this point regardless of usage
        await cache.SetAsync(CacheKey(token), bytes,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
            },
            ct);
    }

    public async Task<RefreshTokenData?> GetAsync(string token, CancellationToken ct)
    {
        var bytes = await cache.GetAsync(CacheKey(token), ct);
        return bytes is null
            ? null
            : JsonSerializer.Deserialize<RefreshTokenData>(bytes, JsonOptions);
    }

    public Task RemoveAsync(string token, CancellationToken ct) =>
        cache.RemoveAsync(CacheKey(token), ct);

    // Always prefix keys to avoid collisions with other cache users
    private static string CacheKey(string token) => $"refresh_token:{token}";
}
```

## Pattern: Output Caching for HTTP Responses

Output caching operates at the HTTP layer before your handler runs — ideal for responses that are the same across many anonymous requests (catalog pages, public APIs).

```csharp
// Infrastructure/DependencyInjection.cs
services.AddOutputCache(options =>
{
    // Named policy for public catalogue endpoints
    options.AddPolicy("PublicCatalogue", builder =>
        builder
            .Cache()
            .Expire(TimeSpan.FromMinutes(5))
            .SetVaryByQuery("category", "page", "pageSize")  // different cache per query string combination
            .Tag("catalogue"));   // tag for programmatic invalidation

    // No-cache policy for authenticated endpoints where output caching is disabled by default
    options.AddPolicy("NoCache", builder => builder.NoCache());
});

// Program.cs — must come after UseRouting, before MapEndpoints
app.UseOutputCache();

// Endpoint — apply the policy by name
group.MapGet("catalogue", GetCatalogue)
     .CacheOutput("PublicCatalogue");

// Evict output cache after an admin updates the catalogue
app.MapPost("admin/catalogue/rebuild", async (IOutputCacheStore store, CancellationToken ct) =>
{
    await store.EvictByTagAsync("catalogue", ct);
    return Results.NoContent();
}).RequireAuthorization(AuthorizationPolicies.IsAdmin);
```

## Anti-patterns

### Don't cache mutable entity objects

```csharp
// BAD — caching the EF Core entity object keeps it alive in memory in a detached state;
//       any in-memory mutations are invisible to other processes and silently lost
var product = await cache.GetOrCreateAsync(
    $"product:{id}",
    async ct => await db.Products.FindAsync([new ProductId(id)], ct));  // returns tracked entity!

// GOOD — always project to an immutable DTO before caching
var product = await cache.GetOrCreateAsync(
    $"product:{id}",
    async ct => await db.Products
        .AsNoTracking()
        .Where(p => p.Id == new ProductId(id))
        .Select(p => new ProductResponse(p.Id.Value, p.Name, p.Price.Amount))  // DTO — safe to serialize
        .FirstOrDefaultAsync(ct));
```

### Don't ignore cache stampedes with IMemoryCache alone

```csharp
// BAD — classic IMemoryCache GetOrCreateAsync is NOT concurrency-safe;
//       under load, all threads see a miss simultaneously and all hit the DB
var product = await memoryCache.GetOrCreateAsync($"product:{id}", async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await db.Products.FindAsync(id);   // N threads all run this on a cold cache
});

// GOOD — HybridCache coalesces concurrent misses into a single factory invocation
var product = await hybridCache.GetOrCreateAsync(
    $"product:{id}",
    async ct => await db.Products.AsNoTracking().Where(...).Select(...).FirstOrDefaultAsync(ct),
    cancellationToken: ct);
```

### Don't use output caching on authenticated endpoints without per-user vary

```csharp
// BAD — response for user A is served to user B; sensitive data leaks across users
group.MapGet("my-orders", GetMyOrders)
     .RequireAuthorization()
     .CacheOutput();  // no user-specific vary key

// GOOD — vary by the user identity claim so each user gets their own cached response,
//         or simply disable output caching for per-user data and use HybridCache instead
group.MapGet("my-orders", GetMyOrders)
     .RequireAuthorization()
     .CacheOutput(builder => builder
         .SetVaryByHeader("Authorization")  // cache key includes the token
         .Expire(TimeSpan.FromSeconds(30)));
```

## Reference

**NuGet Packages:**
```
Microsoft.Extensions.Caching.Hybrid             9.0.*
Microsoft.Extensions.Caching.StackExchangeRedis 9.0.*
Microsoft.Extensions.Caching.Memory             9.0.*
Microsoft.AspNetCore.OutputCaching              9.0.*   (inbox — no NuGet package needed)
```

**Configuration (appsettings.json):**
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,password=yourpassword,ssl=False,abortConnect=False"
  }
}
```

**Decision: which cache tier to use:**

| Scenario | Tier |
|---|---|
| Per-user business data, frequent writes | HybridCache |
| Session tokens, short-lived secrets | IDistributedCache |
| Anonymous HTTP responses, catalog pages | Output Cache |
| In-process computation cache (no Redis) | IMemoryCache |
