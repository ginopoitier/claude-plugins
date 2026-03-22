# Claude Plugins

Personal Claude Code plugin marketplace. One repository — all plugins.

## Install

Add this marketplace in Claude Code:

```
/plugin marketplace add ginopoitier/claude-plugins
```

Install a plugin:

```
/plugin install dev-kit@ginopoitier-plugins
/plugin install kit-maker@ginopoitier-plugins
```

Update all plugins:

```
/plugin marketplace update
```

## Plugins

| Plugin | Version | Description |
|--------|---------|-------------|
| [`dev-kit`](./dev-kit/) | 0.3.0 | .NET Clean Architecture + Vue/TypeScript developer toolkit |
| [`kit-maker`](./kit-maker/) | 1.0.0 | Build and audit Claude Code plugins |

## Post-install: dev-kit

1. Run `/kit-setup` — configure VCS host, CI/CD provider, documentation target
2. Register hooks — see [HOOKS.md](./HOOKS.md) for the `settings.json` snippets
3. Run `/mcp authenticate atlassian` — Jira + Confluence access (work machines only)
4. Set `BITBUCKET_API_TOKEN` env var if using Bitbucket
5. Restart Claude Code

## Post-install: kit-maker

1. Run `/kit-setup` — configure author name and kit defaults
2. Register hooks — see [HOOKS.md](./HOOKS.md) for the `settings.json` snippets

## Repository structure

```
claude-plugins/
  .claude-plugin/
    marketplace.json    ← marketplace catalog (Claude Code reads this)
  dev-kit/
    .claude-plugin/
      plugin.json       ← dev-kit plugin manifest
    skills/             ← 57 skills
    agents/             ← 11 agents
    rules/              ← 17 always-active rules
    knowledge/          ← reference docs
    hooks/              ← see HOOKS.md for registration
  kit-maker/
    .claude-plugin/
      plugin.json       ← kit-maker plugin manifest
    skills/             ← scaffolding + audit skills
    agents/             ← kit-auditor, skill-writer
    hooks/
  README.md
```

## Add to a team project

To suggest these plugins to everyone who opens a project:

```json
{
  "extraKnownMarketplaces": {
    "ginopoitier-plugins": {
      "source": {
        "source": "github",
        "repo": "ginopoitier/claude-plugins"
      }
    }
  }
}
```

Add that to `.claude/settings.json` in any shared repository.

## Private repository access

For background auto-updates, set a GitHub token:

```bash
# bash/zsh
export GITHUB_TOKEN=ghp_xxxxxxxxxxxxxxxxxxxx

# Windows CMD
setx GITHUB_TOKEN "ghp_xxxxxxxxxxxxxxxxxxxx"
```
