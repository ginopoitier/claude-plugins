# Marketplace Spec — Kit Distribution Format

> **Evolved 2026-03-23**: Rewrote to match actual Claude Code plugin system.
> Previous version incorrectly documented `kit.manifest.json` and `install.sh` as required.
> Both are wrong — see below.

## How Distribution Works

All kits live in a **single repository**. Distribution uses Claude Code's native `/plugin install` system:

```
/plugin marketplace add ginopoitier/claude-plugins
/plugin install kit-name@ginopoitier-plugins
```

There is **no `install.sh`** and **no per-kit `kit.manifest.json`**. The two relevant files are:

| File | Location | Purpose |
|------|----------|---------|
| `plugin.json` | `{kit}/.claude-plugin/plugin.json` | Per-kit plugin manifest — Claude Code reads this |
| `marketplace.json` | `{repo-root}/.claude-plugin/marketplace.json` | ONE catalog for the whole repo — lists all kits |

## plugin.json — Per-Kit Manifest

Claude Code reads this to install a kit. One per kit.

```json
{
  "name": "kit-name",
  "version": "1.0.0",
  "description": "One sentence description",
  "author": {
    "name": "Author Name",
    "email": "email@example.com"
  },
  "license": "MIT",
  "keywords": ["tag1", "tag2"],
  "commands": "./skills/",
  "mcpServers": {
    "server-name": {
      "type": "stdio",
      "command": "server-command"
    }
  }
}
```

`mcpServers` is optional — omit if the kit has no MCP dependency.

## marketplace.json — Repo Root Catalog

ONE file at `{repo-root}/.claude-plugin/marketplace.json`. Lists all kits in the repository. Claude Code reads this when a user adds the marketplace.

```json
{
  "name": "author-claude-plugins",
  "owner": {
    "name": "Author Name",
    "email": "author@example.com"
  },
  "metadata": {
    "description": "Brief description of the marketplace",
    "version": "1.3.0"
  },
  "plugins": [
    {
      "name": "kit-name",
      "source": "./kit-name",
      "description": "One sentence — what this kit does and who it's for",
      "version": "1.0.0",
      "author": { "name": "Author Name" },
      "license": "MIT",
      "keywords": ["dotnet", "architecture"],
      "category": "development",
      "requires": ["atlassian-mcp-oauth"]
    }
  ]
}
```

**`category` values:** `development`, `productivity`, `tooling`, `data`, `security`, `mobile`

**`requires`** is optional — use for external prerequisites like OAuth sessions.

## Semantic Versioning Rules

Bump version in **both** `plugin.json` AND the matching entry in the root `marketplace.json`.

| Change Type | Bump | Example |
|-------------|------|---------|
| Skill/rule/agent removed or renamed (breaking) | **MAJOR** | `/scaffold` → `/scaffold-feature` |
| Breaking change to config keys or CLAUDE.md structure | **MAJOR** | Renaming config keys |
| New skill, rule, knowledge doc, or agent added | **MINOR** | Adding `/new-skill` |
| Existing skill extended with new patterns | **MINOR** | New `## Pattern` section |
| Bug fix, wording, trigger keyword added | **PATCH** | Fixing broken example |

**Bump before staging the commit — not after.**

## README.md Required Sections

```markdown
# kit-name

One sentence description.

## What's Included

| Category | Skills |
|----------|--------|
| Category | `/skill-name` — what it does |

## Install

/plugin marketplace add ginopoitier/claude-plugins
/plugin install kit-name@ginopoitier-plugins

Then run `/kit-setup` in Claude Code.

## Configuration

| Level | File | Contains |
|-------|------|----------|
| User / Device | `~/.claude/kit-name.config.md` | ... |
| Project | `.claude/kit.config.md` | ... |

## Requirements

- Claude Code 1.0.0+
- [any other requirements]

## License

MIT
```

## Namespace Collision Prevention

Skills share a flat namespace (`~/.claude/skills/`). To prevent collisions:

1. Use domain-prefixed names for common concepts:
   - `dotnet-health-check` not `health-check`
   - `python-scaffold` not `scaffold`

2. Declare all user-invocable commands in the `commands` field of `plugin.json`.

3. Check for existing skill names: `ls ~/.claude/skills/ | grep "your-skill-name"`

## Kit Structure Reminder

Every kit requires a settings hook to check config. Without it, users won't be prompted to run setup.

```
hooks/
  check-settings.sh     ← exits 0, prints message if config missing
  hooks.json            ← registers check-settings.sh as UserPromptSubmit hook
```

`hooks.json` format:
```json
{
  "hooks": {
    "UserPromptSubmit": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "bash ${CLAUDE_PLUGIN_ROOT}/hooks/check-settings.sh"
          }
        ]
      }
    ]
  }
}
```
