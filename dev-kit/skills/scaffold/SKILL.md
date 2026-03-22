---
name: scaffold
description: >
  Generate a complete vertical slice feature for a Clean Architecture .NET project —
  command/query, handler, validator, endpoint, errors, and xUnit test.
  Load this skill when: "scaffold", "vertical slice", "generate feature", "create feature",
  "new command", "new query", "new endpoint", "boilerplate", "scaffold feature",
  "create handler", "generate slice", "add feature".
user-invocable: true
argument-hint: "[FeatureName]"
allowed-tools: Read, Write, Glob, Grep
---

# Scaffold — Vertical Slice Generator

## Core Principles

1. **Scan before generating** — always Glob the project structure first to detect solution name, namespace, and existing patterns. Never hardcode assumptions about folder layout.
2. **All 7 files, every time** — a complete slice includes command/query, validator, handler, response DTO, errors, endpoint, and test. Partial scaffolding creates dead code.
3. **Quality checklist is a hard gate** — every generated file must pass the checklist before output. No TODOs, no compilation errors, no skipped patterns.
4. **Use `DEFAULT_NAMESPACE` from kit config** — read `~/.claude/kit.config.md` for the root namespace. Fall back to asking the user if not set.
5. **Show, then confirm for large generations** — for multi-feature scaffolding (both command and query), show the file list and ask before writing.

## Patterns

### File Layout

| File | Location |
|------|----------|
| Command or Query record | `Application/{Feature}/{Layer}/{Name}/{Name}Command.cs` |
| Validator | `Application/{Feature}/{Layer}/{Name}/{Name}Validator.cs` |
| Handler | `Application/{Feature}/{Layer}/{Name}/{Name}Handler.cs` |
| Response DTO | `Application/{Feature}/{Layer}/{Name}/{Name}Response.cs` |
| Error definitions | `Domain/{Feature}/{Feature}Errors.cs` (add to if exists) |
| Endpoint group | `Api/Endpoints/{Feature}/{Name}Endpoint.cs` |
| xUnit test | `tests/{App}.Application.Tests/{Feature}/{Layer}/{Name}HandlerTests.cs` |

### Generated Handler — Command

```csharp
// Application/Orders/Commands/CreateOrder/CreateOrderHandler.cs
internal sealed class CreateOrderHandler(AppDbContext db, ILogger<CreateOrderHandler> logger)
    : IRequestHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
    public async Task<Result<CreateOrderResponse>> Handle(
        CreateOrderCommand request, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([request.CustomerId], ct);
        if (customer is null)
            return CustomerErrors.NotFound;

        var order = Order.Create(new CustomerId(request.CustomerId), new Money(request.Amount, "EUR"));
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Order {OrderId} created for customer {CustomerId}",
            order.Id.Value, request.CustomerId);

        return new CreateOrderResponse(order.Id.Value, order.Status.ToString());
    }
}
```

### Generated Handler — Query

```csharp
// Application/Orders/Queries/GetOrder/GetOrderHandler.cs
internal sealed class GetOrderHandler(AppDbContext db)
    : IRequestHandler<GetOrderQuery, Result<GetOrderResponse>>
{
    public async Task<Result<GetOrderResponse>> Handle(
        GetOrderQuery request, CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Where(o => o.Id == new OrderId(request.OrderId))
            .Select(o => new GetOrderResponse(o.Id.Value, o.Status.ToString(), o.TotalAmount.Amount))
            .FirstOrDefaultAsync(ct);

        return order is null ? OrderErrors.NotFound : order;
    }
}
```

### Generated Endpoint

```csharp
// Api/Endpoints/Orders/CreateOrderEndpoint.cs
public sealed class CreateOrderEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        group.MapPost("", CreateOrder)
             .WithName("CreateOrder");

        group.MapGet("{id:guid}", GetOrder)
             .WithName("GetOrder");
    }

    private static async Task<IResult> CreateOrder(
        CreateOrderRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateOrderCommand(request.CustomerId, request.Amount), ct);

        return result.IsFailure
            ? result.Error!.ToProblemDetails()
            : TypedResults.CreatedAtRoute(result.Value, "GetOrder", new { id = result.Value.Id });
    }

    private static async Task<IResult> GetOrder(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetOrderQuery(id), ct);
        return result.IsFailure ? result.Error!.ToProblemDetails() : TypedResults.Ok(result.Value);
    }
}
```

