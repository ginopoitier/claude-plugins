# PR Review Patterns

> Patterns used by the `/review` skill for tech lead code review on GitHub PRs.

## Review Modes

### Mentoring Mode (`--mentoring`)
For junior developers or exploratory branches. Goal: teach, not just gate.

- Explain *why* a pattern is preferred, not just that it's wrong
- Acknowledge what was done well before listing issues
- Suggest resources or examples from the existing codebase
- Use constructive language: "Consider using..." not "This is wrong"
- Still block on security issues and architecture violations

### Gatekeeper Mode (`--gatekeeper`)
For release branches, hotfixes, critical paths. Goal: strict quality gate.

- Terse: state the problem and the fix, no elaboration
- Verdict at the top: `✅ APPROVED` / `⚠️ APPROVED WITH SUGGESTIONS` / `🚫 BLOCKED`
- Every blocker listed before warnings
- No praise — only signal

---

## Severity Levels

| Level | Label | Meaning |
|-------|-------|---------|
| 🚫 Blocker | `BLOCK` | Must be fixed before merge — security, broken logic, architecture violation |
| ⚠️ Warning | `WARN` | Should be fixed — code smell, test gap, naming issue |
| 💡 Suggestion | `SUGGEST` | Nice to have — refactoring opportunity, performance improvement |
| ℹ️ Info | `INFO` | FYI — context, link to related pattern, no action needed |

---

## Review Checklist

### 1. Security
- [ ] No secrets or credentials in code or commit history
- [ ] User input is validated and sanitized
- [ ] Authorization checks present on all protected operations
- [ ] No SQL injection vectors (parameterized queries)
- [ ] No XSS vectors (HTML encoding in outputs)
- [ ] Dependencies are not known-vulnerable

### 2. Architecture
- [ ] Change is in the correct layer (domain logic not in controllers/API layer)
- [ ] No cross-module direct references (use interfaces / mediator)
- [ ] Result pattern used for operation outcomes (not exceptions for control flow)
- [ ] New patterns are consistent with existing codebase patterns

### 3. Business Logic
- [ ] Happy path implemented correctly
- [ ] Error paths handled (NotFound, Validation, Conflict, Unauthorized)
- [ ] Edge cases considered (empty lists, null refs, concurrent access)
- [ ] Acceptance criteria from linked Jira ticket / issue met

### 4. Tests
- [ ] New business logic has tests
- [ ] Tests cover at least one error path per handler/service
- [ ] Tests use real infrastructure (Testcontainers) not mocks for DB
- [ ] No fragile test implementation (testing internals, not behavior)

### 5. Code Quality
- [ ] No obvious code duplication
- [ ] Methods are focused (single responsibility, reasonable length)
- [ ] Names are clear and consistent with project conventions
- [ ] No dead code (commented-out blocks, unused variables)

### 6. Logging and Observability
- [ ] Important operations are logged (Info level)
- [ ] Errors are logged with context (Error level with exception)
- [ ] No sensitive data in log messages (PII, credentials)
- [ ] Structured logging used (no string interpolation in Serilog messages)

---

## Diff Analysis Patterns

### Gather the diff

```bash
# For a PR by number (preferred)
gh pr diff {number}

# For staged changes
git diff HEAD

# For a branch vs main
git diff main...{branch}
```

### Extract Jira ticket from branch

```bash
# Branch: feature/ORD-456-order-validation
BRANCH=$(git branch --show-current)
JIRA_KEY=$(echo "$BRANCH" | grep -oE '[A-Z]+-[0-9]+' | head -1)

# If found, load ticket acceptance criteria:
# mcp__atlassian__jira_get_issue("$JIRA_KEY")
```

### SDLC Compliance Check

If `SDLC_CONFLUENCE_SPACE` is configured:
1. Search Confluence for PR process requirements: `mcp__atlassian__confluence_search`
2. Verify the PR meets documented review criteria
3. Flag any SDLC violations as blockers

---

## Review Output Templates

### Gatekeeper Output

```
## Review: feat/order-validation → main

**Verdict:** 🚫 BLOCKED

### Blockers
1. **BLOCK** `src/Orders/CreateOrderHandler.cs:45` — Raw SQL string concatenation. Use EF Core or parameterized queries.
2. **BLOCK** `src/Orders/CreateOrderHandler.cs:72` — No authorization check. Add `[Authorize]` or check `_currentUser.HasPermission()`.

### Warnings
1. **WARN** `tests/Orders.Tests/CreateOrderTests.cs` — No test for invalid CustomerId. Add at minimum one validation failure test.

### Suggestions
1. **SUGGEST** `src/Orders/CreateOrderValidator.cs:12` — Duplicate validation logic from `UpdateOrderValidator`. Extract to `OrderValidationRules`.

Fix blockers and re-request review.
```

### Mentoring Output

```
## Review: feat/order-validation → main

Great start on the validation layer! Here's what I'd suggest before merging:

### 🚫 Must fix before merge

**SQL Injection risk** (`CreateOrderHandler.cs:45`)
You're building a SQL string by concatenating user input:
```csharp
// Current — dangerous
var sql = $"SELECT * FROM Orders WHERE CustomerId = '{customerId}'";
```
This allows SQL injection. Instead, use EF Core's LINQ:
```csharp
// Safe — parameterized automatically
var order = await _db.Orders
    .Where(o => o.CustomerId == customerId)
    .FirstOrDefaultAsync();
```
See `src/Products/GetProductHandler.cs` for an example of this pattern in the codebase.

### ⚠️ Suggested improvements

**Missing validation failure test** (`CreateOrderTests.cs`)
The tests cover the happy path but don't test what happens when `CustomerId` is empty or null. Adding this test prevents regressions and documents the expected behavior:
```csharp
[Fact]
public async Task CreateOrder_WithEmptyCustomerId_Returns400()
{
    var response = await _client.PostAsJsonAsync("/api/orders",
        new CreateOrderRequest("", []));
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
```
```

---

## Common Anti-patterns to Flag

| Anti-pattern | Severity | File pattern |
|-------------|----------|-------------|
| `string.Format` / interpolation in SQL | 🚫 Blocker | `*Handler.cs`, `*Repository.cs` |
| `catch (Exception e) {}` (swallowed) | 🚫 Blocker | Any |
| Business logic in controller | ⚠️ Warning | `*Controller.cs`, `*Endpoint.cs` |
| `async void` (not event handler) | 🚫 Blocker | Any |
| Missing `.ConfigureAwait(false)` in libraries | 💡 Suggestion | Non-ASP.NET projects |
| `DateTime.Now` instead of `TimeProvider` | ⚠️ Warning | Any |
| `Console.WriteLine` in production code | ⚠️ Warning | Any (not tests) |
| Returning `null` instead of `Result.Failure` | ⚠️ Warning | Handler/Service |
| N+1 query (missing `Include` or projection) | ⚠️ Warning | `*Handler.cs` with EF |
| Hardcoded connection strings / secrets | 🚫 Blocker | Any |
