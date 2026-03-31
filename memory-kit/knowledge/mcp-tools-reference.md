# Memory MCP Tools Reference

Complete reference for all `memory-mcp` MCP tools: signatures, usage patterns, and integration examples.

## Quick Reference Table

| Tool | Purpose | When to use |
|------|---------|-------------|
| `memory_search` | Find relevant memories by query | Before starting work in a known domain |
| `memory_store` | Create or update a memory | After classifying content to capture |
| `memory_get` | Read a specific memory file | When you know the exact file path |
| `memory_list` | Enumerate all memories | Health checks, session start summary |
| `memory_delete` | Remove a memory | When explicitly forgetting |
| `memory_deduplicate` | Find similar memories | Before creating new feedback memories |
| `memory_health` | Full store audit | `/memory-health` skill |
| `memory_sync_index` | Rebuild MEMORY.md | After any delete or drift detected |
| `memory_classify` | Auto-detect type from content | First step in capture flow |
| `memory_export` | Export all memories | Backup, sharing |

---

## `memory_search`

Keyword/TF-IDF search across all memory files for the active project.

```json
// Input
{
  "query": "integration tests database mocking",
  "type": "feedback",          // optional — filter by type
  "project_id": "my-project",  // optional — defaults to active project
  "limit": 5,                  // optional — default 5, max 20
  "min_score": 0.2             // optional — default 0.2, range 0.0-1.0
}

// Output
{
  "results": [
    {
      "file": "memory/feedback_db_testing.md",
      "name": "integration-tests-use-real-db",
      "type": "feedback",
      "score": 0.91,
      "excerpt": "Don't mock the database in integration tests..."
    }
  ],
  "total": 1,
  "project_id": "my-project"
}
```

**Usage pattern:** Call before implementing code in any area that has prior corrections.

---

## `memory_store`

Create or update a memory file. If `file_name` matches an existing file, updates in-place.

```json
// Input
{
  "name": "integration-tests-use-real-db",
  "description": "Integration tests must use real database; mocked tests mask migration failures",
  "type": "feedback",
  "body": "Don't mock the database in integration tests.\n\n**Why:** ...\n\n**How to apply:** ...",
  "project_id": "my-project",  // optional
  "file_name": "feedback_db_testing.md",  // optional — derived from type+name if omitted
  "confidence": 1.0,           // optional — default 1.0
  "source": "manual"           // optional — manual|auto-capture|promoted-instinct
}

// Output
{
  "file": "memory/feedback_db_testing.md",
  "action": "created",  // or "updated"
  "index_updated": true
}
```

**Note:** Always call `memory_classify` first if the type is uncertain.

---

## `memory_classify`

Auto-classify content into a memory type with confidence score.

```json
// Input
{
  "content": "Don't mock the database in integration tests — we got burned last quarter when mocked tests passed but the prod migration failed."
}

// Output
{
  "type": "feedback",
  "confidence": 0.89,
  "reasoning": "Contains correction pattern ('don't') with explicit 'why' explanation and incident reference",
  "suggested_name": "avoid-database-mocking-in-tests",
  "suggested_description": "Integration tests must use real database; mocked tests mask migration failures"
}
```

**Always use this as the first step in the capture flow** before calling `memory_store`.

---

## `memory_deduplicate`

Find memories with high semantic similarity (potential duplicates or conflicts).

```json
// Input
{
  "project_id": "my-project",  // optional
  "threshold": 0.7             // optional — default 0.7
}

// Output
{
  "groups": [
    {
      "similarity": 0.85,
      "files": ["memory/feedback_logging_a.md", "memory/feedback_logging_b.md"],
      "recommendation": "merge — both address console.log usage in production code",
      "conflict": false
    }
  ],
  "total_groups": 1,
  "clean": false
}
```

**Call this before creating a new feedback memory** to avoid duplicates.

---

## `memory_health`

Full health report on the memory store.

```json
// Input
{ "project_id": "my-project" }  // optional

// Output
{
  "total": 12,
  "by_type": { "user": 2, "feedback": 7, "project": 2, "reference": 1 },
  "issues": [
    {
      "severity": "error",
      "file": "memory/feedback_testing.md",
      "issue": "missing required 'description' field in frontmatter"
    },
    {
      "severity": "warning",
      "file": "memory/project_sprint.md",
      "issue": "stale — not updated in 45 days (threshold: 30)"
    }
  ],
  "index_sync": "drift",
  "drift_details": "2 files not in MEMORY.md index",
  "coverage_gaps": ["no reference memories found"],
  "score": 68
}
```

---

## `memory_sync_index`

Rebuild MEMORY.md index from actual files on disk.

```json
// Input
{ "project_id": "my-project" }  // optional

// Output
{
  "added": 3,
  "removed": 1,
  "unchanged": 8,
  "index_path": "/home/user/.claude/projects/my-project/MEMORY.md"
}
```

**Always call after any `memory_delete`** to keep index accurate.

---

## `memory_list`

Enumerate all memories with metadata.

```json
// Input
{
  "type": "feedback",          // optional
  "project_id": "my-project",  // optional
  "sort": "updated",           // optional — updated|created|name|confidence
  "format": "full"             // optional — full|summary
}

// Output (format: full)
{
  "memories": [
    {
      "file": "memory/feedback_db_testing.md",
      "name": "integration-tests-use-real-db",
      "description": "Integration tests must use real database...",
      "type": "feedback",
      "updated": "2026-03-15T14:30:00Z",
      "confidence": 1.0,
      "source": "manual"
    }
  ],
  "total": 7,
  "by_type": { "feedback": 7 }
}
```

---

## `memory_delete`

Remove a memory file and its MEMORY.md index entry.

```json
// Input
{
  "file_path": "memory/project_sprint_q1.md",
  "project_id": "my-project"  // optional
}

// Output
{
  "deleted": "memory/project_sprint_q1.md",
  "index_updated": true
}
```

---

## Integration Patterns

### Capture Flow (complete example)

```
1. memory_classify({ content: userContent })
   → type: "feedback", suggested_name: "avoid-raw-sql"

2. memory_search({ query: "avoid raw sql database", type: "feedback" })
   → No results (safe to create)

3. memory_store({
     name: "avoid-raw-sql",
     description: "Use EF Core instead of raw SQL — migrations won't track raw queries",
     type: "feedback",
     body: "...",
     source: "manual"
   })
   → { file: "memory/feedback_avoid_raw_sql.md", action: "created" }
```

### Session Start Inject Pattern

```
memory_list({ format: "summary", sort: "updated" })
→ Output top 8 most recent memories as context summary
```

### Pre-Implementation Check

```
memory_search({ query: taskDescription, limit: 5 })
→ Surface relevant rules and references before writing code
```

### Health Gate Before Session End

```
memory_health()
→ If score < 70: offer to consolidate/sync
→ If index_sync == "drift": auto-call memory_sync_index
```
