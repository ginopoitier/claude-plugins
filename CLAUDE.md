# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository overview

Personal Claude Code plugin marketplace — a mono-repo containing all Claude Code kits (plugins) plus two MCP servers.

```
/ (repo root = G:/Claude/Kits/)
  CLAUDE.md
  README.md                    ← marketplace catalog and install instructions
  .claude-plugin/
    marketplace.json           ← Claude Code marketplace catalog
  .github/
    workflows/
      vue-mcp-ci.yml
      vue-mcp-publish.yml
  MCP/
    DotNet/                    ← .NET MCP server (Roslyn, SQL Server, Neo4j)
      DotNet.Mcp.sln
      DevKit.Mcp/
      publish/                 ← pre-built binaries for local install
    Vue/                       ← TypeScript MCP server (Vue SFC, Pinia, type checking)
      vue-mcp/                 ← Node.js project (@ginopoitier/vue-mcp on npm)
        src/tools/
  backend-kit/                 ← .NET Clean Architecture toolkit
  vue-kit/                     ← Vue 3 + TypeScript toolkit
  git-kit/                     ← Git CLI toolkit
  github-kit/                  ← GitHub PR / release (home machine)
  bitbucket-kit/               ← Bitbucket PR (work machine)
  kit-maker/                   ← Meta-kit for building kits
  jira-kit/                    ← Jira sprint workflow
  confluence-kit/              ← Confluence ADRs, SDRs
  obsidian-kit/                ← Personal Obsidian notes
```

## Kit structure convention

Every kit follows the same layout:

```
{kit-name}/
  CLAUDE.md                    ← loaded into Claude's context when the kit is active
  AGENTS.md                    ← agent roster and routing table (where present)
  .claude-plugin/
    plugin.json                ← required for /plugin install (name, version, commands, mcpServers)
  skills/
    {skill-name}/
      SKILL.md                 ← skill definition (frontmatter + instructions)
  rules/                       ← always-active rules loaded into Claude's context
  knowledge/                   ← deep reference docs, not always loaded
  agents/                      ← specialized agent definitions
  hooks/                       ← shell scripts registered at install time
  config/                      ← config templates (*.template.md)
  templates/                   ← code generation templates (where applicable)
```

### SKILL.md frontmatter schema

```markdown
---
name: {skill-name}
description: >
  {one-line description}.
  Load this skill when: "{keyword1}", "{keyword2}", ...
user-invocable: true|false
argument-hint: "[{hint}]"
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
---
```

## MCP servers

### DotNet MCP — `MCP/DotNet/`

Powers **backend-kit**. Packaged as .NET global tool `devkit-mcp`.

**Build and run:**
```bash
cd MCP/DotNet/DevKit.Mcp
dotnet build

# Run locally against a specific solution
dotnet run -- --solution /path/to/project.sln

# Publish self-contained binaries
dotnet publish -c Release -o ../publish

# Install as global tool from local source
dotnet pack && dotnet tool install -g --add-source ./nupkg DevKit.Mcp
```

**Architecture:**
- `Program.cs` — registers stdio transport, MSBuildLocator (must be first), auto-discovers `[McpServerToolType]` classes
- `Services/` — `RoslynWorkspaceService` (LRU compilation cache, lazy loading), `Neo4jService`, `SqlServerService`
- `Tools/Roslyn/` — 13 tools: antipatterns, architecture, complexity, coupling, dead code, diagnostics, documentation, find-symbol, performance, project-graph, public-api, security, test-quality
- `Tools/DotNet/` — NuGet audit
- `Tools/Neo4j/`, `Tools/SqlServer/` — graph and database diagnostics

**Critical:** `MSBuildLocator.RegisterDefaults()` is called before any Roslyn types JIT-compile. All logging goes to `stderr`; `stdout` is reserved for MCP JSON-RPC.

### Vue MCP — `MCP/Vue/vue-mcp/`

Powers **vue-kit**. Node.js / TypeScript MCP server.

**Build and run:**
```bash
cd MCP/Vue/vue-mcp
npm install
npm run build

# Run against a specific Vue project
node dist/index.js --project /path/to/vue-project
```

**Tools:** `analyze_vue_components`, `find_vue_component`, `validate_pinia_stores`, `get_vue_type_errors`, `get_vue_composables`, `get_vue_project_structure`, `find_missing_api_types`

**Dependencies:** `@modelcontextprotocol/sdk`, `@vue/compiler-sfc`, `glob`

## Shared skills (in both backend-kit and vue-kit)

These meta skills are duplicated into both kits intentionally:

| Skill | Purpose |
|-------|---------|
| `autonomous-loops` | Bounded iteration loops |
| `context-discipline` | Token budget management |
| `convention-learner` | Project-specific pattern detection |
| `instinct-system` | Confidence-scored hypothesis learning |
| `kit-setup` | Configure kit user-level settings |
| `learning-log` | Session discoveries capture |
| `model-selection` | Route tasks to Haiku/Sonnet/Opus |
| `sdlc-check` | SDLC compliance validation |
| `self-correction-loop` | Capture corrections → MEMORY.md |
| `session-management` | Start/end/resume sessions |
| `signalr-hub` | Full-stack: .NET hub + Vue composable |
| `workflow-mastery` | Multi-session planning |
| `wrap-up-ritual` | Session handoff |

## Kit versioning

Version numbers live in two places and must stay in sync:
- `{kit}/.claude-plugin/plugin.json` → `"version"` field
- `Kits/README.md` → the table row for that kit

## Two-level config system

| Level | File | Scope |
|-------|------|-------|
| User/device | `~/.claude/kit.config.md` | Machine-specific (CI provider, URLs, namespace) |
| Project | `.claude/project.config.md` in each repo | Committed; overrides user config |

Skills that need config and find the file missing must tell the user to run the appropriate setup command (`/kit-setup` or `/project-setup`).
