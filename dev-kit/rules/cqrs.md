# Rule: CQRS with MediatR

## DO
- Route **all business operations** through MediatR — no direct service calls from endpoints
- Commands implement `IRequest<Result<T>>` or `IRequest<Result>` — they mutate state
- Queries implement `IRequest<Result<T>>` — they read state, never mutate
- Handlers are always `internal sealed class`
- One handler per file, named `{FeatureName}Handler.cs`
- Use `IPipelineBehavior<,>` for cross-cutting: ValidationBehavior → LoggingBehavior → TransactionBehavior (commands only)
- Pass `CancellationToken ct` (short name) through the entire call chain
- Use marker interfaces `ICommand<T>` / `IQuery<T>` to scope behaviors to only commands or queries

## DON'T
- Don't call handlers from other handlers — compose via domain logic or publish domain events
- Don't return domain entities from handlers — always map to a DTO/response record
- Don't put validation inside handlers — use `AbstractValidator<TCommand>` in ValidationBehavior
- Don't use `IMediator` — use `ISender` for commands/queries and `IPublisher` for events
- Don't mix read and write in one handler — if you need to return data after a command, return just the ID, then let the client query

## Example

```csharp
// GOOD — internal sealed handler, ISender in endpoint, CancellationToken ct
internal sealed class CreateOrderHandler(AppDbContext db)
    : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
        => (await db.Orders.AddAsync(Order.Create(...), ct)).Entity.Id.Value;
}

// BAD — IMediator instead of ISender, handler calls another handler directly
public class OrderService(IMediator mediator) {
    public async Task Process() {
        await mediator.Send(new CreateOrderCommand(...)); // use ISender
        await mediator.Send(new SendEmailCommand(...));   // don't chain handlers — publish domain event
    }
}
```

## Deep Reference
For full patterns and code examples: @~/.claude/knowledge/dotnet/cqrs-mediatr.md
