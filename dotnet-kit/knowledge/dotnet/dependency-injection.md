# Dependency Injection — .NET 9 Reference

## Overview

.NET 9's built-in DI container covers 95% of real-world scenarios: transient, scoped, and singleton lifetimes, open-generic registrations, and keyed services. Scrutor extends it with assembly scanning and the decorator pattern without introducing a third-party container. Reach for a third-party container (Autofac, DryIoc) only when you need interceptors or convention-based child containers.

## Setup: Infrastructure Registration Scaffold

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration config)
{
    // Register all repositories by convention via Scrutor
    services.Scan(scan => scan
        .FromAssemblyOf<InfrastructureAssemblyMarker>()
        .AddClasses(classes => classes.AssignableTo(typeof(IRepository<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        .AddClasses(classes => classes.AssignableTo<IDomainService>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());

    return services;
}
```

## Pattern: Keyed Services (.NET 8+)

Keyed services let you register multiple implementations of the same interface and resolve them by a key — eliminating the factory-dictionary pattern that was previously required.

```csharp
// Domain/Notifications/INotificationSender.cs
public interface INotificationSender
{
    Task SendAsync(string recipient, string message, CancellationToken ct);
}

// Infrastructure/Notifications/EmailNotificationSender.cs
public sealed class EmailNotificationSender(IEmailClient email) : INotificationSender
{
    public async Task SendAsync(string recipient, string message, CancellationToken ct) =>
        await email.SendAsync(recipient, "Notification", message, ct);
}

// Infrastructure/Notifications/SmsNotificationSender.cs
public sealed class SmsNotificationSender(ISmsClient sms) : INotificationSender
{
    public async Task SendAsync(string recipient, string message, CancellationToken ct) =>
        await sms.SendTextAsync(recipient, message, ct);
}

// Infrastructure/Notifications/PushNotificationSender.cs
public sealed class PushNotificationSender(IPushClient push) : INotificationSender
{
    public async Task SendAsync(string recipient, string message, CancellationToken ct) =>
        await push.SendPushAsync(recipient, message, ct);
}

// Registration — string keys, but enums work too
services.AddKeyedScoped<INotificationSender, EmailNotificationSender>("email");
services.AddKeyedScoped<INotificationSender, SmsNotificationSender>("sms");
services.AddKeyedScoped<INotificationSender, PushNotificationSender>("push");

// ── Resolution ────────────────────────────────────────────────────────────

// Option 1: inject a specific keyed implementation via [FromKeyedServices]
public sealed class OrderConfirmationHandler(
    [FromKeyedServices("email")] INotificationSender emailSender)
    : IRequestHandler<SendOrderConfirmationCommand, Result>
{
    public async Task<Result> Handle(SendOrderConfirmationCommand cmd, CancellationToken ct)
    {
        await emailSender.SendAsync(cmd.CustomerEmail, $"Order {cmd.OrderId} confirmed", ct);
        return Result.Success();
    }
}

// Option 2: resolve at runtime based on user preference stored in the database
public sealed class NotificationDispatcher(IServiceProvider sp) : INotificationDispatcher
{
    public Task DispatchAsync(
        string channel,           // "email" | "sms" | "push" — from user preferences
        string recipient,
        string message,
        CancellationToken ct)
    {
        // Keyed resolution from IServiceProvider — no switch statement required
        var sender = sp.GetRequiredKeyedService<INotificationSender>(channel);
        return sender.SendAsync(recipient, message, ct);
    }
}
```

## Pattern: Decorator Pattern with Scrutor

The decorator pattern wraps an existing service implementation to add cross-cutting concerns (caching, logging, validation) without modifying the original class. Scrutor's `Decorate` extension makes this clean.

```csharp
// Application/Abstractions/IProductRepository.cs
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct);
}

// Infrastructure/Repositories/ProductRepository.cs  — the real implementation
public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct) =>
        db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct) =>
        await db.Products.AsNoTracking().ToListAsync(ct);
}

