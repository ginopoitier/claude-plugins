---
name: sqlserver
description: >
  SQL Server diagnostics and management — query performance, index analysis, blocking detection,
  schema inspection, and EF Core migration status via MCP tools.
  Load this skill when: "sqlserver", "sql server", "database performance", "slow query",
  "/sqlserver", "index analysis", "blocking", "query store", "sql diagnostics", "schema inspect".
user-invocable: true
argument-hint: "[query|schema|indexes|blocking|migrations|slow-queries|size|fk-map] [target]"
allowed-tools: Read, Bash, Glob, Grep
---

# SQL Server — Diagnostics and Management

## Core Principles

1. **Read-only queries only** — The `/sqlserver query` command executes only SELECT statements. Never run DDL (CREATE, ALTER, DROP) or DML (INSERT, UPDATE, DELETE) through this skill. Schema changes go through EF Core migrations.
2. **Always summarize findings with concrete recommendations** — After every diagnostic, tell the user what to do next. "You have 3 missing indexes" is incomplete; "Add these specific indexes, in this order, expecting these improvements" is useful.
3. **Check prerequisites before running** — The `dev-kit-mcp` must be connected and `SQLSERVER_CONNECTION_STRING` must be set in kit config. If either is missing, direct the user to fix it before proceeding.
4. **Index fragmentation context matters** — A missing index recommendation from `sys.dm_db_missing_index_details` is a hint, not a mandate. Always cross-reference with actual query patterns and write overhead before recommending index additions.
5. **Blocking chains need root-cause analysis** — When blocking is detected, identify the head blocker (the session that no one is waiting for). The blocked sessions are symptoms; the head blocker is the cause.

## Patterns

### Index Analysis Workflow

```sql
-- GOOD — check missing indexes alongside usage stats for existing indexes
-- Missing indexes (from SQL Server's automatic tracking)
SELECT
    migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans) AS ImprovementMeasure,
    mid.statement AS TableName,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns
FROM sys.dm_db_missing_index_group_stats migs
JOIN sys.dm_db_missing_index_groups mig ON migs.group_handle = mig.index_group_handle
JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
ORDER BY ImprovementMeasure DESC;

-- Unused existing indexes (high write cost, low read benefit)
SELECT
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    ius.user_seeks + ius.user_scans + ius.user_lookups AS TotalReads,
    ius.user_updates AS TotalWrites
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats ius ON i.object_id = ius.object_id AND i.index_id = ius.index_id
WHERE ius.user_seeks + ius.user_scans + ius.user_lookups < 100
  AND ius.user_updates > 1000;  -- high write cost, low read benefit
```

### Blocking Detection

```sql
-- GOOD — show full blocking chain with query text
SELECT
    blocking.session_id AS BlockingSession,
    blocked.session_id AS BlockedSession,
    blocked.wait_type,
    blocked.wait_time / 1000.0 AS WaitSeconds,
    SUBSTRING(blocking_sql.text, 1, 500) AS BlockingQuery,
    SUBSTRING(blocked_sql.text, 1, 500) AS BlockedQuery
FROM sys.dm_exec_requests blocked
JOIN sys.dm_exec_sessions blocking ON blocked.blocking_session_id = blocking.session_id
CROSS APPLY sys.dm_exec_sql_text(blocking.most_recent_sql_handle) blocking_sql
CROSS APPLY sys.dm_exec_sql_text(blocked.sql_handle) blocked_sql
WHERE blocked.blocking_session_id > 0;

-- Interpretation:
-- BlockingSession with blocking_session_id = 0 is the HEAD BLOCKER
-- Fix the head blocker; blocked sessions will resolve automatically
```

### Slow Query Investigation

```sql
-- GOOD — use Query Store for historical slow query analysis (SQL Server 2016+)
SELECT TOP 20
    qsq.query_id,
    qsqt.query_sql_text,
    qsrs.avg_duration / 1000.0 AS AvgDurationMs,
    qsrs.avg_cpu_time / 1000.0 AS AvgCpuMs,
    qsrs.count_executions AS ExecutionCount,
    qsrs.avg_logical_io_reads AS AvgLogicalReads
FROM sys.query_store_query qsq
JOIN sys.query_store_query_text qsqt ON qsq.query_text_id = qsqt.query_text_id
JOIN sys.query_store_plan qsp ON qsq.query_id = qsp.query_id
JOIN sys.query_store_runtime_stats qsrs ON qsp.plan_id = qsrs.plan_id
ORDER BY qsrs.avg_duration DESC;
```

### Schema Inspection Pattern

```
// Workflow for understanding a table before suggesting changes:
1. /sqlserver schema Orders          → see columns, types, constraints
2. /sqlserver indexes Orders         → see existing indexes, usage stats
3. /sqlserver fk-map Orders          → see what other tables reference it
4. /sqlserver slow-queries           → see queries against this table
→ Now you have full context before recommending any changes
```

