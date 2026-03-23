# Rule: Confluence Page Conventions

## DO
- Always read `CONFLUENCE_SPACE_KEY` from `.claude/confluence.config.md` before creating pages — never hardcode it
- Use `mcp__atlassian__confluence_search` to find a page's ID before updating it — never guess IDs
- Increment the `version` field when calling `confluence_update_page` — fetch the current version first
- Use Confluence storage format (XML) for page content — not Markdown
- Place ADRs under `ADR_PARENT_PAGE` and SDRs under `SDR_PARENT_PAGE` from project config
- Search for existing records before creating new ones to avoid duplicates

## DON'T
- Don't use raw Confluence REST API — always use `mcp__atlassian__confluence_*` tools
- Don't edit an Accepted ADR in place — supersede it with a new ADR and mark the original Deprecated
- Don't store sensitive data (credentials, connection strings) in Confluence pages

## Reading Config

```bash
# User config
CONFLUENCE_BASE_URL=$(grep "^CONFLUENCE_BASE_URL=" ~/.claude/confluence-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SDLC_SPACE=$(grep "^SDLC_CONFLUENCE_SPACE=" ~/.claude/confluence-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SDLC_PAGE=$(grep "^SDLC_PARENT_PAGE=" ~/.claude/confluence-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')

# Project config
CONFLUENCE_SPACE=$(grep "^CONFLUENCE_SPACE_KEY=" .claude/confluence.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SDR_SPACE=$(grep "^SDR_CONFLUENCE_SPACE=" .claude/confluence.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SDR_SPACE=${SDR_SPACE:-$CONFLUENCE_SPACE}
SDR_PARENT=$(grep "^SDR_PARENT_PAGE=" .claude/confluence.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
ADR_PARENT=$(grep "^ADR_PARENT_PAGE=" .claude/confluence.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
```

## Confluence Storage Format Key Elements

```xml
<!-- Heading -->
<h1>Title</h1><h2>Section</h2>

<!-- Paragraph -->
<p>Text here.</p>

<!-- Code block -->
<ac:structured-macro ac:name="code">
  <ac:parameter ac:name="language">csharp</ac:parameter>
  <ac:plain-text-body><![CDATA[your code here]]></ac:plain-text-body>
</ac:structured-macro>

<!-- Info / Warning panel -->
<ac:structured-macro ac:name="info">
  <ac:rich-text-body><p>Note text.</p></ac:rich-text-body>
</ac:structured-macro>

<!-- Table -->
<table><tbody>
  <tr><th>Field</th><th>Value</th></tr>
  <tr><td>Status</td><td>Accepted</td></tr>
</tbody></table>
```
