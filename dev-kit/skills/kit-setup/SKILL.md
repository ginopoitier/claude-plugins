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

## Identity
USER_NAME={value}
USER_EMAIL={value}

## Version Control
VCS_HOST={github|bitbucket|gitlab|azure-devops}
VCS_BASE_URL={derived}
VCS_ORG={value}
VCS_DEFAULT_BRANCH={main}
VCS_PR_DRAFT_BY_DEFAULT={true|false}
BITBUCKET_WORKSPACE={value or blank}
BITBUCKET_API_TOKEN=${BITBUCKET_API_TOKEN}

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
  setx BITBUCKET_API_TOKEN  "paste-your-token-here"   [if Bitbucket]
  setx SEQ_API_KEY          "paste-your-key-here"      [if Seq auth enabled]

bash (add to ~/.bashrc or ~/.zshrc):
  export BITBUCKET_API_TOKEN="paste-your-token-here"
  export SEQ_API_KEY="paste-your-key-here"

After setting env vars: restart your terminal and Claude Code for them to take effect.
Get your token:
  Bitbucket: Bitbucket Settings → Personal access tokens
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
"Please tell me: your name, email, VCS host, org, CI provider, CD provider,
 Jira URL, Confluence URL, Obsidian path, Seq URL, default namespace..."

# GOOD — one section at a time
"Section 1: Identity. What is your full name?"
[wait] → "What is your email?"
[wait] → "Great. Moving to Section 2 — Version Control."
```

### Storing Secrets in Plaintext

```
# BAD — token visible in config file
BITBUCKET_API_TOKEN=ghp_abc123supersecret

# GOOD — placeholder in config, actual value in env var
BITBUCKET_API_TOKEN=${BITBUCKET_API_TOKEN}
# Then show: setx BITBUCKET_API_TOKEN "paste-your-token"
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
| First-time setup | Run all 8 sections in order |
| Reconfigure one section | `/kit-setup ci-cd` → jump to that section only |
| Bitbucket chosen | Ask for workspace + explain PAT env var setup |
| Jira chosen | Explain Atlassian MCP OAuth — no token in config |
| TeamCity chosen | Ask for base URL (used in Kotlin DSL generation) |
| Obsidian chosen | Ask for vault path + subfolder names |
| Secrets present | Show env var setup commands for current OS |
| Atlassian configured | Show `/mcp authenticate atlassian` after save |

## Execution

Interactively configure `~/.claude/kit.config.md` — the **device-level** config that tells the kit which tools you use on this machine.

> **Two-level config system:**
> - `~/.claude/kit.config.md` (this file) — **device-specific**: toolchain, URLs, paths. Different on home vs. work machine.
> - `.claude/project.config.md` (per repo) — **project-specific**: stack, namespace, repo identifiers. Run `/project-setup` in a project to create it.
>
> **Atlassian (Jira + Confluence)** auth is handled by the Atlassian MCP via OAuth — no API tokens needed in this config.
>
> **Bitbucket** uses a personal access token (set as a system env var, never stored in plain text).

Go **section by section**. Ask the questions, wait for answers, confirm the section, then move to the next. Do NOT ask all questions at once.

Show a preview at the end. On confirmation, write to `~/.claude/kit.config.md`.

---

### Section 1 — Identity

Ask:
- What is your full name?
- What is your email?

---

### Section 2 — Version Control

Say: "What VCS host do you use **on this machine**? (You may use a different one on your work machine.)"

Ask:
- VCS host: `github` / `bitbucket` / `gitlab` / `azure-devops`
- Your org or username on this host?
- Default branch name? (default: `main`)
- PRs as drafts by default? (yes/no, default: no)

If `bitbucket`:
- Bitbucket workspace? (e.g. `mycompany`)
- Bitbucket personal access token env var name? (default: `BITBUCKET_API_TOKEN` — stored as `${BITBUCKET_API_TOKEN}`)

---

### Section 3 — CI / CD

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

### Section 4 — Project Management

Ask:
- Do you use a project management tool on this machine?
  - `jira` / `github-issues` / `none`

If `jira`:
- Jira base URL? (e.g. `https://mycompany.atlassian.net`) — used for building ticket URLs

Say: "Jira authentication is handled by the Atlassian MCP via OAuth. No API token is stored in this config. After setup I'll show you how to authenticate."

---

### Section 4.5 — SDLC & Sprint (only if `jira` was chosen)

Say: "The kit can read your company's SDLC from Confluence to apply process requirements automatically in `/epic`, `/tech-refinement`, `/sdlc-check`, and `/review`."

Ask:
- Do you have your SDLC documented in Confluence? (yes/no)

If yes:
- Confluence space key where the SDLC lives? (e.g. `ENG` or `PLATFORM`)
- Title of the parent SDLC page? (e.g. `Software Development Lifecycle`)

Ask:
- How many days is your sprint? (default: `14`)

---

### Section 5 — Documentation

Say: "Where do you write documentation on this machine?"

Ask:
- Primary doc target: `obsidian` / `confluence` / `both` / `none`

If `obsidian` or `both`:
- Obsidian vault path? (full path, e.g. `D:/Notes` or `~/Obsidian/MyVault`)
- Subfolder for dev notes? (default: `Dev`)
- Subfolder for project docs? (default: `Projects`)

If `confluence` or `both`:
- Confluence base URL? (e.g. `https://mycompany.atlassian.net/wiki`) — used for building page URLs
- Default Confluence space key? (e.g. `DEV` — can be overridden per project in `.claude/project.config.md`)

Say: "Confluence authentication is handled by the Atlassian MCP — same OAuth as Jira."

Ask:
- Do you have a local GraphRAG MCP server? If yes, what URL? (skip if unsure)

---

### Section 6 — Local Dev Infrastructure

Ask:
- Seq URL? (default: `http://localhost:5341`)
- Seq API key? (leave blank for no auth — stored as `${SEQ_API_KEY}` if provided)
- SQL Server host for local dev? (default: `localhost`)
- Neo4j URI? (default: `bolt://localhost:7687`)

---

### Section 7 — Scaffolding Defaults

Ask:
- Default namespace prefix? (e.g. `Acme` or `MyCompany` — used in scaffold commands, overridable per project)
- Default path for new projects? (e.g. `C:/Projects` or `~/dev`)

---

### Section 8 — Communication (optional)

Ask:
- Do you use Slack or Teams? (slack / teams / skip)

If slack: workspace URL and default channel?
If teams: webhook URL?

---

### After all sections

Show a **full preview** of the config file. Ask: "Does this look correct? Type `yes` to save, or tell me what to change."

On confirmation, write to `~/.claude/kit.config.md`.

Show only the env vars relevant to what the user configured.

If the user configured Jira or Confluence, show the Atlassian MCP auth block.

Then say:
- "Device config saved at `~/.claude/kit.config.md`"
- "For project-specific settings (stack, namespace, Jira project key), run `/project-setup` inside a repository"
- "To reconfigure one section, just re-run `/kit-setup` — or edit `~/.claude/kit.config.md` directly"

$ARGUMENTS
