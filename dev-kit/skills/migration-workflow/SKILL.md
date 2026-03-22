---
name: migration-workflow
description: >
  Manage EF Core database migrations — create, apply, rollback, validate, and generate
  SQL scripts with safety checks for destructive changes.
  Load this skill when: "migration", "ef core migration", "database update",
  "/migration-workflow", "add migration", "dotnet ef", "schema change", "rollback migration".
user-invocable: true
argument-hint: "[add|apply|status|rollback|script|validate] [migration-name]"
allowed-tools: Read, Bash, Glob, Grep
---

# Migration Workflow — EF Core Database Migrations

## Core Principles

1. **Preview before applying** — Always generate and show the idempotent SQL script to the user before running `database update`. There must be no surprises in production.
2. **Flag destructive operations immediately** — When generating a migration, scan it for table drops, column drops, and non-nullable column additions. These require explicit user acknowledgment before proceeding.
3. **Never apply to production without a backup** — Production migration runs must be preceded by a database backup. The script approach (applying SQL directly) is safer than `dotnet ef database update` on prod.
4. **Locate project structure before running any command** — Always find the `*.Infrastructure` project (DbContext lives here) and the `*.Api` startup project. The `--project` and `--startup-project` flags are required for all EF commands.
5. **Non-nullable columns on existing tables need two phases** — Adding a `NOT NULL` column to a table with data requires either a default value or a two-phase migration (nullable first, backfill, then add constraint).

## Patterns

### Safe Migration Addition

```bash
# GOOD — always check for schema file changes first, then add with both project flags
git diff --name-only "*.cs"  # see what changed before adding migration

dotnet ef migrations add AddOrderTrackingNumber \
  --project src/MyApp.Infrastructure \
  --startup-project src/MyApp.Api

# Review the generated Up() and Down() methods before proceeding
# Look for:
#   migrationBuilder.DropTable(...)       ← data loss risk
#   migrationBuilder.DropColumn(...)      ← data loss risk
#   migrationBuilder.AlterColumn(nullable: false)  ← will fail on existing data
```

### Idempotent SQL Script for Deployment

```bash
# GOOD — generate idempotent script with date stamp for deployment
dotnet ef migrations script \
  --idempotent \
  --output "migrations-$(date +%Y%m%d).sql" \
  --project src/MyApp.Infrastructure \
  --startup-project src/MyApp.Api

# --idempotent wraps each migration in an IF NOT EXISTS check
# Safe to run multiple times — won't re-apply already-applied migrations
```

### Two-Phase Migration for Non-Nullable Column

```csharp
// Phase 1 Migration — add as nullable first
migrationBuilder.AddColumn<string>(
    name: "TrackingNumber",
    table: "Orders",
    nullable: true);  // nullable in phase 1

// Phase 2 Migration — after backfilling data:
migrationBuilder.Sql("UPDATE Orders SET TrackingNumber = 'LEGACY-' + CAST(Id AS NVARCHAR(50)) WHERE TrackingNumber IS NULL");
migrationBuilder.AlterColumn<string>(
    name: "TrackingNumber",
    table: "Orders",
    nullable: false);  // now safe to make non-nullable

// BAD — single migration adding NOT NULL on a table with existing rows
migrationBuilder.AddColumn<string>(
    name: "TrackingNumber",
    table: "Orders",
    nullable: false);  // will fail: existing rows have no value
```

### Migration Status Check

```bash
# Shows which migrations are applied (✅) and which are pending (⏳)
dotnet ef migrations list \
  --project src/MyApp.Infrastructure \
  --startup-project src/MyApp.Api

# Output:
# 20241001_InitialCreate (Applied)
# 20241015_AddOrderStatus (Applied)
# 20241220_AddTrackingNumber (Pending)  ← needs database update
```

## Anti-patterns

### Applying Without Previewing

