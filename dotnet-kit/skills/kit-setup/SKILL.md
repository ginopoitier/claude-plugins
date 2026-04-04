---
name: kit-setup
description: >
  Configure the user/device-level kit settings interactively.
  Walks through VCS host, CI/CD provider (GitHub Actions, TeamCity, Octopus),
  project management (Jira, GitHub Issues), documentation (Obsidian, Confluence),
  local dev infrastructure, and Atlassian MCP authentication.
  Writes to ~/.claude/kit.config.md.
  Load this skill when: "kit-setup", "configure kit", "kit config", "settings",
  "setup kit", "initialize kit", "first time setup", "reconfigure kit", "kit settings".
user-invocable: true
argument-hint: "[optional: section to reconfigure, e.g. ci-cd]"
allowed-tools: Read, Write
---

# Kit Setup

## Core Principles

1. **Go section by section** — ask questions one section at a time, wait for answers, confirm the section, then move to the next. Never dump all questions at once.
2. **Device-level config only** — this configures `~/.claude/kit.config.md` for this machine. Project-specific settings (stack, namespace, Jira project key) belong in `.claude/project.config.md` — tell user to run `/project-setup` in a repo.
3. **Secrets never stored in plaintext** — use `${VAR_NAME}` placeholders for tokens and API keys. Show the exact `setx` / `export` commands after saving.
4. **Atlassian auth is MCP OAuth** — no Jira/Confluence API tokens in the config file. After setup, show the `/mcp authenticate atlassian` command.
5. **Preview before write** — show the full config preview and get confirmation before writing to disk.

## Patterns

### Config File Format

```markdown
# Kit Config — User / Device Level
<!-- Configured {DATE} via /kit-setup -->

## CI / CD
CI_PROVIDER={github-actions|teamcity|azure-devops|none}
CD_PROVIDER={github-actions|octopus|azure-devops|none}
TEAMCITY_BASE_URL={value or blank}
OCTOPUS_URL={value or blank}
OCTOPUS_SPACE={Default}

## Project Management
PM_PROVIDER={jira|github-issues|none}
JIRA_BASE_URL={value or blank}
SPRINT_DURATION_DAYS={14}

## SDLC
SDLC_CONFLUENCE_SPACE={value or blank}
SDLC_PARENT_PAGE={value or blank}

## Documentation
DOCS_PRIMARY={obsidian|confluence|both|none}
OBSIDIAN_VAULT_PATH={value or blank}
OBSIDIAN_DEV_FOLDER={Dev}
OBSIDIAN_PROJECTS_FOLDER={Projects}
CONFLUENCE_BASE_URL={value or blank}
CONFLUENCE_DEFAULT_SPACE_KEY={value or blank}
GRAPHRAG_MCP_URL={value or blank}

## Dev Infrastructure
SEQ_URL={http://localhost:5341}
SEQ_API_KEY=${SEQ_API_KEY}
SQLSERVER_DEFAULT_HOST={localhost}
SQLSERVER_TRUST_CERT=true
NEO4J_DEFAULT_URI={bolt://localhost:7687}

## Scaffolding Defaults
DEFAULT_NAMESPACE={value or blank}
NEW_PROJECT_BASE_PATH={value or blank}

## Communication
SLACK_WORKSPACE={value or blank}
SLACK_DEFAULT_CHANNEL={value or blank}
TEAMS_WEBHOOK_URL={value or blank}
```

### Secrets Output After Save

```
Secret environment variables to set:

Windows (run in CMD — persists across sessions):
  setx SEQ_API_KEY  "paste-your-key-here"   [if Seq auth enabled]

bash (add to ~/.bashrc or ~/.zshrc):
  export SEQ_API_KEY="paste-your-key-here"

After setting env vars: restart your terminal and Claude Code for them to take effect.
```

### Atlassian MCP Auth (after save)

```
Atlassian MCP — authenticate now:

  /mcp authenticate atlassian

This opens a browser to complete the Atlassian OAuth flow.
Authentication persists across sessions — you won't need to do this again.

To verify it's working:
  /mcp status
```

## Anti-patterns

### Asking All Questions at Once

```
# BAD — overwhelming, hard to review
"Please tell me: CI provider, CD provider, Jira URL, Confluence URL,
 Obsidian path, Seq URL, default namespace..."

# GOOD — one section at a time
"Section 1: CI/CD. What CI provider do you use on this machine?"
[wait] → "What CD provider?"
[wait] → "Great. Moving to Section 2 — Project Management."
```

