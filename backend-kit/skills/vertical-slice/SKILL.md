---
name: vertical-slice
description: >
  Scaffold a complete vertical slice — command or query, handler, validator, endpoint,
  and integration test for a single feature operation.
  Load this skill when: "vertical slice", "scaffold feature", "scaffold command",
  "/vertical-slice", "generate handler", "scaffold endpoint", "new feature slice",
  "scaffold query", "scaffold command handler".
user-invocable: true
argument-hint: "<Feature> <Operation> [query|command]"
allowed-tools: Read, Write, Edit, Glob, Grep, Bash
---

# Vertical Slice — Complete Feature Scaffold

## Core Principles

1. **Read existing patterns before generating** — Always check `src/*/Application` and `src/*/Api` for existing conventions: namespace style, base classes used, how validators are named, how endpoints are grouped. Generate to match, not to a generic template.
2. **Commands mutate, queries read** — Commands go in `Commands/` and return `Result` or `Result<T>`. Queries go in `Queries/` and always use `AsNoTracking()` with projection to DTOs. Never mix the two.
3. **Handlers are thin orchestrators** — Handlers load an entity, call a domain method, save, and return. Business logic that ends up inside a handler is a violation of the domain model.
4. **Integration tests cover the full slice** — The integration test exercises the HTTP endpoint through to the database. It is not a unit test of the handler. A single integration test covers routing, binding, validation, business logic, and persistence together.
5. **Generate all 5 pieces together** — A vertical slice is only complete when all five files exist: command/query, validator (for commands), handler, endpoint route, and integration test. Partial slices create gaps in test coverage and inconsistent patterns.

## Patterns

### Command Slice Structure

```
Application/<Feature>s/Commands/<Operation><Feature>/
  <Operation><Feature>Command.cs      ← record : IRequest<Result<T>>
  <Operation><Feature>Validator.cs    ← AbstractValidator<TCommand>
  <Operation><Feature>Handler.cs      ← internal sealed class, primary constructor
```

```csharp
// Application/Orders/Commands/CancelOrder/CancelOrderCommand.cs
public record CancelOrderCommand(Guid OrderId) : IRequest<Result>;

// Application/Orders/Commands/CancelOrder/CancelOrderValidator.cs
public class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}

// Application/Orders/Commands/CancelOrder/CancelOrderHandler.cs
internal sealed class CancelOrderHandler(AppDbContext db, ILogger<CancelOrderHandler> logger)
    : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([new OrderId(request.OrderId)], ct);
        if (order is null) return OrderErrors.NotFound;

        var result = order.Cancel();
        if (result.IsFailure) return result;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Order {OrderId} cancelled", request.OrderId);
        return Result.Success();
    }
}
```

### Query Slice Structure

```csharp
// Application/Orders/Queries/GetOrder/GetOrderQuery.cs
public record GetOrderQuery(Guid OrderId) : IRequest<Result<OrderResponse>>;

// Application/Orders/Queries/GetOrder/GetOrderHandler.cs
// GOOD — AsNoTracking() + projection to DTO, no .Include()
internal sealed class GetOrderHandler(AppDbContext db)
    : IRequestHandler<GetOrderQuery, Result<OrderResponse>>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderQuery request, CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Where(o => o.Id == new OrderId(request.OrderId))
            .Select(o => new OrderResponse(
                o.Id.Value,
                o.CustomerId.Value,
                o.Status.ToString(),
                o.TotalAmount.Amount))
            .FirstOrDefaultAsync(ct);

        return order is null ? OrderErrors.NotFound : order;
    }
}
```

### Endpoint Route Addition

```csharp
// Presentation/Endpoints/Orders/OrderEndpoints.cs
// Add routes to the existing endpoint group — do not create a new group
public sealed class OrderEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        group.MapGet("{id:guid}", GetOrder).WithName("GetOrder");
        group.MapDelete("{id:guid}/cancel", CancelOrder);  // ← added for new slice
    }

    private static async Task<IResult> GetOrder(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetOrderQuery(id), ct);
        return result.IsFailure
            ? result.Error!.ToProblemDetails()
            : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> CancelOrder(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new CancelOrderCommand(id), ct);
        return result.IsFailure
            ? result.Error!.ToProblemDetails()
            : TypedResults.NoContent();
    }
}
```

### Integration Test Pattern

