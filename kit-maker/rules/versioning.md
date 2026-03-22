# Kit Versioning

Kits use semantic versioning: `MAJOR.MINOR.PATCH`. Version is declared in
`.claude-plugin/plugin.json` and mirrored in `.claude-plugin/marketplace.json`.
**Always bump before committing changes to a kit.**

## Version Bump Rules

| Change Type | Bump | Examples |
|-------------|------|---------|
| Removed or renamed a skill/rule that users invoke | **MAJOR** | Deleting `/scaffold-kit`, renaming `/review` to `/code-review` |
| Breaking change to CLAUDE.md structure or config keys | **MAJOR** | Renaming config keys, changing install paths |
| New skill, rule, knowledge doc, or agent added | **MINOR** | Adding `/new-skill`, new rule file, new knowledge doc |
| Existing skill extended with new patterns/sections | **MINOR** | Adding a new ## Pattern to an existing skill |
| Bug fix, wording correction, trigger keyword added | **PATCH** | Fixing a broken example, adding a trigger phrase |
| Meta-skill updates (copied from another kit) | **PATCH** | Syncing context-discipline from kit-maker |
| Version or dependency bumps in plugin.json only | **PATCH** | Updating mcpServers URL |

## DO
- Bump the version before staging the commit — not after
- Update **both** `plugin.json` and `marketplace.json` when bumping
- Use `git diff --stat` to review all changed files before deciding the bump level
- When in doubt between MINOR and PATCH, use MINOR

## DON'T
- Don't ship two consecutive commits with the same version
- Don't bump MAJOR for additive changes — adding skills is always MINOR
- Don't forget `marketplace.json` — mismatched versions between files cause install confusion

## Pre-Commit Version Bump Checklist

Before every `git commit` on a kit:

```
1. git diff --stat                         # review scope of changes
2. Determine bump level from the table above
3. Update version in .claude-plugin/plugin.json
4. Update version in .claude-plugin/marketplace.json (matching entry)
5. git add .claude-plugin/                 # stage both files
6. Proceed with commit
```

## Examples

```
# Added 2 new skills → MINOR
0.3.1 → 0.4.0

# Fixed a broken example in a skill → PATCH
0.4.0 → 0.4.1

# Removed marketplace skill (breaking) → MAJOR
0.4.1 → 1.0.0

# Fixed MCP config + added git-kit to marketplace → PATCH
1.1.0 → 1.1.1
```
