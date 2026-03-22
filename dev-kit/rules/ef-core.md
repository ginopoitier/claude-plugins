# Rule: Entity Framework Core

## DO
- Use `DbContext` directly in handlers — no generic repository wrappers
- Always use `AsNoTracking()` in query handlers (reads)
- Always pass `CancellationToken ct` to every async EF call
- Use `IEntityTypeConfiguration<T>` per entity — never configure in `OnModelCreating` directly
- Use `ApplyConfigurationsFromAssembly()` to auto-register all configurations
- Use strongly typed IDs with EF value converters
- Use owned entities for value objects (`OwnsOne`, `OwnsMany`)
- Use `HasPrecision(18, 2)` for all `decimal` money columns
- Use `HasConversion<string>()` for enums stored as strings
- Override `SaveChangesAsync` to dispatch domain events after save

## DON'T
- Don't use `.Include()` in query handlers — project directly to DTOs with `.Select()`
- Don't load full entities when only a subset of fields is needed
- Don't call `SaveChanges()` — always `SaveChangesAsync(ct)`
- Don't use `EnsureCreated()` in production — use migrations
- Don't use `database.EnsureDeleted()` anywhere but test setup
- Don't lazy load navigation properties — always explicit loading or projection
- Don't put EF Core configurations in the Domain or Application layer

## Example

```csharp
// GOOD — AsNoTracking, projection to DTO, no Include
var orders = await db.Orders
    .AsNoTracking()
    .Where(o => o.CustomerId == new CustomerId(request.CustomerId))
    .Select(o => new OrderDto(o.Id.Value, o.Status.ToString(), o.TotalAmount.Amount))
    .ToListAsync(ct);

// BAD — no AsNoTracking, loads full entity + navigation via Include, then maps in memory
var orders = await db.Orders
    .Include(o => o.Items)          // loads all columns + related rows into memory
    .ToListAsync(ct);               // then maps AFTER materializing — N+1 risk
var dtos = orders.Select(o => new OrderDto(...));
```

## Deep Reference
For full patterns: @~/.claude/knowledge/dotnet/ef-core-patterns.md
