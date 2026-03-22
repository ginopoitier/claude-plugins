# Atlassian Integration — Jira, Confluence, Bitbucket

Jira and Confluence interactions use the **Atlassian MCP server** (`atlassian`).
Bitbucket interactions use the **Bitbucket REST API** directly (not covered by Atlassian MCP).

## MCP Setup

The Atlassian Remote MCP is registered automatically when the dev-kit plugin is installed.
Authenticate once in Claude Code:

```
/mcp authenticate atlassian
```

On first use Claude Code opens a browser to complete the Atlassian OAuth flow.
After authentication, all `mcp__atlassian__*` tools become available.

Verify it's working:
```
/mcp status
```

### Config Keys Used

From `~/.claude/kit.config.md`:
```
JIRA_BASE_URL=https://mycompany.atlassian.net        # used for building URLs in output
BITBUCKET_WORKSPACE=mycompany
BITBUCKET_API_TOKEN=${BITBUCKET_API_TOKEN}           # SECRET — set in system env
```

From `.claude/project.config.md`:
```
JIRA_PROJECT_KEY=ORD
CONFLUENCE_SPACE_KEY=ORD
VCS_REPO=mycompany/order-service
```

---

## Jira

### Branch and Commit Conventions

Include the Jira ticket key in branch names and commit messages — Jira auto-links them:

```bash
git checkout -b feature/ORD-123-add-order-validation
git commit -m "feat(orders): add FluentValidation to CreateOrder command (ORD-123)"
git commit -m "fix: null reference in order total calculation (ORD-456)"
```

### Jira MCP Tools

Always use MCP tools — never raw HTTP calls to the Jira REST API.

```
# Get issue details
mcp__atlassian__jira_get_issue
  issueIdOrKey: "ORD-123"

# Search issues with JQL
mcp__atlassian__jira_search_issues
  jql: "project = ORD AND assignee = currentUser() AND status != Done"
  maxResults: 20

# Create a new issue
mcp__atlassian__jira_create_issue
  projectKey: "ORD"
  summary: "Add FluentValidation to CreateOrder"
  issueType: "Story"
  description: "Acceptance criteria: ..."
  assignee: "user@company.com"

# Transition issue status
mcp__atlassian__jira_transition_issue
  issueIdOrKey: "ORD-123"
  transitionName: "In Progress"    # or use transition ID

# Add a comment
mcp__atlassian__jira_add_comment
  issueIdOrKey: "ORD-123"
  comment: "PR opened: feature/ORD-123-add-order-validation"

# Update issue fields
mcp__atlassian__jira_update_issue
  issueIdOrKey: "ORD-123"
  fields:
    priority: { name: "High" }
    labels: ["backend", "validation"]
```

### JQL Reference

```jql
-- Open issues in my project
project = ORD AND status != Done ORDER BY priority DESC

-- My open issues
project = ORD AND assignee = currentUser() AND status != Done

-- Current sprint
project = ORD AND sprint in openSprints()

-- Recently updated
project = ORD AND updated >= -7d ORDER BY updated DESC

-- By fix version
project = ORD AND fixVersion = "1.2.0"

-- Unassigned bugs
project = ORD AND issuetype = Bug AND assignee is EMPTY
```

---

## Confluence

### Page Hierarchy Convention

```
Space: ORD (Order Service)
├── Architecture/
│   ├── Overview
│   ├── ADRs/
│   │   ├── ADR-001: Clean Architecture choice
│   │   └── ADR-002: Result pattern
│   └── Data Model
├── Development/
│   ├── Getting Started
│   ├── Local Setup
│   └── Environment Variables
├── Operations/
│   ├── Runbooks/
│   └── Monitoring
└── API/
    └── Endpoint Reference
```

### Confluence MCP Tools

Always use MCP tools — never raw HTTP calls to the Confluence REST API.

```
# Search pages
mcp__atlassian__confluence_search
  query: "order service architecture"
  spaceKey: "ORD"

# Get a page by ID or title
mcp__atlassian__confluence_get_page
  pageId: "12345"
  # or: spaceKey + title combination

# Create a page
mcp__atlassian__confluence_create_page
  spaceKey: "ORD"
  title: "ADR-003: Caching Strategy"
  parentId: "11111"              # parent page ID for ADRs/
  content: "<h1>Status</h1><p>Accepted</p>..."   # Confluence storage format

# Update a page
mcp__atlassian__confluence_update_page
  pageId: "12345"
  title: "ADR-003: Caching Strategy"
  content: "<h1>Status</h1><p>Superseded by ADR-007</p>..."
  version: 3                     # must increment

# List spaces
mcp__atlassian__confluence_get_spaces
  limit: 20
```

