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
/plugin install git-kit@ginopoitier-plugins
```

Update all plugins:

```
/plugin marketplace update
```

## Plugins

| Plugin | Version | Description |
|--------|---------|-------------|
| [`dev-kit`](./dev-kit/) | 0.3.5 | .NET Clean Architecture + Vue/TypeScript developer toolkit |
| [`kit-maker`](./kit-maker/) | 1.0.2 | Build and audit Claude Code plugins |
| [`git-kit`](./git-kit/) | 1.0.1 | Git CLI toolkit — commits, branching, rebase, conflict resolution, undo, repo health |

## Post-install: dev-kit

1. Run `/kit-setup` — configure VCS host, CI/CD provider, documentation target
2. Install the MCP server: `dotnet tool install -g DevKit.Mcp`
3. Run `/mcp authenticate atlassian` — Jira + Confluence access (work machines only)
4. Set `BITBUCKET_API_TOKEN` env var if using Bitbucket
5. Restart Claude Code

## Post-install: kit-maker

1. Run `/kit-setup` — configure author name and kit defaults

## Post-install: git-kit

1. Run `/git-setup` — configure default branch, signing key, commit style
2. Optionally run `/git-setup --project` inside a repo to add project-level git config

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
    hooks/              ← registered automatically on plugin install
  kit-maker/
    .claude-plugin/
      plugin.json       ← kit-maker plugin manifest
    skills/             ← 18 skills (10 user-invokable + 8 meta)
    agents/             ← kit-auditor, skill-writer
    hooks/
  git-kit/
    .claude-plugin/
      plugin.json       ← git-kit plugin manifest
    skills/             ← 17 skills (10 user-invokable + 7 meta)
    agents/             ← git-historian, git-surgeon
    rules/              ← 4 always-active rules
    knowledge/          ← reference docs
    hooks/              ← commit-msg validator, pre-push guard
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
