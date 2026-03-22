# Testing — .NET Reference

## NuGet Packages

```xml
<!-- Test project -->
<PackageReference Include="xunit" Version="*" />
<PackageReference Include="xunit.runner.visualstudio" Version="*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="*" />
<PackageReference Include="Testcontainers.MsSql" Version="*" />
<PackageReference Include="FluentAssertions" Version="*" />
<PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="*" />
```

## WebApplicationFactory Setup

```csharp
// tests/{App}.Application.Tests/AppFactory.cs
public sealed class AppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync() => await _db.StartAsync();

    public new async Task DisposeAsync() => await _db.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace real DbContext with test container connection
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseSqlServer(_db.GetConnectionString()));

            // Replace time provider for deterministic tests
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider, FakeTimeProvider>();
        });
    }
}
```

## Integration Test Pattern

```csharp
// tests/{App}.Application.Tests/Orders/CreateOrderTests.cs
public class CreateOrderTests(AppFactory factory) : IClassFixture<AppFactory>
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
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>();
        body!.Status.Should().Be("Pending");
        body.TotalAmount.Should().Be(99.99m);
    }

    [Fact]
    public async Task CreateOrder_NegativeAmount_Returns422()
    {
        var request = new { CustomerId = Guid.NewGuid(), Amount = -5m };
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

## Domain Unit Tests (pure logic)

```csharp
public class OrderTests
{
    [Fact]
    public void Cancel_PendingOrder_SetsStatusCancelled()
    {
        // Arrange
        var order = Order.Create(new CustomerId(Guid.NewGuid()), new Money(50m, "EUR"));

        // Act
        var result = order.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyCancelledOrder_ReturnsConflictError()
    {
        var order = Order.Create(new CustomerId(Guid.NewGuid()), new Money(50m, "EUR"));
        order.Cancel();

        var result = order.Cancel();

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}
```

## Test Data Builder Pattern

```csharp
// tests/Shared/Builders/OrderBuilder.cs
public sealed class OrderBuilder
{
    private CustomerId _customerId = new(Guid.NewGuid());
    private Money _amount = new(100m, "EUR");

    public OrderBuilder WithCustomer(Guid customerId)
    {
        _customerId = new CustomerId(customerId);
        return this;
    }

    public OrderBuilder WithAmount(decimal amount)
    {
        _amount = new Money(amount, "EUR");
        return this;
    }

    public Order Build() => Order.Create(_customerId, _amount);
}

// Usage
var order = new OrderBuilder()
    .WithCustomer(knownCustomerId)
    .WithAmount(250m)
    .Build();
```

## FakeTimeProvider Usage

```csharp
// Advance time in tests
var fakeTime = factory.Services.GetRequiredService<TimeProvider>() as FakeTimeProvider;
fakeTime!.Advance(TimeSpan.FromDays(30));

// Assert time-dependent behavior
order.ExpiresAt.Should().BeBefore(fakeTime.GetUtcNow().DateTime);
```

## Naming Convention

```
MethodName_StateUnderTest_ExpectedBehavior
CreateOrder_ValidRequest_ReturnsCreated
Cancel_AlreadyCancelled_ReturnsConflictError
GetById_NonExistentId_Returns404
```

## Test Project Structure

```
tests/
  {App}.Application.Tests/
    AppFactory.cs
    Shared/
      Builders/          # test data builders
      Extensions/        # HttpClient helpers
    {Feature}/
      {FeatureName}Tests.cs
```
