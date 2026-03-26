# Neo4j Patterns — .NET Reference

## NuGet

```xml
<PackageReference Include="Neo4j.Driver" Version="*" />
```

## Infrastructure Registration

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
{
    // IDriver is singleton — one connection pool per process
    services.AddSingleton<IDriver>(_ =>
        GraphDatabase.Driver(
            config["Neo4j:Uri"],
            AuthTokens.Basic(config["Neo4j:Username"], config["Neo4j:Password"])
        ));

    // IAsyncSession is scoped — one session per request
    services.AddScoped<IAsyncSession>(sp =>
        sp.GetRequiredService<IDriver>().AsyncSession());

    return services;
}
```

## appsettings.json

```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "password"
  }
}
```

## Query Pattern in Handlers

```csharp
internal sealed class GetUserConnectionsHandler(IAsyncSession neo4j)
    : IRequestHandler<GetUserConnectionsQuery, Result<IReadOnlyList<UserConnectionResponse>>>
{
    public async Task<Result<IReadOnlyList<UserConnectionResponse>>> Handle(
        GetUserConnectionsQuery request, CancellationToken ct)
    {
        var query = @"
            MATCH (u:User {id: $userId})-[:CONNECTED_TO]->(connected:User)
            RETURN connected.id AS id,
                   connected.name AS name,
                   connected.email AS email
            ORDER BY connected.name
            LIMIT $limit";

        var result = await neo4j.RunAsync(query, new
        {
            userId = request.UserId.ToString(),
            limit = request.Limit
        });

        var connections = await result.ToListAsync(
            record => new UserConnectionResponse(
                Guid.Parse(record["id"].As<string>()),
                record["name"].As<string>(),
                record["email"].As<string>()
            ), ct);

        return connections;
    }
}
```

## Write Pattern (Command)

```csharp
internal sealed class CreateConnectionHandler(IAsyncSession neo4j, AppDbContext db)
    : IRequestHandler<CreateConnectionCommand, Result>
{
    public async Task<Result> Handle(CreateConnectionCommand request, CancellationToken ct)
    {
        // Verify both users exist in SQL Server first
        var usersExist = await db.Users
            .Where(u => u.Id == new UserId(request.FromUserId) || u.Id == new UserId(request.ToUserId))
            .CountAsync(ct) == 2;

        if (!usersExist)
            return UserErrors.NotFound;

        // Create relationship in Neo4j
        await neo4j.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(@"
                MERGE (a:User {id: $fromId})
                MERGE (b:User {id: $toId})
                MERGE (a)-[:CONNECTED_TO]->(b)",
                new { fromId = request.FromUserId.ToString(), toId = request.ToUserId.ToString() });
        });

        return Result.Success();
    }
}
```

## Node Sync Pattern (Keep Neo4j in Sync with SQL Server)

```csharp
// Domain event handler that syncs user creation to Neo4j
internal sealed class UserCreatedDomainEventHandler(IAsyncSession neo4j)
    : INotificationHandler<UserCreatedDomainEvent>
{
    public async Task Handle(UserCreatedDomainEvent notification, CancellationToken ct)
    {
        await neo4j.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(
                "MERGE (u:User {id: $id}) SET u.name = $name, u.email = $email",
                new
                {
                    id = notification.UserId.ToString(),
                    name = notification.Name,
                    email = notification.Email
                });
        });
    }
}
```

## Graph Data Modeling Rules

- **Nodes** = entities (User, Product, Document, Tag)
- **Relationships** = verbs (CONNECTED_TO, PURCHASED, TAGGED_WITH, AUTHORED)
- Store IDs consistent with SQL Server (Guid as string in Neo4j)
- Sync nodes via domain events after SQL Server commits — Neo4j is the graph projection layer
- Neo4j for: traversals, recommendations, relationship queries, graph analytics
- SQL Server for: CRUD, transactions, source of truth

## Docker (local dev)

```yaml
# docker-compose.yml
services:
  neo4j:
    image: neo4j:latest
    environment:
      - NEO4J_AUTH=neo4j/password
    ports:
      - "7474:7474"   # browser
      - "7687:7687"   # bolt
    volumes:
      - neo4j-data:/data

volumes:
  neo4j-data:
```

Browser: http://localhost:7474
