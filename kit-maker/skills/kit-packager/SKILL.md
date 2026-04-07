---
name: kit-packager
description: >
  Package a Claude Code kit for distribution and marketplace publishing.
  Validates plugin.json, updates marketplace.json, ensures the kit is
  installable via /plugin install, and performs a pre-release health check.
  Load this skill when: "publish kit", "package kit", "distribute kit",
  "share kit", "kit release", "marketplace", "kit version", "release kit".
user-invocable: true
argument-hint: "[kit path and version bump: major|minor|patch]"
allowed-tools: Read, Write, Edit, Bash, Glob
---

# Kit Packager

## Core Principles

1. **Installability is the only metric that matters** — A beautiful kit that can't be installed by a new user is worthless. Validate before publishing.
2. **plugin.json is the manifest** — Everything Claude Code needs to install a kit comes from `{kit}/.claude-plugin/plugin.json`. No `install.sh`, no `kit.manifest.json`.
3. **marketplace.json is the catalog** — One file at the repo root `.claude-plugin/marketplace.json` lists all kits. Version numbers must stay in sync between `plugin.json` and `marketplace.json`.
4. **Semantic versioning** — `MAJOR.MINOR.PATCH`. Breaking changes = major. New skills/rules/agents = minor. Fixes/wording = patch.
5. **Health gate** — Run `/kit-health-check` before packaging. GPA < 3.0 = fix first.

## Patterns

### Pre-Package Checklist

Before bumping a version, verify:

```bash
# 1. Kit health check passes (GPA ≥ 3.0)
# 2. All SKILL.md files have complete frontmatter (name, description, user-invocable, allowed-tools)
# 3. All @-references in CLAUDE.md point to valid installed paths
# 4. All skill directories referenced in CLAUDE.md actually exist on disk
# 5. No hardcoded absolute paths in any file (no C:/, D:/, /home/user/)
# 6. Config template has no user-specific values filled in
# 7. README.md Install section uses /plugin install, not bash install.sh
```

### plugin.json — Per-Kit Manifest

```json
{
  "name": "kit-name",
  "version": "MAJOR.MINOR.PATCH",
  "description": "One sentence: what this kit does and who it helps",
  "author": {
    "name": "Author Name",
    "email": "author@example.com"
  },
  "license": "MIT",
  "keywords": ["domain", "technology", "use-case"],
  "commands": "./skills/",
  "mcpServers": {
    "server-name": {
      "type": "stdio",
      "command": "server-binary-name"
    }
  }
}
```

`mcpServers` is optional — omit for kits with no local MCP server.
OAuth-based MCPs (like Atlassian) are declared in `marketplace.json` via `"requires"`, not here.

### marketplace.json — Repo Root Catalog

Update the matching plugin entry in `.claude-plugin/marketplace.json`:

```json
{
  "plugins": [
    {
      "name": "kit-name",
      "source": "./kit-name",
      "description": "One sentence description",
      "version": "MAJOR.MINOR.PATCH",
      "author": { "name": "Author Name" },
      "license": "MIT",
      "keywords": ["domain", "technology"],
      "category": "development",
      "requires": ["atlassian-mcp-oauth"]
    }
  ]
}
```

`requires` is optional — only for OAuth dependencies like Atlassian.
`category` values: `development`, `productivity`, `tooling`, `data`, `security`.

### Version Bump Flow

```bash
# 1. Review scope of changes
git diff --stat

# Determine bump level:
# Removed/renamed user-facing skill or config key → MAJOR
# New skill, rule, knowledge doc, or agent added   → MINOR
# Bug fix, wording, trigger keyword added          → PATCH

# 2. Update plugin.json
Edit: {kit}/.claude-plugin/plugin.json → bump "version"

# 3. Update marketplace.json (must match)
Edit: .claude-plugin/marketplace.json → bump matching plugin entry "version"

# 4. Update root README.md version table
Edit: README.md → update version column for this kit

# 5. Stage all three together
git add {kit}/.claude-plugin/plugin.json
git add .claude-plugin/marketplace.json
git add README.md
```