## Anti-patterns

### Making Index Recommendations Without Usage Data

```
// BAD — recommending every missing index from sys.dm_db_missing_index_details
"You're missing 12 indexes. Add all of them."
→ Some have trivial improvement measure
→ Some have high write overhead on frequently-updated tables
→ Duplicate/overlapping indexes waste space and slow writes

// GOOD — prioritize by ImprovementMeasure, cross-check write frequency
"Top priority: Index on Orders(CustomerId) includes (Status, CreatedAt)
 — ImprovementMeasure: 1,847,000. This table has low write frequency (200/hr)
 so the maintenance overhead is acceptable."
```

### Diagnosing Blocked Sessions Instead of Head Blocker

```sql
-- BAD — trying to fix blocked sessions directly (they're just waiting)
-- "Session 52 is blocked — let's kill it"
KILL 52;  -- session 54 (the next one in chain) immediately blocks again

-- GOOD — find and address the head blocker
-- Session 48 is blocking 52, which is blocking 54, which is blocking 61
-- Head blocker: 48 (blocking_session_id = 0)
-- Resolution: investigate session 48's query for missing index or long transaction
```

### Running Diagnostics Without Prerequisites Check

```
// BAD — jumping to MCP tool calls before confirming connectivity
sqlserver_get_index_analysis()
→ "Error: connection string not configured"

// GOOD — check config first
1. Read ~/.claude/kit.config.md → verify SQLSERVER_CONNECTION_STRING present
2. Verify dev-kit-mcp is listed in active MCP connections
3. If missing → "Run /kit-setup to configure SQLSERVER_CONNECTION_STRING"
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| API is slow — suspect database | `/sqlserver slow-queries 500` |
| Want to see all tables and structure | `/sqlserver schema` |
| Check specific table structure | `/sqlserver schema Orders` |
| EF migrations not matching DB | `/sqlserver migrations` |
| App hangs under load | `/sqlserver blocking` |
| Analyze index opportunities | `/sqlserver indexes` |
| Database growing unexpectedly | `/sqlserver size` |
| Need to understand FK relationships | `/sqlserver fk-map Orders` |
| Run a custom diagnostic query | `/sqlserver query "SELECT TOP 10 ..."` |
| Connection string missing | Run `/kit-setup` |

## Execution

### Prerequisites
The MCP `dev-kit-mcp` must be connected and SQL Server connection must be configured in `~/.claude/kit.config.md` (`SQLSERVER_CONNECTION_STRING`). If missing, tell the user to run `/kit-setup`.

### `/sqlserver query <sql>`
Runs read-only SQL query (SELECT statements only):
- Use `sqlserver_run_query` MCP tool
- Format results as a table
- Show execution time and row count

### `/sqlserver schema [table]`
- Omitting table: calls `sqlserver_get_database_schema` — shows all tables, views, stored procedures
- With table name: calls `sqlserver_get_table_structure` — columns, types, constraints, indexes

### `/sqlserver indexes [table]`
Calls `sqlserver_get_index_analysis`:
- Shows missing indexes (from `sys.dm_db_missing_index_details`) sorted by ImprovementMeasure
- Shows index usage statistics (reads vs writes)
- Flags unused indexes (low reads, high writes)
- Recommends specific `CREATE INDEX` statements for high-priority gaps

### `/sqlserver blocking`
Calls `sqlserver_detect_blocking_queries`:
- Shows currently blocked and blocking sessions
- Identifies the head blocker (root cause)
- Shows query text, wait type, duration
- Recommends resolution (kill, query optimization, index)

### `/sqlserver migrations`
Calls `sqlserver_get_migration_status`:
- Shows applied EF Core migrations
- Shows pending migrations
- Warns if schema drift detected

### `/sqlserver slow-queries [ms=1000]`
Calls `sqlserver_get_slow_queries` with duration threshold:
- Top 20 slowest queries from Query Store or DMVs
- Execution count, avg duration, total CPU, avg logical reads
- Suggests: add index, rewrite query, add caching

### `/sqlserver size`
Calls `sqlserver_get_database_size`:
- Database total size, data file, log file
- Table sizes sorted by largest first

### `/sqlserver fk-map [table]`
Calls `sqlserver_get_foreign_key_map`:
- Foreign key relationships for the given table
- Or all FK relationships in the database

### After Diagnostics
Always summarize findings and suggest concrete next actions:
1. For index issues → suggest the specific `CREATE INDEX` statement via an EF Core migration
2. For blocking → identify the head blocker query and suggest optimization
3. For slow queries → suggest Query Store review and specific indexing strategy
4. For pending migrations → remind to run `/migration-workflow apply`
