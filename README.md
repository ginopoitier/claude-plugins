# Claude Plugins

Personal Claude Code plugin marketplace. One repository — all plugins.

## Install

Add this marketplace in Claude Code:

```
/plugin marketplace add ginopoitier/claude-plugins
```

Install a plugin:

```
/plugin install dotnet-kit@ginopoitier-plugins
/plugin install vue-kit@ginopoitier-plugins
/plugin install kit-maker@ginopoitier-plugins
/plugin install git-kit@ginopoitier-plugins
/plugin install github-kit@ginopoitier-plugins
/plugin install bitbucket-kit@ginopoitier-plugins
/plugin install jira-kit@ginopoitier-plugins
/plugin install confluence-kit@ginopoitier-plugins
/plugin install obsidian-kit@ginopoitier-plugins
/plugin install memory-kit@ginopoitier-plugins
```

Update all plugins:

```
/plugin marketplace update
```

## Plugins

| Plugin | Version | Description |
|--------|---------|-------------|
| [`dotnet-kit`](./dotnet-kit/) | 0.3.2 | .NET Clean Architecture developer toolkit — CQRS, EF Core, Minimal APIs, Serilog, CI/CD |
| [`vue-kit`](./vue-kit/) | 0.2.0 | Vue 3 + TypeScript developer toolkit — Pinia, SignalR, TailwindCSS, Vite |
| [`kit-maker`](./kit-maker/) | 1.2.0 | Build and audit Claude Code plugins |
| [`git-kit`](./git-kit/) | 1.0.3 | Git CLI toolkit — commits, branching, rebase, conflict resolution, undo, repo health |
| [`github-kit`](./github-kit/) | 1.1.0 | GitHub platform — PR creation, review, releases (home machine) |
| [`bitbucket-kit`](./bitbucket-kit/) | 1.2.0 | Bitbucket platform — PR creation, review, and releases (work machine) |
| [`jira-kit`](./jira-kit/) | 1.2.0 | Jira sprint work — epics, stories, refinement, standup |
| [`confluence-kit`](./confluence-kit/) | 1.1.0 | Confluence docs — ADRs, SDRs, SDLC pages |
| [`obsidian-kit`](./obsidian-kit/) | 1.2.0 | Obsidian personal notes and dev journal |
| [`memory-kit`](./memory-kit/) | 1.0.0 | Intelligent memory management — semantic search, auto-capture, deduplication, session injection |

## Typical install combinations

**Full-stack .NET + Vue (home, GitHub):**
```
dotnet-kit + vue-kit + git-kit + github-kit + kit-maker + obsidian-kit
```

**Full-stack .NET + Vue (work, Bitbucket + Atlassian):**
```
dotnet-kit + vue-kit + git-kit + bitbucket-kit + jira-kit + confluence-kit + obsidian-kit
```

**.NET only:**
```
dotnet-kit + git-kit + github-kit (or bitbucket-kit)
```

**Frontend only:**
```
vue-kit + git-kit + github-kit (or bitbucket-kit)
```

## Post-install setup

### dotnet-kit
1. Run `/kit-setup` — configure CI/CD provider, documentation targets
2. Install the MCP server: `dotnet tool install -g DevKit.Mcp --add-source G:/Claude/Kits/MCP/DotNet`
   Or when published to NuGet: `dotnet tool install -g DevKit.Mcp`
3. Restart Claude Code

### vue-kit
1. Run `/kit-setup` — configure project defaults
2. Install the MCP server: `npm install -g @ginopoitier/vue-mcp`
   Or build from source:
   ```bash
   cd G:/Claude/Kits/MCP/Vue/vue-mcp
   npm install && npm run build
   ```
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

### memory-kit
1. Install the MCP server: `npm install -g @ginopoitier/memory-mcp`
2. Restart Claude Code

## MCP Servers

| MCP | Language | Powers | Source |
|-----|----------|--------|--------|
| `devkit-mcp` | .NET 9 | dotnet-kit — Roslyn analysis, SQL Server, Neo4j | `G:/Claude/Kits/MCP/DotNet/` |
| `vue-mcp` | Node.js / TypeScript | vue-kit — Vue SFC analysis, Pinia, type checking | `G:/Claude/Kits/MCP/Vue/` |
| `memory-mcp` | Node.js / TypeScript | memory-kit — semantic search, deduplication, health monitoring | `G:/Claude/Kits/MCP/Memory/` |

## Repository structure

```
claude-plugins/
  MCP/
    DotNet/
      DotNet.Mcp.sln          ← .NET solution for devkit-mcp global tool
      DevKit.Mcp/             ← Roslyn, SQL Server, Neo4j MCP tools
      publish/                ← pre-built binaries
    Vue/
      vue-mcp/                ← TypeScript MCP server for vue-kit
        src/tools/            ← component-analyzer, pinia-analyzer, type-checker, project-analyzer
  Kits/
    README.md                 ← this file
    dotnet-kit/
      .claude-plugin/plugin.json
      skills/                 ← 50+ skills (backend + shared meta)
      agents/                 ← 15 agents (11 domain + 4 dev workflow)
      rules/                  ← 15 rules
      knowledge/              ← dotnet/, decisions/, tech-lead/, shared/
      templates/              ← web-api/, worker-service/, blazor-app/, class-library/, modular-monolith/
      hooks/                  ← auto-format .cs, NuGet restore, bash guard
    vue-kit/
      .claude-plugin/plugin.json
      skills/                 ← 13+ skills (vue + shared meta)
      agents/                 ← 9 agents (5 domain + 4 dev workflow)
      rules/                  ← 8 rules
      knowledge/              ← vue/, shared/
      templates/              ← vue-app/
      hooks/                  ← auto-format .ts/.vue with Prettier, bash guard
    git-kit/
    github-kit/
    bitbucket-kit/
    jira-kit/
    confluence-kit/
    obsidian-kit/
    kit-maker/
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
