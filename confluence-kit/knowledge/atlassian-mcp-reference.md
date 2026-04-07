# Atlassian MCP Tool Reference

> Quick reference for `mcp__atlassian__*` tools used by confluence-kit and jira-kit skills.
> Authentication: `/mcp authenticate atlassian` — OAuth flow, persists across sessions.

## Confluence Tools

### Search

```
mcp__atlassian__confluence_search({
  query: "architecture decisions",     // CQL query or keyword string
  space: "DEV",                        // optional space key
  limit: 10                            // default 10, max 50
})
```

Returns: array of `{ id, title, url, spaceKey, lastModified, excerpt }`

**CQL examples:**
```
"order validation" AND space = DEV AND type = page
title ~ "ADR-" AND ancestor = "12345"
label = "architecture" AND lastModified >= "2026-01-01"
```

### Get Page

```
mcp__atlassian__confluence_get_page({
  pageId: "12345",                     // page ID (from search results)
  includeMarkdown: true               // return content as markdown (default true)
})
```

Returns: `{ id, title, spaceKey, content, url, version, parentId }`

### Create Page

```
mcp__atlassian__confluence_create_page({
  spaceKey: "DEV",                     // required — space to create in
  title: "ADR-007: Use Result Pattern",
  content: "## Status\nAccepted\n\n## Context\n...",  // markdown
  parentId: "56789"                    // optional — parent page ID
})
```

Returns: `{ id, title, url, version }`

### Update Page

```
mcp__atlassian__confluence_update_page({
  pageId: "12345",
  title: "ADR-007: Use Result Pattern",
  content: "## Status\nAccepted\n\n...",  // full replacement content
  version: 3                           // current version number (required for conflict detection)
})
```

Returns: `{ id, title, url, version }`

### Get Children (sub-pages)

```
mcp__atlassian__confluence_get_children({
  pageId: "12345",
  limit: 50
})
```

Returns: array of `{ id, title, url }`

---

## Jira Tools

### Get Issue

```
mcp__atlassian__jira_get_issue({
  issueKey: "ORD-456"                  // Jira issue key
})
```

Returns: `{ key, summary, description, status, assignee, reporter, labels, priority, components, acceptanceCriteria, storyPoints }`

### Search Issues (JQL)

```
mcp__atlassian__jira_search_issues({
  jql: "project = ORD AND sprint in openSprints() AND assignee = currentUser()",
  fields: ["summary", "status", "priority", "assignee"],
  limit: 50
})
```

Returns: `{ issues: [...], total, startAt }`

**Common JQL patterns:**
```jql
-- Current sprint for a project
project = ORD AND sprint in openSprints()

-- My assigned work
assignee = currentUser() AND status != Done

-- Blockers
priority = Highest AND status != Done

-- Linked to an epic
"Epic Link" = ORD-100

-- Recently updated
project = ORD AND updated >= -7d

-- Specific statuses
project = ORD AND status in ("In Progress", "In Review")
```

### Create Issue

```
mcp__atlassian__jira_create_issue({
  project: "ORD",
  issueType: "Story",                  // Story | Bug | Task | Sub-task | Epic
  summary: "Add order cancellation",
  description: "As a customer, I want...\n\n**Acceptance Criteria:**\n...",
  priority: "Medium",                  // Highest | High | Medium | Low | Lowest
  labels: ["api", "orders"],
  assignee: "user@example.com",        // email or account ID
  storyPoints: 3,
  epicLink: "ORD-100"
})
```

### Update Issue

```
mcp__atlassian__jira_update_issue({
  issueKey: "ORD-456",
  fields: {
    summary: "Updated summary",
    status: "In Progress",             // transitions the status
    assignee: "user@example.com",
    storyPoints: 5
  }
})
```

### Add Comment

```
mcp__atlassian__jira_add_comment({
  issueKey: "ORD-456",
  comment: "PR #123 addresses this. Pending review."
})
```

---

## Config Reading Patterns

```bash
# Confluence — user config
CONFLUENCE_BASE_URL=$(grep "^CONFLUENCE_BASE_URL=" ~/.claude/confluence-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
CONFLUENCE_DEFAULT_SPACE=$(grep "^CONFLUENCE_DEFAULT_SPACE_KEY=" ~/.claude/confluence-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')

# Confluence — project config (preferred)
CONFLUENCE_SPACE_KEY=$(grep "^CONFLUENCE_SPACE_KEY=" .claude/confluence.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
ADR_PARENT_PAGE=$(grep "^ADR_PARENT_PAGE=" .claude/confluence.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
ADR_PARENT_PAGE=${ADR_PARENT_PAGE:-"Architecture Decisions"}

# Jira — user config
JIRA_BASE_URL=$(grep "^JIRA_BASE_URL=" ~/.claude/jira-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SPRINT_DURATION=$(grep "^SPRINT_DURATION_DAYS=" ~/.claude/jira-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SPRINT_DURATION=${SPRINT_DURATION:-14}

# Jira — project config
JIRA_PROJECT_KEY=$(grep "^JIRA_PROJECT_KEY=" .claude/jira.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
```

---

## Error Handling

| Error | Cause | Resolution |
|-------|-------|-----------|
| `Unauthorized` | Not authenticated | Run `/mcp authenticate atlassian` |
| `Page not found` | Wrong page ID | Verify ID from search results |
| `Version conflict` | Stale version number | Re-fetch page to get current version |
| `Space not found` | Wrong space key | Check with `mcp__atlassian__confluence_search` |
| `Field not on screen` | Jira field config | Use `mcp__atlassian__jira_get_issue` to check available fields |

---

## Confluence Page Hierarchy

Skills navigate the Confluence hierarchy using parent IDs:

```
Space (key: DEV)
└── Architecture (parent page)
    ├── ADR-001: Use Clean Architecture
    ├── ADR-002: Use CQRS with MediatR
    └── ADR-007: Use Result Pattern

└── Software Decision Records
    ├── SDR-2026-01: Upgrade to .NET 10
    └── SDR-2026-02: Migrate to Minimal APIs
```

To find a parent page ID:
```
result = mcp__atlassian__confluence_search({
  query: "Architecture Decisions",
  space: "DEV"
})
parentId = result.pages[0].id
```