### Confluence Storage Format

Confluence uses its own XML-based storage format. Key elements:

```xml
<!-- Heading -->
<h1>Architecture Decision Record</h1>

<!-- Paragraph -->
<p>This is a paragraph.</p>

<!-- Code block -->
<ac:structured-macro ac:name="code">
  <ac:parameter ac:name="language">csharp</ac:parameter>
  <ac:plain-text-body><![CDATA[
public sealed class CreateOrderHandler : IRequestHandler<...>
  ]]></ac:plain-text-body>
</ac:structured-macro>

<!-- Info panel -->
<ac:structured-macro ac:name="info">
  <ac:rich-text-body><p>This ADR is superseded by ADR-007.</p></ac:rich-text-body>
</ac:structured-macro>

<!-- Table -->
<table><tbody>
  <tr><th>Field</th><th>Value</th></tr>
  <tr><td>Status</td><td>Accepted</td></tr>
</tbody></table>
```

---

## Bitbucket

Bitbucket is not covered by the Atlassian MCP — use the REST API directly.

Base URL: `https://api.bitbucket.org/2.0/`
Auth: `Authorization: Bearer {BITBUCKET_API_TOKEN}` (token from system env)

### Bitbucket REST API

```bash
# List open pull requests
curl -H "Authorization: Bearer $BITBUCKET_API_TOKEN" \
  "https://api.bitbucket.org/2.0/repositories/$BITBUCKET_WORKSPACE/$REPO_SLUG/pullrequests?state=OPEN"

# Create a pull request
curl -H "Authorization: Bearer $BITBUCKET_API_TOKEN" \
  -X POST "https://api.bitbucket.org/2.0/repositories/$BITBUCKET_WORKSPACE/$REPO_SLUG/pullrequests" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "feat: add order validation (ORD-123)",
    "description": "Adds FluentValidation to CreateOrder command.\n\nJira: ORD-123",
    "source": {"branch": {"name": "feature/ORD-123-order-validation"}},
    "destination": {"branch": {"name": "main"}},
    "reviewers": [{"uuid": "{reviewer-uuid}"}],
    "close_source_branch": true
  }'

# Add a PR comment
curl -H "Authorization: Bearer $BITBUCKET_API_TOKEN" \
  -X POST "https://api.bitbucket.org/2.0/repositories/$BITBUCKET_WORKSPACE/$REPO_SLUG/pullrequests/$PR_ID/comments" \
  -H "Content-Type: application/json" \
  -d '{"content": {"raw": "Ready for review. CI green."}}'
```

### Branch Naming with Jira Integration

Bitbucket auto-links branches, commits, and PRs to Jira issues when they contain the issue key:

```bash
git checkout -b feature/ORD-123-add-order-validation
git checkout -b fix/ORD-456-null-reference-total
git checkout -b chore/ORD-789-upgrade-ef-core-9
```

---

## Workflow: Feature Development with Atlassian Stack

```
1. Pick ticket               mcp__atlassian__jira_search_issues (sprint + unassigned)
2. Transition to In Progress mcp__atlassian__jira_transition_issue (ORD-123, "In Progress")
3. Create branch             git checkout -b feature/ORD-123-add-order-validation
4. Develop                   Claude Code with dev-kit
5. Commit with ticket key    "feat: add FluentValidation (ORD-123)"
6. Open PR                   Bitbucket REST API or Bitbucket UI
   - PR title includes ORD-123 → Jira auto-links
7. CI runs                   TeamCity triggered by Bitbucket webhook (no Claude involvement)
8. PR approved + merged      TeamCity triggers pack + Octopus push (no Claude involvement)
9. Write ADR (if needed)     mcp__atlassian__confluence_create_page (Architecture/ADRs/)
10. Transition to Done       mcp__atlassian__jira_transition_issue (ORD-123, "Done")
```

---

## Troubleshooting MCP

| Problem | Solution |
|---------|----------|
| `mcp__atlassian__*` tools not available | Run `/mcp status` — check atlassian server is connected |
| Auth error | Re-run `/mcp authenticate atlassian` |
| Wrong Jira project | Check `JIRA_PROJECT_KEY` in `.claude/project.config.md` |
| Confluence page not found | Use `confluence_search` to find the correct page ID first |
| Bitbucket 401 | Check `$BITBUCKET_API_TOKEN` is set in system env (`echo $BITBUCKET_API_TOKEN`) |