```bash
# BAD — running database update without seeing what SQL will execute
dotnet ef database update --project src/MyApp.Infrastructure --startup-project src/MyApp.Api

# GOOD — generate idempotent script first, review it, then apply
dotnet ef migrations script --idempotent --output preview.sql ...
# [show preview.sql to user]
# [user confirms]
dotnet ef database update ...
```

### Missing --project Flags

```bash
# BAD — omitting project flags, EF uses wrong working directory
dotnet ef migrations add AddPaymentStatus

# GOOD — always specify both project flags
dotnet ef migrations add AddPaymentStatus \
  --project src/MyApp.Infrastructure \
  --startup-project src/MyApp.Api
```

### Rollback Without Awareness of Consequences

```bash
# BAD — rolling back without generating the Down() SQL for review
dotnet ef database update 20241001_InitialCreate ...

# GOOD — generate and show the down-migration SQL first
dotnet ef migrations script 20241220_AddTrackingNumber 20241001_InitialCreate \
  --output rollback-preview.sql ...
# [show to user, warn about data loss]
# [explicit user confirmation]
dotnet ef database update 20241001_InitialCreate ...
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Added/changed an entity configuration | `/migration-workflow add <DescriptiveName>` |
| Check what migrations are pending | `/migration-workflow status` |
| Apply pending migrations to dev database | `/migration-workflow apply` |
| Prepare SQL for a production deployment | `/migration-workflow script` |
| Migration added a column drop | Warn user, require explicit confirmation |
| Non-nullable column on existing table | Two-phase migration: nullable → backfill → constrain |
| Rollback dev database to earlier state | `/migration-workflow rollback <MigrationName>` |
| Migration file looks wrong / model mismatch | `/migration-workflow validate` |
| Production deploy | Generate script, review, apply manually with DBA |

## Execution

### Prerequisite — Locate Project Structure
Before any command: find the `*.Infrastructure` project (contains `DbContext`) and `*.Api` startup project. Use `Glob "**/*.Infrastructure.csproj"` and `Glob "**/*.Api.csproj"` to locate them.

### `/migration-workflow add <MigrationName>`
1. Check for uncommitted schema changes: `git diff --name-only "*.cs"`
2. Run: `dotnet ef migrations add {MigrationName} --project src/{App}.Infrastructure --startup-project src/{App}.Api`
3. Review the generated migration file — flag any:
   - Table drops (data loss risk)
   - Column drops (data loss risk)
   - Non-nullable columns added to existing tables (will fail on non-empty DB)
4. Show the migration summary to the user and await acknowledgment for any flagged items

### `/migration-workflow apply [environment]`
1. Check pending migrations: `dotnet ef migrations list --project ... --startup-project ...`
2. Generate SQL preview: `dotnet ef migrations script --idempotent --output migration-preview.sql ...`
3. Show the SQL to the user for review
4. Run: `dotnet ef database update --project ... --startup-project ...`
5. Verify success with another `migrations list`

### `/migration-workflow status`
Run: `dotnet ef migrations list --project ... --startup-project ...`
- Show applied migrations (✅) and pending migrations (⏳)
- Warn if migrations are out of order or have gaps

### `/migration-workflow rollback <TargetMigration>`
1. **WARN**: This is destructive — confirm with user before proceeding
2. Generate down-migration SQL for review
3. Run: `dotnet ef database update {TargetMigration} --project ... --startup-project ...`

### `/migration-workflow script [from] [to]`
Generate idempotent SQL script for deployment:
`dotnet ef migrations script --idempotent --output migrations-{date}.sql --project ... --startup-project ...`

### `/migration-workflow validate`
1. Check for common issues:
   - Missing or empty `Down()` implementations
   - Migrations not matching model snapshot
   - Orphaned migration files (in folder but not in snapshot)

## Safety Rules
- Always generate and review SQL before applying to production
- Never run `database update` on prod without a backup
- Always test migrations on a copy of production data first
- Non-nullable column additions need a default value or two-phase migration
