# Rule: Minimal API Design

## DO
- Group endpoints using `IEndpointGroup` with auto-discovery via `MapAllEndpoints()`
- One `IEndpointGroup` implementation per feature/domain (e.g. `OrderEndpoints`, `ProductEndpoints`)
- Use `TypedResults` over generic `Results` methods: `TypedResults.Ok(value)` not `Results.Ok(value)`
- Use `MapGroup("/api/{feature}")` with `.WithTags(...)` and `.RequireAuthorization()` where needed
- Add `AddEndpointFilter<ValidationFilter<T>>()` for request body validation
- Return `TypedResults.CreatedAtRoute(...)` for POST endpoints that create resources
- Return `TypedResults.NoContent()` for DELETE and action endpoints
- Use `CancellationToken ct` as last parameter in every endpoint handler
- Map `Result` failures via `result.Error!.ToProblemDetails()`

## DON'T
- Don't put business logic in endpoints — only `await sender.Send(command, ct)`
- Don't define inline lambdas in `Program.cs` for routes — always use `IEndpointGroup`
- Don't return domain entities — always return DTOs/response records
- Don't use `[FromBody]`, `[FromRoute]` attributes — Minimal APIs bind automatically
- Don't return `IResult` when you can return a typed result (OpenAPI benefits)
- Don't add Swashbuckle — use built-in `Microsoft.AspNetCore.OpenApi`

## Example

```csharp
// GOOD — TypedResults, ISender only, no business logic
private static async Task<IResult> CreateOrder(
    CreateOrderRequest request, ISender sender, CancellationToken ct)
{
    var result = await sender.Send(new CreateOrderCommand(request.CustomerId, request.Amount), ct);
    return result.IsFailure
        ? result.Error!.ToProblemDetails()
        : TypedResults.CreatedAtRoute("GetOrder", new { id = result.Value }, result.Value);
}

// BAD — business logic in endpoint, IResult return type loses OpenAPI metadata
app.MapPost("/orders", async (CreateOrderRequest req, AppDbContext db) => {
    var order = new Order { ... }; // business logic here!
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Ok(order); // returns domain entity, not DTO
});
```

## Deep Reference
For full patterns: @~/.claude/knowledge/dotnet/minimal-api-patterns.md