// Infrastructure/Repositories/CachedProductRepository.cs — the decorator
// Does NOT depend on ProductRepository directly — it wraps IProductRepository
public sealed class CachedProductRepository(
    IProductRepository inner,   // inner is the original ProductRepository
    HybridCache cache)
    : IProductRepository
{
    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct) =>
        cache.GetOrCreateAsync(
            $"product:{id.Value}",
            innerCt => inner.GetByIdAsync(id, innerCt),  // delegates to the real repo on miss
            options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) },
            cancellationToken: ct);

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct) =>
        cache.GetOrCreateAsync(
            "products:all",
            innerCt => inner.GetAllAsync(innerCt),
            options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
            cancellationToken: ct);
}

// Registration — order matters: register the concrete first, then decorate
services.AddScoped<IProductRepository, ProductRepository>();
// Scrutor replaces the registration with: CachedProductRepository(ProductRepository)
services.Decorate<IProductRepository, CachedProductRepository>();
```

## Pattern: Factory Pattern for Runtime Construction

Use a factory when the implementation cannot be determined at registration time and depends on runtime data.

```csharp
// Domain/Reports/IReportGenerator.cs
public interface IReportGenerator
{
    Task<byte[]> GenerateAsync(ReportRequest request, CancellationToken ct);
}

// Infrastructure/Reports/IReportGeneratorFactory.cs
public interface IReportGeneratorFactory
{
    IReportGenerator Create(ReportFormat format);
}

// Infrastructure/Reports/ReportGeneratorFactory.cs
public sealed class ReportGeneratorFactory(IServiceProvider sp) : IReportGeneratorFactory
{
    public IReportGenerator Create(ReportFormat format) =>
        // KeyedService resolution: implementations registered with format as key
        sp.GetRequiredKeyedService<IReportGenerator>(format);
}

// Registration
services.AddKeyedScoped<IReportGenerator, PdfReportGenerator>(ReportFormat.Pdf);
services.AddKeyedScoped<IReportGenerator, ExcelReportGenerator>(ReportFormat.Excel);
services.AddKeyedScoped<IReportGenerator, CsvReportGenerator>(ReportFormat.Csv);
services.AddScoped<IReportGeneratorFactory, ReportGeneratorFactory>();

// Usage in a handler
public sealed class GenerateReportHandler(IReportGeneratorFactory factory)
    : IRequestHandler<GenerateReportCommand, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(GenerateReportCommand cmd, CancellationToken ct)
    {
        var generator = factory.Create(cmd.Format);
        var bytes = await generator.GenerateAsync(
            new ReportRequest(cmd.DateFrom, cmd.DateTo, cmd.Filters), ct);
        return bytes;
    }
}
```

## Pattern: Open-Generic Registrations

Register a single implementation that handles any generic type parameter — avoids registering every concrete variant manually.

```csharp
// Application/Abstractions/IRepository.cs
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    void Add(T entity);
    void Remove(T entity);
}

// Infrastructure/Repositories/Repository.cs — works for any Entity subtype
public sealed class Repository<T>(AppDbContext db) : IRepository<T>
    where T : Entity
{
    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct) =>
        // EF Core's Find uses the primary key convention — works for all entity types
        db.Set<T>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public void Add(T entity) => db.Set<T>().Add(entity);

    public void Remove(T entity) => db.Set<T>().Remove(entity);
}

