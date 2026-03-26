using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace DevKit.Mcp.Services;

public sealed class Neo4jService(IConfiguration config, ILogger<Neo4jService> logger) : IAsyncDisposable
{
    private IDriver? _driver;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(config["Neo4j:Uri"]);

    public async Task<IDriver> GetDriverAsync(CancellationToken ct = default)
    {
        if (_driver is not null) return _driver;

        await _lock.WaitAsync(ct);
        try
        {
            if (_driver is not null) return _driver;

            var uri = config["Neo4j:Uri"]
                ?? throw new InvalidOperationException("Neo4j:Uri is not configured. Set it via --Neo4j:Uri or environment variable.");
            var user = config["Neo4j:Username"] ?? "neo4j";
            var pass = config["Neo4j:Password"] ?? "password";

            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, pass));
            logger.LogInformation("Neo4j driver created for {Uri}", uri);
            return _driver;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_driver is not null)
            await _driver.DisposeAsync();
        _lock.Dispose();
    }
}
