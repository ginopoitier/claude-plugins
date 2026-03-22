# Rule: C# Modern Conventions

## DO
- Use **primary constructors** for dependency injection: `class Handler(AppDbContext db, ILogger<Handler> logger)`
- Use **file-scoped namespaces**: `namespace MyApp.Orders.Application;`
- Use **records** for DTOs, commands, queries, value objects: `public record CreateOrderCommand(Guid CustomerId, decimal Amount)`
- Use **pattern matching** over type checks: `if (result is { IsFailure: true, Error: var err })`
- Use `required` modifier for non-optional properties
- Use `IReadOnlyList<T>` for return types, not `List<T>`
- Prefer `var` when the type is obvious from the right side
- Use `nameof()` instead of magic strings for property names
- Use collection expressions: `List<string> items = ["a", "b"]`
- Suffix async methods with `Async`

## DON'T
- Don't use regions — split into smaller files instead
- Don't use `DateTime.Now` — use `TimeProvider` injected via DI
- Don't use `string.IsNullOrEmpty` in new code — use `string.IsNullOrWhiteSpace` or `is null or ""`
- Don't use `object` as a parameter type — use generics or specific types
- Don't suppress nullable warnings with `!` unless absolutely necessary — fix nullability properly
- Don't use `static` classes for things that need testing — inject abstractions
- Don't comment what code does — rename it instead. Comments explain *why*, not *what*
- Don't use `#pragma warning disable` without a comment explaining why

## Examples

```csharp
// GOOD — primary constructor DI, file-scoped namespace, record command, pattern matching
namespace MyApp.Orders.Application;

public record CreateOrderCommand(Guid CustomerId, decimal Amount) : IRequest<Result<Guid>>;

internal sealed class CreateOrderHandler(AppDbContext db, ILogger<CreateOrderHandler> logger)
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        if (cmd is { Amount: <= 0 }) return OrderErrors.InvalidAmount; // pattern matching
        List<string> tags = ["order", "pending"];                       // collection expression
        // ...
    }
}

// BAD — constructor injection via field assignment, class-scoped namespace, no record
namespace MyApp.Orders.Application {
    public class CreateOrderCommand { public Guid CustomerId { get; set; } } // not a record
    public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
    {
        private readonly AppDbContext _db; // old-style field injection
        public CreateOrderHandler(AppDbContext db) { _db = db; }
    }
}
```