```csharp
// tests/MyApp.Tests/Orders/CancelOrderTests.cs
public class CancelOrderTests(AppFactory factory) : IClassFixture<AppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CancelOrder_PendingOrder_Returns204()
    {
        // Arrange
        var orderId = await CreatePendingOrderAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/orders/{orderId}/cancel");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CancelOrder_AlreadyCancelled_Returns409()
    {
        var orderId = await CreatePendingOrderAsync();
        await _client.DeleteAsync($"/api/orders/{orderId}/cancel");  // first cancel

        var response = await _client.DeleteAsync($"/api/orders/{orderId}/cancel");  // second cancel

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelOrder_NonExistentOrder_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/orders/{Guid.NewGuid()}/cancel");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

## Anti-patterns

### Business Logic in Handlers

```csharp
// BAD — handler contains business rules that belong in the domain
internal sealed class CancelOrderHandler(AppDbContext db) : IRequestHandler<...>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([...], ct);
        if (order.Status == OrderStatus.Shipped)  // business rule in handler!
            return Result.Failure(new Error("Cannot cancel shipped orders", ...));
        order.Status = OrderStatus.Cancelled;     // direct mutation, no domain event!
        ...
    }
}

// GOOD — handler delegates to domain method
var result = order.Cancel();  // Order.Cancel() contains the rule and raises the event
if (result.IsFailure) return result;
```

### Query Handler Loading Full Entity with Include

```csharp
// BAD — loads full entity graph, then maps in memory
var order = await db.Orders
    .Include(o => o.LineItems)
    .Include(o => o.Customer)
    .FirstOrDefaultAsync(o => o.Id == id, ct);  // tracks + loads everything

var dto = new OrderResponse(order.Id.Value, order.Customer.Name, ...);

// GOOD — project directly to DTO in the query, no Include
var dto = await db.Orders
    .AsNoTracking()
    .Where(o => o.Id == new OrderId(request.OrderId))
    .Select(o => new OrderResponse(
        o.Id.Value,
        o.Customer.Name,
        o.LineItems.Count))
    .FirstOrDefaultAsync(ct);
```

### Generating Partial Slices

```
// BAD — generating only the handler without the test
/vertical-slice Order Cancel command
→ generates command, handler, endpoint route
→ skips integration test "to add later"
→ later never comes; slice has no test coverage

// GOOD — always generate all 5 pieces as a unit
1. CancelOrderCommand.cs
2. CancelOrderValidator.cs
3. CancelOrderHandler.cs
4. Add route to OrderEndpoints.cs
5. CancelOrderTests.cs with happy path + error paths
→ Run /verify before marking slice complete
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| New write operation (create, update, cancel, etc.) | `/vertical-slice <Feature> <Operation> command` |
| New read operation (get by id, list, search) | `/vertical-slice <Feature> <Operation> query` |
| Operation on existing endpoint group | Add route to existing `IEndpointGroup` — never create a duplicate group |
| Command with no return value | `IRequest<Result>` — endpoint returns `TypedResults.NoContent()` |
| Command that creates a resource | `IRequest<Result<Guid>>` — endpoint returns `TypedResults.CreatedAtRoute(...)` |
| Query returns nullable | Use `Result<T>` — return `EntityErrors.NotFound` if null |
| Validation rules needed | Always generate `AbstractValidator<TCommand>` for commands |
| Query needs sorted/paged list | Use `Skip/Take` with max page size; project to summary DTO |

## Execution

Read `~/.claude/kit.config.md` for `DEFAULT_NAMESPACE` and project structure. Check for existing patterns in `src/*/Application` and `src/*/Api`.

### `/vertical-slice <Feature> <Operation> command`
Generates:
1. `Application/<Feature>s/Commands/<Operation><Feature>/<Operation><Feature>Command.cs`
2. `Application/<Feature>s/Commands/<Operation><Feature>/<Operation><Feature>Validator.cs`
3. `Application/<Feature>s/Commands/<Operation><Feature>/<Operation><Feature>Handler.cs`
4. Route added to `Presentation/Endpoints/<Feature>/<Feature>Endpoints.cs`
5. `tests/<Project>.Tests/<Feature>/<Operation><Feature>Tests.cs` — happy path + key error paths

### `/vertical-slice <Feature> <Operation> query`
Same structure but in `Queries/` folder. Handler always uses `AsNoTracking()` + projection to DTO. No validator needed (queries have no side effects).

### Code Style
Follow all rules in `~/.claude/rules/csharp.md`, `~/.claude/rules/cqrs.md`, `~/.claude/rules/result-pattern.md`.

### After Generation
Run `/verify` to confirm the slice builds and the integration tests pass.

$ARGUMENTS
