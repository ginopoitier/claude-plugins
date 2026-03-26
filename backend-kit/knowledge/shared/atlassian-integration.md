# Atlassian Integration

> Jira and Confluence are owned by dedicated kits:
> - **jira-kit** — sprint work, epics, stories, standup
> - **confluence-kit** — SDLC, ADRs, SDRs, work documentation
>
> Both use the **Atlassian MCP** via OAuth. Authenticate once:
> ```
> /mcp authenticate atlassian
> ```
> The same session covers both Jira and Confluence.

## When backend-kit reads Atlassian data

Some backend-kit skills read Jira or Confluence as a side-effect of their work:

| Skill | What it reads | Config it needs |
|-------|--------------|-----------------|
| `/review` | Jira ticket ACs (when ticket key is in branch name) | `JIRA_PROJECT_KEY` from `.claude/jira.config.md` |
| `/pr-prep` | Jira ticket ACs and description | `JIRA_PROJECT_KEY` from `.claude/jira.config.md` |
| `/sdlc-check` | SDLC pages from Confluence | `SDLC_CONFLUENCE_SPACE` + `SDLC_PARENT_PAGE` from `~/.claude/confluence-kit.config.md` |

These skills use `mcp__atlassian__*` tools directly — they do not need any Atlassian config in `~/.claude/kit.config.md`.

## MCP Tool Reference

### Jira

```
mcp__atlassian__jira_get_issue          issueIdOrKey: "ORD-123"
mcp__atlassian__jira_search_issues      jql: "project = ORD AND ..."
mcp__atlassian__jira_create_issue       projectKey, summary, issueType, description
mcp__atlassian__jira_update_issue       issueIdOrKey, fields: {...}
mcp__atlassian__jira_transition_issue   issueIdOrKey, transitionName: "In Progress"
mcp__atlassian__jira_add_comment        issueIdOrKey, comment: "..."
```

### Confluence

```
mcp__atlassian__confluence_search       query, spaceKey
mcp__atlassian__confluence_get_page     pageId
mcp__atlassian__confluence_create_page  spaceKey, title, parentId, content
mcp__atlassian__confluence_update_page  pageId, title, content, version
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `mcp__atlassian__*` tools not available | `/mcp status` — check atlassian server is connected |
| Auth error | Re-run `/mcp authenticate atlassian` |
| Wrong Jira project | Check `JIRA_PROJECT_KEY` in `.claude/jira.config.md` |
| Confluence page not found | Use `confluence_search` to find the correct page ID first |
