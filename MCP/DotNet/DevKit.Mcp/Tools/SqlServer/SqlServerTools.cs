using System.ComponentModel;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.SqlServer;

[McpServerToolType]
public sealed class SqlServerTools(SqlServerService sql)
{
    [McpServerTool, Description(
        "Executes a read-only SQL query against the configured SQL Server database. " +
        "Only SELECT statements are allowed. Returns results as a list of row objects.")]
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> RunSqlQuery(
        [Description("SELECT statement to execute. Must be read-only.")] string query,
        CancellationToken ct = default)
    {
        if (!sql.IsConfigured)
            throw new InvalidOperationException("SQL Server not configured. Set SqlServer:ConnectionString in MCP config.");

        if (!query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !query.TrimStart().StartsWith("WITH", StringComparison.OrdinalIgnoreCase) &&
            !query.TrimStart().StartsWith("EXEC", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only SELECT, WITH (CTE), and EXEC (read stored procs) are allowed.");

        return await sql.QueryAsync(query, ct: ct);
    }

    [McpServerTool, Description(
        "Returns the database schema — all tables, views, and stored procedures with their columns and row counts.")]
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetDatabaseSchema(
        [Description("Filter to a specific schema name. Omit for all schemas.")] string? schemaName = null,
        CancellationToken ct = default)
    {
        if (!sql.IsConfigured) throw new InvalidOperationException("SQL Server not configured.");

        var schemaFilter = schemaName is not null ? $"AND t.TABLE_SCHEMA = '{schemaName.Replace("'", "''")}'" : "";

        var query = $"""
            SELECT
                t.TABLE_SCHEMA as [Schema],
                t.TABLE_NAME as [Table],
                t.TABLE_TYPE as [Type],
                c.COLUMN_NAME as [Column],
                c.DATA_TYPE as [DataType],
                c.IS_NULLABLE as [Nullable],
                c.CHARACTER_MAXIMUM_LENGTH as [MaxLength],
                c.COLUMN_DEFAULT as [Default],
                COLUMNPROPERTY(OBJECT_ID(t.TABLE_SCHEMA + '.' + t.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') as [IsIdentity],
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as [IsPrimaryKey]
            FROM INFORMATION_SCHEMA.TABLES t
            JOIN INFORMATION_SCHEMA.COLUMNS c
                ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME
            LEFT JOIN (
                SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk ON pk.TABLE_SCHEMA = t.TABLE_SCHEMA
                AND pk.TABLE_NAME = t.TABLE_NAME
                AND pk.COLUMN_NAME = c.COLUMN_NAME
            WHERE 1=1 {schemaFilter}
            ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME, c.ORDINAL_POSITION
            """;

        return await sql.QueryAsync(query, ct: ct);
    }

    [McpServerTool, Description(
        "Returns the detailed structure of a specific table — columns, types, constraints, indexes, and foreign keys.")]
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetTableStructure(
        [Description("Table name to inspect.")] string tableName,
        [Description("Schema name. Defaults to 'dbo'.")] string schemaName = "dbo",
        CancellationToken ct = default)
    {
        if (!sql.IsConfigured) throw new InvalidOperationException("SQL Server not configured.");

        var safeName = tableName.Replace("'", "''");
        var safeSchema = schemaName.Replace("'", "''");

        var query = $"""
            SELECT
                c.COLUMN_NAME as [Column],
                c.DATA_TYPE as [Type],
                c.CHARACTER_MAXIMUM_LENGTH as [MaxLength],
                c.NUMERIC_PRECISION as [Precision],
                c.NUMERIC_SCALE as [Scale],
                c.IS_NULLABLE as [Nullable],
                c.COLUMN_DEFAULT as [Default],
                COLUMNPROPERTY(OBJECT_ID('{safeSchema}.{safeName}'), c.COLUMN_NAME, 'IsIdentity') as [IsIdentity],
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as [IsPrimaryKey],
                CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as [IsForeignKey],
                fk.REF_TABLE as [ReferencesTable]
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                AND tc.TABLE_NAME = '{safeName}' AND tc.TABLE_SCHEMA = '{safeSchema}'
            ) pk ON pk.COLUMN_NAME = c.COLUMN_NAME
            LEFT JOIN (
                SELECT ku.COLUMN_NAME, ccu.TABLE_NAME as REF_TABLE
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc ON rc.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON ccu.CONSTRAINT_NAME = rc.UNIQUE_CONSTRAINT_NAME
                WHERE tc.CONSTRAINT_TYPE = 'FOREIGN KEY'
                AND tc.TABLE_NAME = '{safeName}' AND tc.TABLE_SCHEMA = '{safeSchema}'
            ) fk ON fk.COLUMN_NAME = c.COLUMN_NAME
            WHERE c.TABLE_NAME = '{safeName}' AND c.TABLE_SCHEMA = '{safeSchema}'
            ORDER BY c.ORDINAL_POSITION
            """;

        return await sql.QueryAsync(query, ct: ct);
    }

    [McpServerTool, Description(
        "Returns all stored procedures and their parameter definitions.")]
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetStoredProcedures(
        [Description("Filter to procedures matching this name pattern (% wildcards). Omit for all.")] string? nameFilter = null,
        CancellationToken ct = default)
    {
        if (!sql.IsConfigured) throw new InvalidOperationException("SQL Server not configured.");

        var filter = nameFilter is not null ? $"AND r.ROUTINE_NAME LIKE '{nameFilter.Replace("'", "''")}'" : "";

        var query = $"""
            SELECT
                r.ROUTINE_SCHEMA as [Schema],
                r.ROUTINE_NAME as [Name],
                p.PARAMETER_NAME as [Parameter],
                p.DATA_TYPE as [ParamType],
                p.PARAMETER_MODE as [Mode],
                p.CHARACTER_MAXIMUM_LENGTH as [MaxLength],
                r.CREATED as [Created],
                r.LAST_ALTERED as [LastModified]
            FROM INFORMATION_SCHEMA.ROUTINES r
            LEFT JOIN INFORMATION_SCHEMA.PARAMETERS p
                ON p.SPECIFIC_NAME = r.ROUTINE_NAME AND p.SPECIFIC_SCHEMA = r.ROUTINE_SCHEMA
            WHERE r.ROUTINE_TYPE = 'PROCEDURE' {filter}
            ORDER BY r.ROUTINE_SCHEMA, r.ROUTINE_NAME, p.ORDINAL_POSITION
            """;

        return await sql.QueryAsync(query, ct: ct);
    }

    [McpServerTool, Description(
        "Analyzes indexes — shows missing indexes recommended by the query optimizer, " +
        "usage statistics (seeks/scans/lookups), and flags unused indexes.")]
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetIndexAnalysis(
        [Description("Filter to a specific table. Omit for all tables.")] string? tableName = null,
        CancellationToken ct = default)
    {
        if (!sql.IsConfigured) throw new InvalidOperationException("SQL Server not configured.");

        var tableFilter = tableName is not null ? $"AND OBJECT_NAME(i.object_id) = '{tableName.Replace("'", "''")}'" : "";

        var query = $"""
            -- Existing indexes with usage stats
            SELECT
                'Existing' as [IndexType],
                OBJECT_NAME(i.object_id) as [Table],
                i.name as [IndexName],
                i.type_desc as [Type],
                i.is_unique as [IsUnique],
                i.is_primary_key as [IsPK],
                ISNULL(s.user_seeks, 0) as [Seeks],
                ISNULL(s.user_scans, 0) as [Scans],
                ISNULL(s.user_lookups, 0) as [Lookups],
                ISNULL(s.user_updates, 0) as [Updates],
                ISNULL(s.last_user_seek, NULL) as [LastSeek],
                CASE WHEN ISNULL(s.user_seeks, 0) + ISNULL(s.user_scans, 0) + ISNULL(s.user_lookups, 0) = 0
                     AND i.is_primary_key = 0 AND i.is_unique = 0 THEN 'UNUSED' ELSE 'USED' END as [Status]
            FROM sys.indexes i
            LEFT JOIN sys.dm_db_index_usage_stats s
                ON s.object_id = i.object_id AND s.index_id = i.index_id AND s.database_id = DB_ID()
            WHERE i.object_id > 100 AND i.type > 0 {tableFilter}

            UNION ALL

            -- Missing indexes from optimizer
            SELECT
                'Missing' as [IndexType],
                OBJECT_NAME(mid.object_id) as [Table],
                'IX_MISSING_' + OBJECT_NAME(mid.object_id) + '_' + REPLACE(REPLACE(ISNULL(mid.equality_columns,'') + '_' + ISNULL(mid.inequality_columns,''), ', ', '_'), '[', '') as [IndexName],
                'NONCLUSTERED' as [Type],
                0 as [IsUnique],
                0 as [IsPK],
                migs.user_seeks as [Seeks],
                migs.user_scans as [Scans],
                0 as [Lookups],
                0 as [Updates],
                migs.last_user_seek as [LastSeek],
                'MISSING' as [Status]
            FROM sys.dm_db_missing_index_details mid
            JOIN sys.dm_db_missing_index_groups mig ON mig.index_handle = mid.index_handle
            JOIN sys.dm_db_missing_index_group_stats migs ON migs.group_handle = mig.index_group_handle
            WHERE mid.database_id = DB_ID()

            ORDER BY [Table], [Status]
            """;

        return await sql.QueryAsync(query, ct: ct);
    }

    [McpServerTool, Description(
        "Returns EF Core migration status — applied and pending migrations from the __EFMigrationsHistory table.")]
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetMigrationStatus(
        [Description("Schema of the migrations table. Defaults to 'dbo'.")] string schema = "dbo",
        CancellationToken ct = default)
    {
        if (!sql.IsConfigured) throw new InvalidOperationException("SQL Server not configured.");

        var safeSchema = schema.Replace("'", "''");

        // Check if the migrations table exists
        var tableExists = await sql.QueryAsync(
            $"SELECT COUNT(1) as [Exists] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{safeSchema}' AND TABLE_NAME = '__EFMigrationsHistory'",
            ct: ct);

        if (tableExists.Count == 0 || Convert.ToInt32(tableExists[0]["Exists"]) == 0)
            return [new Dictionary<string, object?> { ["Status"] = "No __EFMigrationsHistory table found. Run `dotnet ef database update` to initialize." }];

        var query = $"""
            SELECT
                MigrationId,
                ProductVersion,
                SUBSTRING(MigrationId, 1, 14) as [AppliedAt]
            FROM [{safeSchema}].[__EFMigrationsHistory]
            ORDER BY MigrationId
            """;

        return await sql.QueryAsync(query, ct: ct);
    }

    [McpServerTool, Description(
        "Returns the top slow queries by average duration. " +
        "Requires Query Store to be enabled (ALTER DATABASE ... SET QUERY_STORE = ON).")]
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetSlowQueries(
        [Description("Minimum average duration in milliseconds to report. Default 500.")] int minDurationMs = 500,
        [Description("Maximum number of queries to return. Default 20.")] int topN = 20,
        CancellationToken ct = default)
    {
        if (!sql.IsConfigured) throw new InvalidOperationException("SQL Server not configured.");

        // Try Query Store first (SQL Server 2016+)
        var qsEnabled = await sql.QueryAsync(
            "SELECT actual_state FROM sys.database_query_store_options WHERE actual_state > 0",
            ct: ct);

        if (qsEnabled.Count > 0)
        {
            var query = $"""
                SELECT TOP {topN}
                    SUBSTRING(qt.query_sql_text, 1, 300) as [QueryText],
                    rs.avg_duration / 1000.0 as [AvgDurationMs],
                    rs.max_duration / 1000.0 as [MaxDurationMs],
                    rs.count_executions as [Executions],
                    rs.avg_cpu_time / 1000.0 as [AvgCpuMs],
                    rs.avg_logical_io_reads as [AvgLogicalReads],
                    qp.last_execution_time as [LastRun]
                FROM sys.query_store_query q
                JOIN sys.query_store_query_text qt ON qt.query_text_id = q.query_text_id
                JOIN sys.query_store_plan qp ON qp.query_id = q.query_id
                JOIN sys.query_store_runtime_stats rs ON rs.plan_id = qp.plan_id
                WHERE rs.avg_duration / 1000.0 >= {minDurationMs}
                    AND qp.last_execution_time >= DATEADD(hour, -24, GETUTCDATE())
                ORDER BY rs.avg_duration DESC
                """;
            return await sql.QueryAsync(query, ct: ct);
        }

        // Fallback to DMV-based (less precise)
        var dmvQuery = $"""
            SELECT TOP {topN}
                SUBSTRING(st.text, (qs.statement_start_offset/2)+1,
                    ((CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text)
                    ELSE qs.statement_end_offset END - qs.statement_start_offset)/2)+1) as [QueryText],
                qs.total_elapsed_time / qs.execution_count / 1000.0 as [AvgDurationMs],
                qs.max_elapsed_time / 1000.0 as [MaxDurationMs],
                qs.execution_count as [Executions],
                qs.total_worker_time / qs.execution_count / 1000.0 as [AvgCpuMs],
                qs.total_logical_reads / qs.execution_count as [AvgLogicalReads],
                qs.last_execution_time as [LastRun]
            FROM sys.dm_exec_query_stats qs
            CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
            WHERE qs.total_elapsed_time / qs.execution_count / 1000.0 >= {minDurationMs}
            ORDER BY qs.total_elapsed_time / qs.execution_count DESC
            """;
        return await sql.QueryAsync(dmvQuery, ct: ct);
    }

    [McpServerTool, Description(
        "Returns the database and table sizes — data, index, and free space for each table.")]
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetDatabaseSize(
        CancellationToken ct = default)
    {
        if (!sql.IsConfigured) throw new InvalidOperationException("SQL Server not configured.");

        var query = """
            SELECT
                t.NAME as [Table],
                s.Name as [Schema],
                p.rows as [RowCount],
                SUM(a.total_pages) * 8 / 1024.0 as [TotalMB],
                SUM(a.used_pages) * 8 / 1024.0 as [UsedMB],
                (SUM(a.total_pages) - SUM(a.used_pages)) * 8 / 1024.0 as [FreeMB]
            FROM sys.tables t
            JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
            JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
            JOIN sys.allocation_units a ON p.partition_id = a.container_id
            JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE t.NAME NOT LIKE 'dt%' AND t.is_ms_shipped = 0 AND i.OBJECT_ID > 255
            GROUP BY t.Name, s.Name, p.Rows
            ORDER BY SUM(a.total_pages) DESC
            """;

        return await sql.QueryAsync(query, ct: ct);
    }

    [McpServerTool, Description(
        "Detects currently blocked queries — sessions waiting on locks, " +
        "the blocking session, wait type, and duration. Empty result means no blocking.")]
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> DetectBlockingQueries(
        CancellationToken ct = default)
    {
        if (!sql.IsConfigured) throw new InvalidOperationException("SQL Server not configured.");

        var query = """
            SELECT
                r.session_id as [BlockedSessionId],
                r.blocking_session_id as [BlockingSessionId],
                r.wait_type as [WaitType],
                r.wait_time / 1000.0 as [WaitSeconds],
                r.status as [Status],
                SUBSTRING(st.text, (r.statement_start_offset/2)+1,
                    ((CASE r.statement_end_offset WHEN -1 THEN DATALENGTH(st.text)
                    ELSE r.statement_end_offset END - r.statement_start_offset)/2)+1) as [BlockedQuery],
                bst.text as [BlockingQuery],
                s.login_name as [Login],
                s.host_name as [Host],
                s.program_name as [Application]
            FROM sys.dm_exec_requests r
            JOIN sys.dm_exec_sessions s ON s.session_id = r.session_id
            CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) st
            LEFT JOIN sys.dm_exec_requests br ON br.session_id = r.blocking_session_id
            OUTER APPLY sys.dm_exec_sql_text(br.sql_handle) bst
            WHERE r.blocking_session_id > 0
            ORDER BY r.wait_time DESC
            """;

        return await sql.QueryAsync(query, ct: ct);
    }

    [McpServerTool, Description(
        "Returns the foreign key relationship map — which tables reference which, " +
        "useful for understanding entity relationships and cascade behavior.")]
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetForeignKeyMap(
        [Description("Filter to relationships involving this table (as parent or child). Omit for all.")] string? tableName = null,
        CancellationToken ct = default)
    {
        if (!sql.IsConfigured) throw new InvalidOperationException("SQL Server not configured.");

        var tableFilter = tableName is not null
            ? $"AND (OBJECT_NAME(fk.parent_object_id) = '{tableName.Replace("'", "''")}' OR OBJECT_NAME(fk.referenced_object_id) = '{tableName.Replace("'", "''")}')"
            : "";

        var query = $"""
            SELECT
                OBJECT_NAME(fk.parent_object_id) as [ChildTable],
                COL_NAME(fkc.parent_object_id, fkc.parent_column_id) as [ChildColumn],
                OBJECT_NAME(fk.referenced_object_id) as [ParentTable],
                COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) as [ParentColumn],
                fk.name as [ConstraintName],
                fk.delete_referential_action_desc as [OnDelete],
                fk.update_referential_action_desc as [OnUpdate]
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            WHERE 1=1 {tableFilter}
            ORDER BY [ChildTable], [ConstraintName]
            """;

        return await sql.QueryAsync(query, ct: ct);
    }
}
