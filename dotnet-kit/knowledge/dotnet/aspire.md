# .NET Aspire — AppHost & Orchestration Reference

## Overview

.NET Aspire is a stack for building observable, production-ready distributed applications. The `AppHost` project wires together services, databases, message brokers, and caches; service discovery is automatic; the developer dashboard gives live traces, logs, and metrics without any configuration. Aspire does not replace your deployment infrastructure — it generates manifest files consumed by tools like the Aspire Azure Developer CLI (`azd`) for cloud deployment.

## Setup: AppHost Project

The AppHost is a regular .NET console project that references all participating services.

```csharp
// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// ── Infrastructure ─────────────────────────────────────────────────────────

// SQL Server container — Aspire pulls the image and manages the container lifecycle
var sqlServer = builder.AddSqlServer("sql")
    // Persist data between runs so migrations survive a dev restart
    .WithDataVolume("myapp-sql-data")
    // Launch SQL Server Management Studio alongside — useful during dev
    .WithSqlCmdScripting();

var appDb = sqlServer.AddDatabase("appdb");

// Redis — used by HybridCache and output caching
var redis = builder.AddRedis("redis")
    .WithDataVolume("myapp-redis-data");

// RabbitMQ — message broker for Wolverine/MassTransit
var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()    // enables the http://localhost:15672 management UI
    .WithDataVolume("myapp-rabbitmq-data");

// ── Application Services ──────────────────────────────────────────────────

// API project — Aspire injects service discovery env vars automatically
var api = builder.AddProject<Projects.MyApp_Api>("api")
    // References wire up connection strings and service URLs as environment variables
    .WithReference(appDb)
    .WithReference(redis)
    .WithReference(rabbitMq)
    // Wait for dependencies to be healthy before starting the API
    .WaitFor(appDb)
    .WaitFor(redis)
    .WaitFor(rabbitMq);

// Worker service — separate process for background message processing
builder.AddProject<Projects.MyApp_Worker>("worker")
    .WithReference(appDb)
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq);

// ── External Services (no container) ─────────────────────────────────────

// External HTTP service — Aspire registers the URL for service discovery
builder.AddHttpEndpoint("payment-gateway",
    url: builder.Configuration["PaymentGateway:Url"] ?? "https://sandbox.stripe.com");

builder.Build().Run();
```

## Pattern: Service Discovery in the API

Aspire injects service connection strings and URLs as environment variables. The `IConfiguration` key format follows the Aspire naming convention: `ConnectionStrings:{resource-name}`.

```csharp
// Infrastructure/DependencyInjection.cs in MyApp.Api
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration config)
{
    // Aspire injects: ConnectionStrings__appdb=Server=...;Database=appdb;...
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(config.GetConnectionString("appdb")));

    // Aspire injects: ConnectionStrings__redis=localhost:6379
    services.AddStackExchangeRedisCache(options =>
        options.Configuration = config.GetConnectionString("redis"));

    // Aspire injects: ConnectionStrings__rabbitmq=amqp://guest:guest@localhost:5672/
    builder.Host.UseWolverine(opts =>
        opts.UseRabbitMq(new Uri(config.GetConnectionString("rabbitmq")!)));

    return services;
}
```

## Pattern: Service-to-Service HTTP Calls via Aspire Discovery

Aspire registers service endpoints in the discovery service. Use the `http://{service-name}` scheme — Aspire resolves the real address at runtime.

```csharp
// Infrastructure/DependencyInjection.cs
services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
{
    // "inventory" matches the resource name in AppHost — Aspire resolves the real port
    client.BaseAddress = new Uri("http://inventory");
})
.AddStandardResilienceHandler();   // Polly resilience on top of service-discovered client

// Program.cs — enable service discovery resolution for HttpClient
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(defaults =>
    defaults.AddServiceDiscovery());
```

## Pattern: Resource Configuration with Parameters

Use parameters for secrets and environment-specific values — never hardcode.

```csharp
// AppHost/Program.cs
// Parameters can be secret (masked in dashboard) or plain
var jwtSecretParam = builder.AddParameter("jwt-secret", secret: true);
var sqlPasswordParam = builder.AddParameter("sql-password", secret: true);

var sqlServer = builder.AddSqlServer("sql", password: sqlPasswordParam)
    .WithDataVolume("myapp-sql-data");

var api = builder.AddProject<Projects.MyApp_Api>("api")
    .WithReference(sqlServer.AddDatabase("appdb"))
    .WithEnvironment("Auth__JwtSecret", jwtSecretParam);  // injected as env var

// Parameters are supplied via:
// - user-secrets on the AppHost project during development
// - azd environment variables during cloud deployment
// - never in appsettings.json checked into source control
```

