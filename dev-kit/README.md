# Dev Kit

Production-ready .NET Clean Architecture + Vue/TypeScript developer toolkit for Claude Code.

## What's Included

### Skills (51)

| Category | Skills |
|----------|--------|
| **Code Generation** | `/scaffold`, `/ddd`, `/vertical-slice`, `/signalr-hub` |
| **Analysis & Quality** | `/verify`, `/dotnet-health-check`, `/health-check`, `/80-20-review`, `/de-sloppify`, `/convention-learner`, `/security-scan`, `/dependency-audit`, `/domain-event-map`, `/testing` |
| **Infrastructure & Patterns** | `/caching`, `/configuration`, `/dependency-injection`, `/authentication`, `/messaging`, `/resilience`, `/httpclient-factory`, `/opentelemetry`, `/aspire` |
| **API Design** | `/openapi`, `/scalar`, `/error-handling`, `/modern-csharp` |
| **Database** | `/sqlserver`, `/migration-workflow` |
| **Documentation** | `/adr` |
| **DevOps** | `/docker`, `/ci-cd`, `/container-publish` |
| **Session & Workflow** | `/session-management`, `/workflow-mastery`, `/wrap-up-ritual` |
| **Diagnostics** | `/seq-dig` |
| **Project Setup** | `/dotnet-init`, `/project-setup`, `/project-structure`, `/architecture-advisor` |
| **Kit** | `/kit-setup`, `/marketplace` |
| **Meta (auto-active)** | `instinct-system`, `self-correction-loop`, `autonomous-loops`, `learning-log` |

### Agents (11)

| Agent | Role |
|-------|------|
| `api-designer` | Minimal API endpoints, routing, OpenAPI |
| `dotnet-architect` | Architecture decisions, layer design, CQRS structure |
| `ef-core-specialist` | EF Core entity configs, migrations, query optimization |
| `test-engineer` | xUnit tests, Testcontainers, WebApplicationFactory |
| `vue-expert` | Vue 3 components, Pinia stores, SignalR, TailwindCSS |
| `build-error-resolver` | Diagnose and fix build/compiler errors |
| `code-reviewer` | Full code review for correctness, security, architecture |
| `devops-engineer` | CI/CD pipeline files, Docker, deployment scaffolding |
| `performance-analyst` | N+1 queries, missing indexes, caching opportunities |
| `refactor-cleaner` | Technical debt, modern C# idioms, complexity reduction |
| `security-auditor` | OWASP, auth/authz, secrets, vulnerability scanning |

### MCP Integrations

| Service | MCP | Purpose |
|---------|-----|---------|
| **Dev Kit** | `devkit-mcp` (dotnet tool) | Roslyn C# analysis, SQL Server diagnostics, Neo4j graph queries |
| Jira | Atlassian Remote MCP | Search issues, create tickets, transition status, add comments |
| Confluence | Atlassian Remote MCP | Create and update documentation pages |
| Bitbucket | REST API | Create PRs, add comments (Bitbucket not in Atlassian MCP) |

## Stack

- **Backend:** C# / .NET — Clean Architecture · MediatR CQRS · Minimal APIs · EF Core · Serilog + Seq
- **Frontend:** Vue 3 · TypeScript · Vite · TailwindCSS · Pinia · SignalR
- **Databases:** SQL Server · Neo4j
- **CI/CD (home):** GitHub Actions
- **CI/CD (work):** TeamCity · Octopus Deploy
- **Project Management:** Jira (via Atlassian MCP)
- **Documentation:** Confluence (via Atlassian MCP) · Obsidian

## Install

### Via Claude Code plugin system (recommended)

First, add your marketplace:

```
/plugin marketplace add ginopoitier/claude-plugins
```

Then install dev-kit:

```
/plugin install dev-kit@ginopoitier-plugins
```

Preview before installing:

```
/plugin install dev-kit@ginopoitier-plugins --dry-run
```

### Direct install (local development)

Clone the repo and install the plugin locally:

```bash
git clone https://github.com/ginopoitier/claude-plugins.git
/plugin install ./claude-plugins/dev-kit
```

### Validate the plugin (local development)

```
/plugin validate .
```

## First-Time Setup

### 1. Run kit-setup

```
/kit-setup
```

Walks through: identity · VCS host · CI/CD provider · project management · documentation · local dev infrastructure.

### 2. Install the MCP server

```bash
dotnet tool install -g DevKit.Mcp
```

This installs `devkit-mcp` globally. The plugin wires it up automatically via `mcpServers` in `plugin.json`. Provides Claude with Roslyn-powered C# analysis, SQL Server diagnostic queries, and Neo4j graph access.

To update later:

```bash
dotnet tool update -g DevKit.Mcp
```

### 3. Set secret environment variables

Only Bitbucket token needed — Atlassian auth is handled by MCP OAuth.

**Windows (CMD):**

```cmd
setx BITBUCKET_API_TOKEN  "your-bitbucket-token"   (work only — if VCS_HOST=bitbucket)
setx SEQ_API_KEY          "your-seq-key"            (if Seq auth enabled)
```

**bash/zsh (`~/.bashrc` or `~/.zshrc`):**

```bash
export BITBUCKET_API_TOKEN="your-bitbucket-token"
export SEQ_API_KEY="your-seq-key"
```

Get your Bitbucket token: Bitbucket Settings → Personal access tokens

### 4. Authenticate Atlassian MCP (work machines only)

```
/mcp authenticate atlassian
```

Opens a browser to complete the Atlassian OAuth flow. Persists across sessions. Gives Claude access to Jira and Confluence tools.

Verify:
```
/mcp status
```

### 5. Restart Claude Code

Restart after setting environment variables.

### 6. Configure a project

Inside any repo:

```
/project-setup
```

Creates `.claude/project.config.md` with project-specific settings (stack, namespace, Jira key, Confluence space).

## Two-Level Config System

| Level | File | Contains |
|-------|------|----------|
| Device | `~/.claude/kit.config.md` | VCS host, CI/CD URLs, Obsidian vault, Seq URL, default namespace |
| Project | `.claude/project.config.md` | Project name, namespace, architecture, Jira key, Confluence space, CI project IDs |

Run `/kit-setup` once per machine. Run `/project-setup` once per repo.

## CI/CD Approach

The `/ci-cd` skill **generates pipeline config files** — it never calls CI/CD APIs directly:

- Writes `.github/workflows/*.yml` → commit → GitHub Actions picks it up
- Writes `.teamcity/settings.kts` → commit → TeamCity detects and applies it
- Writes `appsettings.octopus.json` → commit → Octopus uses it on next deploy

## Requirements

- Claude Code 1.0.0+
- .NET 9 SDK (for `dotnet format` hook)
- `jq` (for install.sh hook registration, direct install only)

## License

MIT
