# Rule: Performance

## DO
- Use `AsNoTracking()` on all read-only EF Core queries (queries never modify data)
- Use **projection** (`Select()`) to fetch only needed columns — never load full entity for a read
- Use `IAsyncEnumerable<T>` + `yield return` for streaming large datasets
- Cache expensive, rarely-changing data with `IMemoryCache` or `IDistributedCache`
- Use `ResponseCaching` middleware for GET endpoints with stable data
- Use **pagination** for any endpoint that returns collections (`Skip/Take` with a max page size)
- Profile queries with EF Core logging: `EnableSensitiveDataLogging()` in dev
- Use `ValueTask<T>` instead of `Task<T>` for hot paths with frequent synchronous completion
- Add database indexes for any column used in `WHERE`, `ORDER BY`, or `JOIN` clauses
- Use `Span<T>` / `Memory<T>` for parsing and buffer operations in hot paths
- Use `StringBuilder` for string concatenation in loops (not `+=`)

## DON'T
- Don't call `.ToList()` before filtering — always filter, then materialize
- Don't use `.Include()` for query handlers — project to DTOs instead
- Don't load all records to count them — use `CountAsync()`
- Don't use `Task.Result` or `Task.Wait()` in async code — await instead
- Don't create `new HttpClient()` inline — use `IHttpClientFactory`
- Don't perform N+1 queries — batch or join in a single query
- Don't block the thread pool with synchronous I/O in async contexts
- Don't use `DateTime.Now` in hot paths — cache it or use `TimeProvider`
- Don't use `Guid.NewGuid()` as a clustered key (causes index fragmentation) — use sequential GUIDs or `NEWSEQUENTIALID()` in SQL

## EF Core Performance Checklist
```csharp
// ✅ Correct: projection with AsNoTracking
var items = await db.Orders
    .AsNoTracking()
    .Where(o => o.Status == OrderStatus.Pending)
    .Select(o => new OrderDto(o.Id.Value, o.TotalAmount.Amount))
    .ToListAsync(ct);

// ❌ Wrong: loading entity then mapping
var items = await db.Orders.ToListAsync(ct); // loads ALL columns
var dtos = items.Select(o => new OrderDto(o.Id.Value, o.TotalAmount.Amount));
```

## Caching Pattern
```csharp
// Use IMemoryCache for single-instance, IDistributedCache for distributed/Redis
var result = await cache.GetOrCreateAsync($"orders:{customerId}", async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
    return await db.Orders.AsNoTracking().Where(...).ToListAsync();
});
```
