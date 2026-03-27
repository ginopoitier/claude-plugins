# Rule: Result Pattern

## DO
- Return `Result<T>` or `Result` from **all handlers** — never throw for business failures
- Define errors as `static readonly` fields on `{Entity}Errors` static classes
- Use `ErrorType` enum: `Failure | NotFound | Validation | Conflict | Unauthorized`
- Map `Result` to `ProblemDetails` at the endpoint layer using `ToProblemDetails()` extension
- Use implicit conversions: `return CustomerErrors.NotFound;` instead of `return Result<T>.Failure(...)`
- Keep error codes namespaced: `"Order.NotFound"`, `"Customer.AlreadyExists"`

## DON'T
- Don't throw exceptions for **expected** failures: not found, conflict, validation, unauthorized
- Don't return raw error strings from endpoints — always `ProblemDetails`
- Don't swallow failures — always propagate the error up to the endpoint
- Don't catch `Result` failures silently in pipeline behaviors (except ValidationBehavior)
- Don't create a `Result` failure without an `Error` — always include code + description

## HTTP Mapping
```
NotFound     → 404
Validation   → 422
Conflict     → 409
Unauthorized → 401
Failure      → 500
```

## Deep Reference
For full implementation: @~/.claude/knowledge/dotnet/result-pattern.md