## Pattern: Health Checks Visible in the Dashboard

Aspire's dashboard shows resource health. Add health checks to your services so the dashboard reflects real readiness rather than just process status.

```csharp
// Infrastructure/DependencyInjection.cs
services.AddHealthChecks()
    // DB health: runs a SELECT 1 — fails fast if EF Core can't connect
    .AddDbContextCheck<AppDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "sql"])
    // Redis health: PING command
    .AddRedis(
        config.GetConnectionString("redis")!,
        name: "redis",
        failureStatus: HealthStatus.Degraded)
    // RabbitMQ: checks broker connection
    .AddRabbitMQ(
        config.GetConnectionString("rabbitmq")!,
        name: "rabbitmq",
        failureStatus: HealthStatus.Degraded);

// Program.cs — map health endpoints (Aspire polls /health and /alive)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    Predicate      = _ => true   // include all checks in /health
});

app.MapHealthChecks("/alive", new HealthCheckOptions
{
    // /alive is a liveness probe — only fail if the process itself is broken
    Predicate = hc => hc.Tags.Contains("live")
});
```

## Pattern: Aspire Manifest for Production Deployment

Generate the deployment manifest to hand off to `azd` or a custom deployment pipeline.

```bash
# Generate manifest — describes all resources in a deployment-tool-agnostic JSON format
dotnet run --project AppHost -- --publisher manifest --output-path ./aspire-manifest.json

# Deploy to Azure Container Apps using the Azure Developer CLI
azd init --from-code     # infers project structure from the AppHost
azd up                   # provisions infrastructure + deploys all containers
```

## Anti-patterns

### Don't hardcode ports or connection strings in service projects

```csharp
// BAD — hardcoded Redis port breaks when Aspire assigns a different ephemeral port
services.AddStackExchangeRedisCache(options =>
    options.Configuration = "localhost:6379");  // wrong port in CI or when Redis is remote

// GOOD — read from IConfiguration; Aspire injects the correct value at runtime
services.AddStackExchangeRedisCache(options =>
    options.Configuration = config.GetConnectionString("redis"));
```

### Don't reference AppHost projects from service projects

```csharp
// BAD — creates a circular dependency; service projects must not know about the AppHost
// In MyApp.Api.csproj:
<ProjectReference Include="../AppHost/AppHost.csproj" />  // circular!

// GOOD — only the AppHost references service projects, never the reverse
// In AppHost.csproj:
<ProjectReference Include="../src/MyApp.Api/MyApp.Api.csproj" />
<ProjectReference Include="../src/MyApp.Worker/MyApp.Worker.csproj" />
```

### Don't use Aspire's managed containers in production

```csharp
// BAD — Aspire's AddSqlServer/AddRedis containers are for development only;
//       using them in production bypasses HA, backups, and managed service features

// GOOD — use AddConnectionString in production to point to managed cloud resources;
//         the Aspire manifest handles this distinction automatically:
if (builder.Environment.IsProduction())
{
    // In production, override with a real connection string from Key Vault / environment
    builder.AddConnectionString("appdb");   // reads ConnectionStrings__appdb from env
}
else
{
    builder.AddSqlServer("appdb").WithDataVolume(...);
}
```

## Reference

**NuGet Packages:**
```
Aspire.Hosting                              9.0.*   (AppHost project)
Aspire.Hosting.AppHost                      9.0.*
Aspire.Hosting.SqlServer                    9.0.*
Aspire.Hosting.Redis                        9.0.*
Aspire.Hosting.RabbitMQ                     9.0.*
Aspire.Microsoft.EntityFrameworkCore.SqlServer  9.0.*  (service project)
Aspire.StackExchange.Redis                  9.0.*   (service project)
Microsoft.Extensions.ServiceDiscovery       9.0.*   (service project)
AspNetCore.HealthChecks.Redis               8.*
AspNetCore.HealthChecks.RabbitMQ            8.*
```

**AppHost project file additions:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="Aspire.AppHost.Sdk" Version="9.0.*" />
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <IsAspireHost>true</IsAspireHost>
  </PropertyGroup>
</Project>
```
