using System.ComponentModel;
using System.Text.Json;
using DevKit.Mcp.Services;
using ModelContextProtocol.Server;
using Neo4j.Driver;

namespace DevKit.Mcp.Tools.Neo4j;

[McpServerToolType]
public sealed class Neo4jTools(Neo4jService neo4j)
{
    [McpServerTool, Description(
        "Runs a Cypher query against Neo4j and returns results as JSON. " +
        "Always use parameterized queries — pass values in the params argument, never interpolate into the query string.")]
    public async Task<string> RunQuery(
        [Description("Cypher query to execute. Use $paramName for parameters.")] string query,
        [Description("JSON object of query parameters, e.g. {\"userId\": \"abc-123\", \"limit\": 10}. Optional.")] string? parameters = null,
        CancellationToken ct = default)
    {
        if (!neo4j.IsConfigured)
            return "Neo4j is not configured. Set Neo4j:Uri, Neo4j:Username, Neo4j:Password.";

        var driver = await neo4j.GetDriverAsync(ct);

        var queryParams = parameters is not null
            ? JsonSerializer.Deserialize<Dictionary<string, object?>>(parameters) ?? []
            : new Dictionary<string, object?>();

        await using var session = driver.AsyncSession();
        var result = await session.RunAsync(query, queryParams);
        var records = await result.ToListAsync(ct);

        var rows = records.Select(r =>
            r.Keys.ToDictionary(k => k, k => r[k].As<object?>()));

        return JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description(
        "Returns the Neo4j graph schema — all node labels, relationship types, and their properties. " +
        "Use this before writing Cypher to understand the data model.")]
    public async Task<string> GetSchema(CancellationToken ct = default)
    {
        if (!neo4j.IsConfigured)
            return "Neo4j is not configured. Set Neo4j:Uri, Neo4j:Username, Neo4j:Password.";

        var driver = await neo4j.GetDriverAsync(ct);
        await using var session = driver.AsyncSession();

        // Get node labels with property keys
        var labelsResult = await session.RunAsync(
            "CALL db.schema.nodeTypeProperties() YIELD nodeType, propertyName, propertyTypes " +
            "RETURN nodeType, collect({property: propertyName, types: propertyTypes}) AS properties " +
            "ORDER BY nodeType");
        var labels = await labelsResult.ToListAsync(ct);

        // Get relationship types
        var relsResult = await session.RunAsync(
            "CALL db.schema.relTypeProperties() YIELD relType, propertyName " +
            "RETURN relType, collect(propertyName) AS properties " +
            "ORDER BY relType");
        var rels = await relsResult.ToListAsync(ct);

        var schema = new
        {
            NodeLabels = labels.Select(r => new
            {
                Label = r["nodeType"].As<string>(),
                Properties = r["properties"].As<List<IDictionary<string, object>>>()
                    .Select(p => new { Name = p["property"], Types = p["types"] })
            }),
            RelationshipTypes = rels.Select(r => new
            {
                Type = r["relType"].As<string>(),
                Properties = r["properties"].As<List<string>>()
            })
        };

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description(
        "Finds the shortest path between two nodes in the graph. " +
        "Returns the path as a sequence of nodes and relationships.")]
    public async Task<string> FindPath(
        [Description("Label of the start node, e.g. 'User'.")] string fromLabel,
        [Description("Property to match the start node on, e.g. 'id'.")] string fromProperty,
        [Description("Value to match the start node, e.g. 'abc-123'.")] string fromValue,
        [Description("Label of the end node, e.g. 'Product'.")] string toLabel,
        [Description("Property to match the end node on, e.g. 'id'.")] string toProperty,
        [Description("Value to match the end node, e.g. 'xyz-456'.")] string toValue,
        [Description("Relationship type to traverse, e.g. 'CONNECTED_TO'. Omit for any relationship.")] string? relationshipType = null,
        [Description("Max path length. Defaults to 6.")] int maxHops = 6,
        CancellationToken ct = default)
    {
        if (!neo4j.IsConfigured)
            return "Neo4j is not configured.";

        var driver = await neo4j.GetDriverAsync(ct);
        await using var session = driver.AsyncSession();

        var relPattern = relationshipType is not null
            ? $"[r:{relationshipType}*1..{maxHops}]"
            : $"[r*1..{maxHops}]";

        var query = $$"""
            MATCH (from:{{fromLabel}} {{{fromProperty}}: $fromValue}),
                  (to:{{toLabel}} {{{toProperty}}: $toValue})
            MATCH path = shortestPath((from)-{{relPattern}}-(to))
            RETURN [n IN nodes(path) | labels(n)[0] + ': ' + coalesce(n.name, n.id, 'unknown')] AS nodes,
                   [r IN relationships(path) | type(r)] AS relationships,
                   length(path) AS hops
            """;

        var result = await session.RunAsync(query,
            new { fromValue, toValue });
        var records = await result.ToListAsync(ct);

        if (records.Count == 0)
            return $"No path found between {fromLabel}:{fromValue} and {toLabel}:{toValue} within {maxHops} hops.";

        var path = records[0];
        return JsonSerializer.Serialize(new
        {
            Hops = path["hops"].As<int>(),
            Nodes = path["nodes"].As<List<string>>(),
            Relationships = path["relationships"].As<List<string>>()
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
