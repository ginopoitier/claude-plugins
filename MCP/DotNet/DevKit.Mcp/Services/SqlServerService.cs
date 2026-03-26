using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DevKit.Mcp.Services;

public sealed class SqlServerService : IAsyncDisposable
{
    private readonly string? _connectionString;
    private SqlConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public SqlServerService(IConfiguration configuration)
    {
        _connectionString = configuration["SqlServer:ConnectionString"]
            ?? configuration.GetConnectionString("SqlServer");
    }

    public async Task<SqlConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "SQL Server connection string not configured. Set 'SqlServer:ConnectionString' or 'ConnectionStrings:SqlServer'.");

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is not null && _connection.State == System.Data.ConnectionState.Open)
                return _connection;

            _connection?.Dispose();
            _connection = new SqlConnection(_connectionString);
            await _connection.OpenAsync(ct);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        string sql,
        Dictionary<string, object?>? parameters = null,
        CancellationToken ct = default)
    {
        var conn = await GetConnectionAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.CommandTimeout = 30;

        if (parameters is not null)
            foreach (var (key, value) in parameters)
                cmd.Parameters.AddWithValue(key, value ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var results = new List<IReadOnlyDictionary<string, object?>>();

        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            results.Add(row);
        }

        return results;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
        _lock.Dispose();
    }
}