// Single open-generic registration covers IRepository<Order>, IRepository<Product>, etc.
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Usage — no additional registration needed for new entity types
public sealed class GetOrderHandler(IRepository<Order> orders)
    : IRequestHandler<GetOrderQuery, Result<OrderResponse>>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderQuery query, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(query.OrderId, ct);
        return order is null ? OrderErrors.NotFound : order.ToResponse();
    }
}
```

## Pattern: Scrutor Assembly Scanning

Scrutor's `Scan` method replaces hundreds of manual `AddScoped<IFoo, Foo>()` registrations.

```csharp
// Infrastructure/DependencyInjection.cs
services.Scan(scan => scan
    .FromAssemblyOf<InfrastructureAssemblyMarker>()

    // Pattern 1: register all IRepository<T> implementations as scoped
    .AddClasses(classes => classes
        .AssignableTo(typeof(IRepository<>))
        .Where(t => !t.IsGenericTypeDefinition))  // skip the open-generic base
        .AsImplementedInterfaces()
        .WithScopedLifetime()

    // Pattern 2: register domain services by interface
    .AddClasses(classes => classes
        .InNamespaces("MyApp.Infrastructure.Services"))
        .AsMatchingInterface()   // EmailService → IEmailService
        .WithScopedLifetime()

    // Pattern 3: register all integration event handlers as themselves (Wolverine convention)
    .AddClasses(classes => classes
        .AssignableTo<IIntegrationEventHandler>())
        .AsSelf()
        .WithTransientLifetime());
```

## Anti-patterns

### Don't inject IServiceProvider to resolve dependencies in constructors

```csharp
// BAD — service locator pattern hides dependencies; makes testing harder;
//       violates the Explicit Dependencies Principle
public sealed class OrderService(IServiceProvider sp)
{
    public async Task ProcessAsync(Order order)
    {
        // Tests must set up the entire container just to test this one method
        var repo     = sp.GetRequiredService<IOrderRepository>();
        var email    = sp.GetRequiredService<IEmailService>();
        await repo.SaveAsync(order);
        await email.SendAsync(order.CustomerEmail, "Confirmed");
    }
}

// GOOD — declare dependencies explicitly in the constructor
public sealed class OrderService(
    IOrderRepository repo,
    IEmailService email)
{
    public async Task ProcessAsync(Order order, CancellationToken ct)
    {
        await repo.SaveAsync(order, ct);
        await email.SendAsync(order.CustomerEmail, "Confirmed", ct);
    }
}
```

### Don't capture scoped services in singletons

```csharp
// BAD — AppDbContext is scoped (one per request); injecting it into a singleton
//       causes it to be reused across requests, leading to concurrency bugs and
//       stale tracked entities from previous requests
services.AddSingleton<IProductCache>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>(); // scoped inside singleton!
    return new ProductCache(db);
});

// GOOD — inject IServiceScopeFactory into the singleton and create a scope per operation
services.AddSingleton<IProductCache>(sp =>
    new ProductCache(sp.GetRequiredService<IServiceScopeFactory>()));

public sealed class ProductCache(IServiceScopeFactory scopeFactory) : IProductCache
{
    public async Task<Product?> GetAsync(ProductId id, CancellationToken ct)
    {
        // New scope = new DbContext instance — no cross-request contamination
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    }
}
```

### Don't register the same service multiple times with conflicting lifetimes

```csharp
// BAD — registering the same interface twice means only the last registration wins;
//       the first registration is silently ignored
services.AddScoped<IEmailService, SmtpEmailService>();
services.AddScoped<IEmailService, SendGridEmailService>();  // overwrites the first!

// GOOD — if you need multiple implementations, use keyed services or a collection
services.AddKeyedScoped<IEmailService, SmtpEmailService>("smtp");
services.AddKeyedScoped<IEmailService, SendGridEmailService>("sendgrid");

// Or if you want all implementations resolved together:
services.AddScoped<IEmailService, SmtpEmailService>();
services.AddScoped<IEmailService, SendGridEmailService>();
// Inject IEnumerable<IEmailService> to get both
public sealed class CompositeEmailService(IEnumerable<IEmailService> senders) : IEmailService { ... }
```

## Reference

**NuGet Packages:**
```
Scrutor                    4.*
Microsoft.Extensions.DependencyInjection   9.0.*   (inbox — no NuGet needed in .NET 9)
```

**Lifetime quick reference:**

| Lifetime | Scope | Use For |
|---|---|---|
| `Transient` | New instance every resolution | Stateless, lightweight utilities |
| `Scoped` | One per HTTP request / unit of work | DbContext, repositories, handlers |
| `Singleton` | One per application lifetime | Caches, HttpClient factories, configuration |