### README.md Install Section

Every kit README must use the plugin install mechanism:

```markdown
## Install

/plugin marketplace add ginopoitier/claude-plugins
/plugin install kit-name@ginopoitier-plugins

Then run `/kit-setup` in Claude Code.
```

Never use `bash install.sh` — there is no install.sh.

### Validate Installability

Before publishing, verify the kit structure is clean:

```bash
# Check plugin.json is valid JSON
python3 -c "import json; json.load(open('{kit}/.claude-plugin/plugin.json'))" && echo "Valid"

# Check all SKILL.md files have required frontmatter fields
grep -rL "user-invocable:" {kit}/skills/*/SKILL.md

# Check no hardcoded absolute paths
grep -r "C:/\|D:/\|/home/" {kit}/skills/ {kit}/rules/ {kit}/CLAUDE.md

# Verify version sync between plugin.json and marketplace.json
python3 -c "
import json
p = json.load(open('{kit}/.claude-plugin/plugin.json'))['version']
m = next(x['version'] for x in json.load(open('.claude-plugin/marketplace.json'))['plugins'] if x['name'] == '{kit}')
print('SYNC OK' if p == m else f'MISMATCH: plugin.json={p}, marketplace.json={m}')
"
```

## Anti-patterns

### Using install.sh or kit.manifest.json

```
# BAD — obsolete distribution mechanism
bash install.sh
kit.manifest.json with "install": { "rules": "~/.claude/rules/..." }

# GOOD — Claude Code native plugin system
/plugin install kit-name@ginopoitier-plugins
plugin.json + marketplace.json
```

### Version Sync Drift

```
# BAD — bumped plugin.json but forgot marketplace.json
plugin.json: "version": "1.2.0"
marketplace.json: "version": "1.1.0"  ← stale

# GOOD — always update both in the same commit
# And update README.md version table too
```

### Shipping Without Health Check

```
# BAD — "looks good to me, let's publish"
→ Users install kit → skills don't load → trigger keywords wrong → bad experience

# GOOD — gate on health check
Run /kit-health-check → GPA ≥ 3.0 → then package
Any D/F grades = fix first
```

### Hardcoded Absolute Paths

```
# BAD — breaks on any machine except the author's
@~/.claude/rules/kit-name/my-rule.md  ← fine
C:/Users/ginop/.claude/rules/...      ← hardcoded, breaks everywhere else

# GOOD — use the installed path convention
@~/.claude/rules/kit-name/my-rule.md
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| New skill added | MINOR version bump; update plugin.json + marketplace.json + README |
| Bug fix / wording | PATCH bump; update both files |
| Renamed a user-facing skill | MAJOR bump; add migration note to README |
| MCP server dependency | Add to `mcpServers` in plugin.json |
| OAuth MCP (Atlassian) | Add `"requires"` to marketplace.json entry |
| Health check GPA < 3.0 | Fix issues first, don't publish |
| Version mismatch detected | Fix marketplace.json to match plugin.json |

## Deep Reference

For full marketplace spec, plugin.json schema, and distribution guidelines:
@~/.claude/knowledge/kit-maker/marketplace-spec.md

## Execution

1. Parse `$ARGUMENTS` — extract kit path and version bump level (`major`, `minor`, `patch`)
2. Run `/kit-health-check` on the kit — if GPA < 3.0, stop and list issues to fix first
3. Run the Pre-Package Checklist: valid JSON, complete frontmatter, no hardcoded paths, README uses `/plugin install`
4. Determine bump level from `git diff --stat` and bump policy (breaking = major, new skill/rule = minor, fix = patch)
5. Update `{kit}/.claude-plugin/plugin.json` version
6. Update matching entry in `.claude-plugin/marketplace.json` version (must match exactly)
7. Update version table row in `README.md`
8. Verify version sync: confirm `plugin.json` == `marketplace.json` == `README.md`
9. Stage all three files together: `git add {kit}/.claude-plugin/plugin.json .claude-plugin/marketplace.json README.md`
10. Report: "Ready to commit. Version bumped to {new-version}."

$ARGUMENTS
