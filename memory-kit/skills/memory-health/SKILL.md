---
name: memory-health
description: >
  Audit the memory store: stale entries, missing fields, duplicates, index drift, and
  coverage gaps. Produces a health score and actionable fix list. Load this skill when:
  "memory health", "audit memories", "/memory-health", "memory-health", "check memories",
  "clean up memory", "memory audit", "memory report", "how many memories".
user-invocable: true
argument-hint: "[--project <id>]"
allowed-tools: Read, Glob, Grep
---

# Memory Health

## Core Principles

1. **Health score first** — Produce a single 0-100 score so the user knows at a glance if their memory store needs attention.
2. **Issues by severity** — Group issues as error (must fix), warning (should fix), info (nice to fix).
3. **Always offer fixes** — Every issue listed should have a suggested action or skill to resolve it.
4. **Index sync is critical** — A drifted MEMORY.md index means memories are invisible to hooks and search. Fix first.
5. **Coverage gaps matter** — A store with no user memories or no feedback memories is missing context that would improve Claude's responses.

## Health Check Flow

```
1. LOAD
   Call memory_health (or fall back to manual file scan if MCP unavailable)

2. REPORT
   Show:
   - Summary: total memories, by-type breakdown, health score
   - Issues: grouped by severity (error → warning → info)
   - Index sync status
   - Coverage gaps

3. OFFER ACTIONS
   Per issue category, suggest the right skill:
   - Duplicates → /memory-consolidate
   - Stale entries → /memory-forget
   - Index drift → Run memory_sync_index
   - Missing fields → /memory-capture (update the file)

4. AUTO-FIX (with confirmation)
   Ask: "Fix index drift automatically? [Y/n]"
   If yes → call memory_sync_index, report changes
```

## Report Format

```
Memory Health Report — {project-id}
Generated: {date}

Summary
  Total memories: 12
  By type: feedback (7), user (2), project (2), reference (1)
  Health score: 72/100

Issues — Errors (must fix)
  ✗ feedback_testing.md — missing 'description' field (invisible in search)
  ✗ MEMORY.md index is out of sync — 3 files not indexed

Issues — Warnings (should fix)
  ⚠ project_sprint.md — stale (last updated 45 days ago, threshold: 30)
  ⚠ feedback_logging.md — potential duplicate of feedback_console.md (similarity: 0.78)

Issues — Info (nice to fix)
  ℹ reference_jira.md — last updated 75 days ago (threshold: 90)

Coverage Gaps
  ! No reference memories — consider documenting external system locations
  ! No user profile memory — Claude doesn't know your role/expertise

Suggested Actions
  1. Run memory_sync_index to fix index drift (automatic)
  2. Run /memory-consolidate to review duplicate memories
  3. Run /memory-forget to remove stale project_sprint.md
  4. Update feedback_testing.md to add missing description
```

## Health Scoring

| Metric | Weight | Max Points |
|--------|--------|-----------|
| No errors | 40% | 40 |
| No warnings | 25% | 25 |
| Index in sync | 20% | 20 |
| No coverage gaps | 15% | 15 |

Score 90-100: Excellent — store is well-maintained
Score 70-89: Good — minor issues to address
Score 50-69: Fair — multiple issues, deduplicate and prune
Score < 50: Poor — immediate attention required

## Fallback (MCP unavailable)

If `memory_health` MCP tool is unavailable, perform a manual audit:

```bash
MEMORY_DIR="~/.claude/projects/{project-id}/memory"
MEMORY_INDEX="~/.claude/projects/{project-id}/MEMORY.md"

# Count files by type
ls $MEMORY_DIR/user_*.md    # user memories
ls $MEMORY_DIR/feedback_*.md # feedback memories
ls $MEMORY_DIR/project_*.md  # project memories
ls $MEMORY_DIR/reference_*.md # reference memories

# Check index entries vs files
INDEX_COUNT=$(grep -c "^-" $MEMORY_INDEX)
FILE_COUNT=$(ls $MEMORY_DIR/*.md | wc -l)
# If FILE_COUNT != INDEX_COUNT → drift detected

# Check frontmatter completeness
for f in $MEMORY_DIR/*.md; do
  grep -q "^description:" $f || echo "MISSING description: $f"
  grep -q "^type:" $f || echo "MISSING type: $f"
done
```

## Anti-patterns

### Running Health Without Acting

```
# BAD — health report generated, issues noted, nothing done
"Memory health: 12 issues found."
*No follow-up*

# GOOD — health report leads to immediate fixes
"Memory health: 12 issues. Fixing index drift now... Done.
 2 duplicates detected — run /memory-consolidate to review."
```

### Ignoring Coverage Gaps

```
# BAD — treating coverage gaps as optional
"No user profile memory (info only, skipping)"

# GOOD — coverage gaps degrade response quality
"No user profile memory detected. Claude doesn't know your role.
 Add one with /memory-capture? This improves tailoring significantly."
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| User runs /memory-health | Full health report + offer auto-fixes |
| Index drift detected | Auto-fix with memory_sync_index (confirm first) |
| Duplicates detected | Run /memory-consolidate for interactive merge |
| Stale project memories | Run /memory-forget to remove or update |
| Missing description fields | Update via memory_store, can't be left blank |
| No memories found at all | Offer /memory-setup to initialize |
| Health score drops below 50 | Escalate to: consolidate → forget → rebuild index |
