# Entity Framework Core Patterns — .NET Reference

## DbContext Setup

```csharp
// Infrastructure/Persistence/AppDbContext.cs
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Dispatch domain events before saving
        var events = ChangeTracker.Entries<Entity>()
            .SelectMany(e => e.Entity.PopDomainEvents())
            .ToList();

        var result = await base.SaveChangesAsync(ct);

        // Publish after commit
        foreach (var domainEvent in events)
            await _publisher.Publish(domainEvent, ct); // inject IPublisher

        return result;
    }
}
```

## Entity Type Configuration

```csharp
// Infrastructure/Persistence/Configurations/OrderConfiguration.cs
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        // Strongly typed ID conversion
        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => new OrderId(value));

        builder.Property(o => o.CustomerId)
            .HasConversion(id => id.Value, value => new CustomerId(value));

        // Value object as owned entity
        builder.OwnsOne(o => o.TotalAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
        });

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.ToTable("Orders");
    }
}
```

## Infrastructure DI Registration

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(config.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

    return services;
}
```

## Query Patterns in Handlers

```csharp
// Always use AsNoTracking() for queries
var product = await db.Products
    .AsNoTracking()
    .Where(p => p.Id == new ProductId(request.ProductId) && !p.IsDeleted)
    .Select(p => new ProductResponse(p.Id.Value, p.Name, p.Price.Amount))
    .FirstOrDefaultAsync(ct);

// Pagination
var page = await db.Orders
    .AsNoTracking()
    .Where(o => o.CustomerId == new CustomerId(request.CustomerId))
    .OrderByDescending(o => o.CreatedAt)
    .Skip((request.Page - 1) * request.PageSize)
    .Take(request.PageSize)
    .Select(o => new OrderSummary(o.Id.Value, o.Status.ToString(), o.TotalAmount.Amount))
    .ToListAsync(ct);
```

## Soft Delete Pattern

```csharp
// Domain/Shared/Entity.cs
public abstract class Entity
{
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}

// Apply global query filter in DbContext
builder.HasQueryFilter(e => !e.IsDeleted);
```

## Migrations

```bash
# Always use --project and --startup-project flags
dotnet ef migrations add <Name> --project src/YourApp.Infrastructure --startup-project src/YourApp.Api
dotnet ef database update --project src/YourApp.Infrastructure --startup-project src/YourApp.Api
```

## Interceptors for Audit Trail

```csharp
// Infrastructure/Persistence/Interceptors/AuditInterceptor.cs
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        var db = eventData.Context;
        if (db is null) return base.SavingChangesAsync(eventData, result, ct);

        var now = DateTime.UtcNow;
        foreach (var entry in db.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }
}
```

## SQL Server Connection String Template

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyAppDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```
