---
name: bitbucket-setup
description: >
  Configure bitbucket-kit user-level and project-level settings. Sets up workspace,
  base URL, API token reference, and PR defaults. Load this skill when:
  "bitbucket setup", "configure bitbucket", "bitbucket-setup", "setup bitbucket kit",
  "bitbucket config", "bitbucket kit config", "setup bitbucket", "initialize bitbucket config".
user-invocable: true
argument-hint: "[--user | --project | --both]"
allowed-tools: Bash, Read, Write
---

# Bitbucket Setup

## Core Principles

1. **Two levels** — user config (`~/.claude/bitbucket-kit.config.md`) is per-machine; project config (`.claude/bitbucket.config.md`) is per-repo and committed.
2. **Token stays in env** — the API token is never written to any file. Config stores `${BITBUCKET_API_TOKEN}` as a placeholder; the value comes from the system env var.
3. **Project config is committed** — all developers on the project share the same repo coordinates.
4. **Infer what you can** — detect `BITBUCKET_REPO` from `git remote get-url origin` before asking.
5. **Preview before write** — show the full config block and get confirmation before writing.

## Patterns

### Detect repo from remote

```bash
git remote get-url origin
# https://bitbucket.org/acme-corp/order-service.git  (Bitbucket Cloud)
# → BITBUCKET_REPO=acme-corp/order-service
# → BITBUCKET_WORKSPACE=acme-corp

# ssh://git@bitbucket.org/acme-corp/order-service.git
# → same extraction applies
```

### User-level config (`--user`)

Ask section by section — wait for each answer before continuing:

**Section 1 — Workspace**
- What is your Bitbucket workspace slug? (e.g. `acme-corp`)
- Are you using Bitbucket Server or Data Center? If yes, what is the base URL? (default: `https://bitbucket.org`)

**Section 2 — API Token**
- What environment variable name holds your Bitbucket personal access token? (default: `BITBUCKET_API_TOKEN`)

Show the token setup instructions after confirming:

```
Set your token as a system environment variable (never store it in a file):

Windows (CMD — persists across sessions):
  setx BITBUCKET_API_TOKEN "your-token-here"

bash (add to ~/.bashrc or ~/.zshrc):
  export BITBUCKET_API_TOKEN="your-token-here"

Get your token: Bitbucket → Personal settings → Personal access tokens
Required scopes: Repositories (Read), Pull requests (Write)

After setting, restart your terminal and Claude Code.
```

**Section 3 — PR Defaults**
- Open new PRs as drafts by default? (yes/no, default: no)
- Default reviewer username? (optional, leave blank to skip)

Write to `~/.claude/bitbucket-kit.config.md`:

```markdown
# Bitbucket Kit Config — User / Device Level
<!-- Configured {DATE} via /bitbucket-setup -->

## Bitbucket Identity
BITBUCKET_WORKSPACE={value}
BITBUCKET_BASE_URL={https://bitbucket.org or server URL}
BITBUCKET_API_TOKEN=${BITBUCKET_API_TOKEN}

## Pull Request Defaults
BITBUCKET_PR_DRAFT_BY_DEFAULT={true|false}
BITBUCKET_DEFAULT_REVIEWER={value or blank}
```

### Project-level config (`--project`)

Run from the repo root. Detect what you can, then confirm:

```bash
# Detect from remote
git remote get-url origin
# → derive BITBUCKET_REPO

# Detect default branch
git symbolic-ref refs/remotes/origin/HEAD 2>/dev/null | sed 's@^refs/remotes/origin/@@'
```

Ask:
- Confirm detected `BITBUCKET_REPO` — correct?
- Confirm detected `DEFAULT_BRANCH` — correct?
- Override PR draft default for this project? (leave blank to inherit)
- Default reviewer for this project? (leave blank to inherit)

Write to `.claude/bitbucket.config.md`, then:

```bash
git add .claude/bitbucket.config.md
git commit -m "chore: add bitbucket-kit project config"
```

## Decision Guide

| Scenario | Command |
|----------|---------|
| First time on a new (work) machine | `/bitbucket-setup --user` |
| Starting a new project | `/bitbucket-setup --project` |
| Joining an existing project | `/bitbucket-setup --project` (reads existing config if present) |
| Config missing, hook triggered it | Run `/bitbucket-setup` — detects which level is missing |
| Both levels missing | `/bitbucket-setup --both` — runs user then project in sequence |

## Execution

Detect which config levels are missing:

```bash
[[ -f ~/.claude/bitbucket-kit.config.md ]] && USER_EXISTS=true || USER_EXISTS=false
[[ -f .claude/bitbucket.config.md ]] && PROJECT_EXISTS=true || PROJECT_EXISTS=false
```

- If `--user` or user config missing → run user setup
- If `--project` or project config missing → run project setup
- If `--both` → run user then project
- If neither flag and both configs exist → ask which section to reconfigure

$ARGUMENTS
