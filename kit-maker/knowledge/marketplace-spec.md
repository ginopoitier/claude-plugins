# Marketplace Spec — Kit Distribution Format

## Overview

A marketplace-ready kit is self-contained: one directory with everything needed to install, configure, and use it. No external dependencies except Claude Code itself.

## Required Files for Distribution

```
kit-name/
  CLAUDE.md           # Entry point — required
  kit.manifest.json   # Metadata and install paths — required
  install.sh          # One-command installer — required for marketplace
  README.md           # Human-readable documentation — required for marketplace
  [content dirs]      # rules/, skills/, knowledge/, etc.
```

## kit.manifest.json Full Specification

```json
{
  "$schema": "https://claude-kits.dev/manifest/v1",
  "id": "kit-name",                              // kebab-case, globally unique
  "name": "Kit Display Name",                    // Title Case, shown in marketplace
  "version": "1.0.0",                            // semver: MAJOR.MINOR.PATCH
  "description": "One sentence. Who it's for.",  // max 140 chars
  "author": "author-handle",                     // used in attribution
  "homepage": "https://github.com/org/kit-name", // optional
  "license": "MIT",                              // optional, defaults to proprietary
  "tags": ["domain", "technology"],              // 2–5 tags for discovery
  "requires": [],                                // other kit IDs this depends on
  "min_claude_code_version": "1.0.0",            // minimum Claude Code version

  "install": {
    "rules":     "~/.claude/rules/kit-name/",    // rule files go here
    "skills":    "~/.claude/skills/",            // flat namespace
    "knowledge": "~/.claude/knowledge/kit-name/",
    "agents":    "~/.claude/agents/",
    "hooks":     "~/.claude/hooks/kit-name/",
    "templates": "~/.claude/templates/kit-name/"
  },

  "config": "config/kit.config.template.md",    // null if no config needed

  "entrypoint": "CLAUDE.md",                    // file to include in user's CLAUDE.md

  "commands": [                                  // user-invocable slash commands
    "/scaffold-skill",
    "/kit-health-check"
  ],

  "mcp": {                                       // null if no MCP server
    "server": "mcp/publish/KitName.Mcp.dll",
    "env": {
      "REQUIRED_VAR": "description of what to put here"
    }
  }
}
```

## Semantic Versioning Rules

| Change Type | Version Bump | Example |
|-------------|-------------|---------|
| New skill added | MINOR | 1.0.0 → 1.1.0 |
| New rule added | MINOR | 1.0.0 → 1.1.0 |
| Bug fix in skill | PATCH | 1.0.0 → 1.0.1 |
| Skill renamed (breaking) | MAJOR | 1.0.0 → 2.0.0 |
| Rule removed (breaking) | MAJOR | 1.0.0 → 2.0.0 |
| CLAUDE.md structure changed | MAJOR | 1.0.0 → 2.0.0 |

## Tag Vocabulary

Use these standard tags for marketplace discoverability:

**Domain tags:** `dotnet`, `python`, `javascript`, `typescript`, `go`, `rust`, `java`, `data-science`, `devops`, `security`, `mobile`, `ml`

**Use case tags:** `code-generation`, `analysis`, `testing`, `deployment`, `observability`, `documentation`, `refactoring`, `architecture`

**Stack tags:** `azure`, `aws`, `gcp`, `kubernetes`, `docker`, `github`, `gitlab`, `postgres`, `mongodb`

## install.sh Requirements

A compliant install.sh must:
1. Accept `--dry-run` flag (shows what would be installed without making changes)
2. Use `$CLAUDE_CONFIG_DIR` if set, fall back to `$HOME/.claude`
3. Exit with code 0 on success, non-zero on failure
4. Print what it installed (one line per component)
5. NOT require sudo
6. NOT modify any files outside `~/.claude/`

## README.md Required Sections

```markdown
# Kit Name

> One sentence description

## What This Kit Does
[2-3 sentences — the problem it solves and who it's for]

## Skills
| Command | Description |
|---------|-------------|
| /skill-name | what it does |

## Install
```bash
git clone {url}
cd kit-name
bash install.sh
```

## Configure
[what config is needed and how to set it up]

## Requirements
- Claude Code 1.0.0+
- [other requirements]

## License
```

## Namespace Collision Prevention

Skills share a flat namespace (`~/.claude/skills/`). To prevent collisions:

1. Use domain-prefixed names for common concepts:
   - `dotnet-health-check` not `health-check`
   - `python-scaffold` not `scaffold`

2. Check for existing skill names before publishing:
   ```bash
   ls ~/.claude/skills/ | grep "your-skill-name"
   ```

3. Declare your skill names in `kit.manifest.json` `commands` array — marketplace tooling will flag conflicts.

## Update/Upgrade Protocol

When a user has an older version installed:
```bash
# Re-run install.sh — it overwrites with new versions
bash install.sh

# For major version upgrades (breaking changes):
bash install.sh --migrate  # runs migration script if provided
```

Migration scripts live at `migrations/{old-version}-to-{new-version}.sh`.
