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
/plugin install github-kit@ginopoitier-plugins
/plugin install bitbucket-kit@ginopoitier-plugins
/plugin install jira-kit@ginopoitier-plugins
/plugin install confluence-kit@ginopoitier-plugins
/plugin install obsidian-kit@ginopoitier-plugins
```

Update all plugins:

```
/plugin marketplace update
```

## Plugins

| Plugin | Version | Description |
|--------|---------|-------------|
| [`dev-kit`](./dev-kit/) | 0.3.6 | .NET Clean Architecture + Vue/TypeScript developer toolkit |
| [`kit-maker`](./kit-maker/) | 1.0.3 | Build and audit Claude Code plugins |
| [`git-kit`](./git-kit/) | 1.0.2 | Git CLI toolkit — commits, branching, rebase, conflict resolution, undo, repo health |
| [`github-kit`](./github-kit/) | 1.0.1 | GitHub platform — PR creation, releases (home machine) |
| [`bitbucket-kit`](./bitbucket-kit/) | 1.0.1 | Bitbucket platform — PR creation and review (work machine) |
| [`jira-kit`](./jira-kit/) | 1.0.1 | Jira sprint work — epics, stories, refinement, standup |
| [`confluence-kit`](./confluence-kit/) | 1.0.1 | Confluence docs — ADRs, SDRs, SDLC pages |
| [`obsidian-kit`](./obsidian-kit/) | 1.0.1 | Obsidian personal notes and dev journal |

## Typical install combinations

**Home machine (GitHub):**
```
dev-kit + git-kit + github-kit + kit-maker + obsidian-kit
```

**Work machine (Bitbucket + Atlassian):**
```
dev-kit + git-kit + bitbucket-kit + jira-kit + confluence-kit + obsidian-kit
```

## Post-install setup

### dev-kit
1. Run `/kit-setup` — configure CI/CD provider, documentation targets
2. Install the MCP server: `dotnet tool install -g DevKit.Mcp`
3. Restart Claude Code

### git-kit
1. Run `/git-setup` — configure default branch, signing key, commit style
2. Optionally run `/git-setup --project` inside each repo

### github-kit
1. Install `gh` CLI and authenticate: `gh auth login`
2. Run `/github-setup` — configure org and PR defaults

### bitbucket-kit
1. Create a Personal Access Token in Bitbucket settings
2. Set env var: `setx BITBUCKET_API_TOKEN "your-token"` (Windows) or add to `~/.bashrc`
3. Run `/bitbucket-setup` — configure workspace and PR defaults

### jira-kit + confluence-kit
1. Run `/mcp authenticate atlassian` — one OAuth session covers both
2. Run `/jira-setup` and `/confluence-setup`

### obsidian-kit
1. Run `/obsidian-setup` — configure vault path and folder structure

### kit-maker
1. Run `/kit-setup` — configure author name and kit defaults

## Repository structure

```
claude-plugins/
  .claude-plugin/
    marketplace.json    ← marketplace catalog (Claude Code reads this)
  dev-kit/
    .claude-plugin/
      plugin.json       ← dev-kit plugin manifest
    skills/             ← 44 user-invokable + 7 meta skills
    agents/             ← 12 agents
    rules/              ← 17 always-active rules
    knowledge/          ← reference docs
    hooks/              ← registered automatically on plugin install
  kit-maker/
    .claude-plugin/
      plugin.json
    skills/             ← 10 user-invokable + 8 meta skills
    agents/             ← kit-auditor, skill-writer
    hooks/
  git-kit/
    .claude-plugin/
      plugin.json
    skills/             ← 10 user-invokable + 7 meta skills
    agents/             ← git-historian, git-surgeon
    rules/              ← 4 always-active rules
    knowledge/          ← reference docs
    hooks/              ← commit-msg validator, pre-push guard
  github-kit/
    .claude-plugin/
      plugin.json
    skills/             ← /pr, /release, /github-setup
    rules/
    hooks/
  bitbucket-kit/
    .claude-plugin/
      plugin.json
    skills/             ← /pr, /bitbucket-setup
    rules/
    hooks/
  jira-kit/
    .claude-plugin/
      plugin.json
    skills/             ← /epic, /story, /tech-refinement, /standup, /jira-setup
    rules/
    hooks/
  confluence-kit/
    .claude-plugin/
      plugin.json
    skills/             ← /adr, /sdr, /confluence-setup
    rules/
    hooks/
  obsidian-kit/
    .claude-plugin/
      plugin.json
    skills/             ← /note, /obsidian-setup
    rules/
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