### Generated Test

```csharp
// tests/App.Application.Tests/Orders/Commands/CreateOrderHandlerTests.cs
public class CreateOrderHandlerTests(AppFactory factory) : IClassFixture<AppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateOrder_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new { CustomerId = Guid.NewGuid(), Amount = 99.99m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        body!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task CreateOrder_InvalidAmount_Returns422()
    {
        var request = new { CustomerId = Guid.NewGuid(), Amount = -1m };
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetOrder_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

## Anti-patterns

### Scaffolding Without Reading Existing Patterns

```
# BAD — generate generic boilerplate without checking project
Write CreateOrderHandler.cs using assumed namespace "MyApp"

# GOOD — scan first
Glob "**/*.csproj" → find "Acme.Orders.Application"
Glob "Application/*/Commands/**/*Handler.cs" → check existing handler style
→ generate matching exactly: namespace Acme.Orders.Application.Orders.Commands.CreateOrder;
```

### Business Logic in the Endpoint

```csharp
// BAD — calculation in endpoint
app.MapPost("/api/orders", async (CreateOrderRequest req, ISender sender, CancellationToken ct) =>
{
    var discount = req.Amount > 1000 ? 0.1m : 0; // ← business logic here
    var result = await sender.Send(new CreateOrderCommand(req.CustomerId, req.Amount - discount), ct);
    ...
});

// GOOD — endpoint only calls Send()
app.MapPost("/api/orders", async (CreateOrderRequest req, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new CreateOrderCommand(req.CustomerId, req.Amount), ct);
    return result.IsFailure ? result.Error!.ToProblemDetails() : TypedResults.Created(...);
});
```

### Skipping the Test or the Error File

```
# BAD — "we'll add tests later"
Generated: Command, Handler, Endpoint
Skipped: Test, Errors, Validator

# GOOD — complete slice, always
Generated: Command, Validator, Handler, Response, Errors, Endpoint, Test (7 files)
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Feature name given | Scaffold immediately after project scan |
| Command AND query needed | Generate both — 14 files total |
| Entity doesn't exist yet | Ask if user wants Domain entity scaffolded too |
| Existing errors file found | Add to it — don't create a duplicate |
| Namespace not in config | Ask user before generating |
| Query-only (read-only feature) | Skip command, validator, transaction behavior |
| Unsure about request fields | Ask before generating — don't assume |

## Execution

Load these before generating:
@~/.claude/kit.config.md
@~/.claude/knowledge/dotnet/clean-architecture.md
@~/.claude/knowledge/dotnet/cqrs-mediatr.md
@~/.claude/knowledge/dotnet/result-pattern.md
@~/.claude/knowledge/dotnet/minimal-api-patterns.md

### Gather context

If not provided in $ARGUMENTS, ask:
1. Feature name (e.g. `CreateOrder`, `GetProduct`, `CancelSubscription`)
2. Command, Query, or both?
3. Entity/aggregate involved
4. Request fields (name: type)
5. Response fields (name: type) — if any

Scan the project structure first with Glob to detect the solution name and existing patterns.

### Quality checklist

Every generated file must comply with all active rules. Verify before outputting:
- [ ] Handler is `internal sealed`, uses primary constructor
- [ ] Returns `Result<T>` or `Result` — no business exceptions
- [ ] `CancellationToken ct` on every async call
- [ ] `AsNoTracking()` on all query DbContext calls
- [ ] Error mapped to `ToProblemDetails()` in endpoint
- [ ] Correct HTTP status code (201/200/204)
- [ ] Structured logging for the significant business event
- [ ] Test covers: happy path + not found + validation failure

### Output

Show each file with its full path as a `###` heading. Complete, compilable code — no TODOs.
Use `DEFAULT_NAMESPACE` from kit config as the root namespace.

End with a one-line summary of what was generated and any assumptions made.

$ARGUMENTS
