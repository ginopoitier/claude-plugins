---
name: sqlserver
description: >
  SQL Server diagnostics via the devkit-mcp MCP server — inspect schema, analyze indexes,
  detect blocking queries, profile slow queries, run ad-hoc SQL, and review table structure.
  Load this skill when: "sql server", "database schema", "slow queries", "blocking queries",
  "index analysis", "table structure", "foreign keys", "run sql", "db diagnostics",
  "database size", "sql diagnostics", "stored procedures", "migration status".
user-invocable: true
argument-hint: "[schema|indexes|blocking|slow|size|table <name>|query <sql>|fk|procs|migrations]"
allowed-tools: Read, Bash
---

# SQL Server — Database Diagnostics

## Core Principles

1. **Read before writing** — All tools in this skill are read-only diagnostics. Never modify schema or data through MCP tools. Use EF Core migrations for schema changes.
2. **Use MCP tools, not raw SQL** — The devkit-mcp SQL Server tools return structured, formatted output. Prefer them over ad-hoc `run_sql_query` for standard diagnostic tasks.
3. **Connection comes from devkit-mcp config** — The MCP server uses the connection string configured at startup. Do not prompt the user for credentials.
4. **Blocking takes priority** — If the user reports slowness, check blocking first (`get_blocking_queries`). Blocking cascades and makes index analysis misleading.

## Patterns

### Schema Inspection

```
# Inspect the full database schema — tables, columns, types, nullability
→ Use: get_database_schema
→ Returns: all tables with column definitions, data types, and constraints

# Get detailed structure for a specific table
→ Use: get_table_structure  tableName="Orders"
→ Returns: columns, indexes, constraints, row count estimate

# Map foreign key relationships across the database
→ Use: get_foreign_key_map
→ Returns: parent/child table relationships, ON DELETE behavior
```

### Performance Diagnostics

```
# Find the slowest queries by average duration (requires Query Store enabled)
→ Use: get_slow_queries
→ Returns: top N queries ranked by avg duration, execution count, total CPU

# Check for blocking chains (sessions waiting on locks)
→ Use: detect_blocking_queries
→ Returns: blocking chain with head blocker, wait type, blocking SQL

# Analyze index usage — missing, unused, and duplicate indexes
→ Use: get_index_analysis
→ Returns: index usage stats, missing index recommendations, duplicate candidates
```

### Ad-Hoc Queries

```sql
-- GOOD — read-only diagnostics query
→ Use: run_sql_query  sql="SELECT TOP 10 name, row_count FROM sys.dm_db_partition_stats ..."

-- BAD — never use run_sql_query for writes
→ Do NOT: run_sql_query  sql="UPDATE Orders SET Status = 'Cancelled' WHERE ..."
-- Use EF Core and proper application code for data mutations
```

### Storage and Migration

```
# Check database and table sizes
→ Use: get_database_size
→ Returns: database total size, data/log split, per-table space usage

# Check EF Core migration status (applied vs pending)
→ Use: get_migration_status
→ Returns: applied migrations with timestamps, pending migrations list

# List stored procedures and their definitions
→ Use: get_stored_procedures
→ Returns: procedure names, creation dates, parameter lists
```

## Anti-patterns

### Using run_sql_query for Writes

```
# BAD — running DML through the diagnostic tool
→ run_sql_query sql="DELETE FROM Logs WHERE CreatedAt < '2024-01-01'"
→ MCP tools bypass application logic, audit trails, and EF tracking

# GOOD — use application code for mutations
→ Diagnose with MCP tools, implement changes through EF Core migrations or application endpoints
```

### Checking Indexes Before Blocking

```
# BAD — jumping to index analysis while blocking is occurring
"The query is slow, let me check the indexes."
→ If a session is blocked, it shows as slow regardless of index health

# GOOD — blocking first, then indexes
→ detect_blocking_queries first
→ If blocking found: identify and resolve the head blocker
→ If no blocking: then get_index_analysis
```

### Diagnosing Without Query Store

```
# BAD — assuming get_slow_queries will work on any database
→ get_slow_queries returns empty on databases without Query Store enabled

# GOOD — check if Query Store is enabled first
→ run_sql_query sql="SELECT state_desc FROM sys.database_query_store_options WHERE database_id = DB_ID()"
→ If not enabled: ALTER DATABASE [MyDb] SET QUERY_STORE = ON
```

## Decision Guide

| Scenario | Tool |
|----------|------|
| "What tables exist?" | `get_database_schema` |
| "Show me the Orders table structure" | `get_table_structure tableName="Orders"` |
| "The app is slow / timing out" | `detect_blocking_queries` first, then `get_slow_queries` |
| "We're running out of disk space" | `get_database_size` |
| "Which indexes are we missing?" | `get_index_analysis` |
| "How are tables related?" | `get_foreign_key_map` |
| "Which migrations have run?" | `get_migration_status` |
| "Find all stored procedures" | `get_stored_procedures` |
| "Run a diagnostic SELECT" | `run_sql_query` (read-only only) |

## Execution

Identify the diagnostic task from the user's arguments, select the appropriate devkit-mcp SQL Server tool, run it, and present the results with a brief interpretation — flag any blocking chains, missing indexes, or oversized tables that require action.

$ARGUMENTS
