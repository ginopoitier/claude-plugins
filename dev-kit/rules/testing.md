# Rule: Testing

## DO
- Use **xUnit** for all .NET tests
- Prefer **integration tests** over unit tests for handlers — one `WebApplicationFactory` test covers routing, binding, validation, business logic, and persistence together
- Use **real databases** via Testcontainers (SQL Server or SQLite) — not `UseInMemoryDatabase`
- Follow **AAA pattern**: Arrange / Act / Assert with blank lines between sections
- Name tests: `MethodName_StateUnderTest_ExpectedBehavior` or `Given_When_Then`
- Use test data **builder pattern** for complex domain objects
- Test **behavior**, not implementation: assert on the outcome, not which methods were called
- Use `FakeTimeProvider` for time-dependent code — never `DateTime.Now` in tests
- Test error paths explicitly: not found, conflict, validation failure

## DON'T
- Don't mock `DbContext` — use a real test database
- Don't test `private` methods — test public behavior that exercises them
- Don't share mutable state between tests — each test is fully independent
- Don't use `Thread.Sleep` in tests — use `FakeTimeProvider` or async helpers
- Don't assert on log output as a substitute for testing behavior
- Don't write tests that only test the happy path — error cases matter equally

## Example

```csharp
// GOOD — integration test, real DB via Testcontainers, AAA, named correctly
public class CreateOrderTests(AppFactory factory) : IClassFixture<AppFactory>
{
    [Fact]
    public async Task CreateOrder_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new { CustomerId = Guid.NewGuid(), Amount = 99.99m };

        // Act
        var response = await factory.CreateClient().PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateOrder_NegativeAmount_Returns422() { /* error path */ }
}

// BAD — unit test with mocked DbContext, only happy path, poor naming
public class OrderHandlerTests {
    [Fact]
    public async Task Test1() { // bad name
        var db = new Mock<AppDbContext>(); // don't mock DbContext
        // no error path tested
    }
}
```

## Deep Reference
For full testing setup with Testcontainers: @~/.claude/knowledge/dotnet/testing.md