### Storing Secrets in Plaintext

```
# BAD — token visible in config file
SEQ_API_KEY=abc123supersecret

# GOOD — placeholder in config, actual value in env var
SEQ_API_KEY=${SEQ_API_KEY}
# Then show: setx SEQ_API_KEY "paste-your-key"
```

### Skipping the Config Preview

```
# BAD — write immediately after answers
"All done! Config written."

# GOOD — show full preview first
"Here's what I'll write to ~/.claude/kit.config.md:
 [full config block]
 Does this look correct? Type 'yes' to save, or tell me what to change."
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| First-time setup | Run all 4 sections in order |
| Reconfigure one section | `/kit-setup ci-cd` → jump to that section only |
| VCS / git identity needed | Configure your version control platform separately and use `/git-setup` for local identity |
| Jira / Confluence needed | Configure your issue tracker and documentation systems separately |
| Obsidian notes needed | Configure your note storage separately |
| TeamCity chosen | Ask for base URL (used in Kotlin DSL generation) |
| Secrets present | Show env var setup commands for current OS |

## Execution

Interactively configure `~/.claude/kit.config.md` — the **device-level** config that tells the kit which tools you use on this machine.

> **Two-level config system:**
> - `~/.claude/kit.config.md` (this file) — **device-specific**: toolchain, URLs, paths. Different on home vs. work machine.
> - `.claude/project.config.md` (per repo) — **project-specific**: stack, namespace, Jira project key. Run `/project-setup` in a project to create it.
>
> **VCS and git identity** are not configured here. Run `/github-setup` or `/bitbucket-setup` for platform config, and `/git-setup` for local git identity.
>
> **Jira** settings live in `~/.claude/jira-kit.config.md` — run `/jira-setup`.
> **Confluence** settings live in `~/.claude/confluence-kit.config.md` — run `/confluence-setup`.
> **Obsidian** settings live in `~/.claude/obsidian-kit.config.md` — run `/obsidian-setup`.

Go **section by section**. Ask the questions, wait for answers, confirm the section, then move to the next. Do NOT ask all questions at once.

Show a preview at the end. On confirmation, write to `~/.claude/kit.config.md`.

> **VCS and git identity** are configured separately — configure your version control platform separately and use `/git-setup` for local git identity.

---

### Section 1 — CI / CD

Say: "What CI/CD tools do you use **on this machine**? The `/ci-cd` skill generates pipeline config files for your chosen provider."

Ask:
- CI provider: `github-actions` / `teamcity` / `azure-devops` / `none`
- CD provider: `github-actions` / `octopus` / `azure-devops` / `none` (default: same as CI)

If `teamcity`:
- TeamCity base URL? (e.g. `https://build.mycompany.com`) — used as reference in generated Kotlin DSL

If `octopus`:
- Octopus Deploy URL? (e.g. `https://deploy.mycompany.com`) — used as reference in generated configs
- Octopus space name? (default: `Default`)

> Note: The CI/CD skill generates pipeline files only. It never calls TeamCity, Octopus, or GitHub APIs directly.

---

### Section 2 — Local Dev Infrastructure

Ask:
- Seq URL? (default: `http://localhost:5341`)
- Seq API key? (leave blank for no auth — stored as `${SEQ_API_KEY}` if provided)
- SQL Server host for local dev? (default: `localhost`)
- Neo4j URI? (default: `bolt://localhost:7687`)

---

### Section 3 — Scaffolding Defaults

Ask:
- Default namespace prefix? (e.g. `Acme` or `MyCompany` — used in scaffold commands, overridable per project)
- Default path for new projects? (e.g. `C:/Projects` or `~/dev`)

---

### Section 4 — Communication (optional)

Ask:
- Do you use Slack or Teams? (slack / teams / skip)

If slack: workspace URL and default channel?
If teams: webhook URL?

---

### After all sections

Show a **full preview** of the config file. Ask: "Does this look correct? Type `yes` to save, or tell me what to change."

On confirmation, write to `~/.claude/kit.config.md`.

Show only the env vars relevant to what the user configured.

Then say:
- "Device config saved at `~/.claude/kit.config.md`"
- "For project-specific settings (stack, namespace), run `/project-setup` inside a repository"
- "To reconfigure one section, just re-run `/kit-setup` — or edit `~/.claude/kit.config.md` directly"
- "Also run: `/jira-setup` · `/confluence-setup` · `/obsidian-setup` · `/github-setup` or `/bitbucket-setup` · `/git-setup`"

$ARGUMENTS
