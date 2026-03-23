---
name: github-setup
description: >
  Configure github-kit user-level and project-level settings. Sets up GitHub org,
  base URL, PR defaults, and repo coordinates. Load this skill when:
  "github setup", "configure github", "github-setup", "setup github kit",
  "github config", "github kit config", "setup github", "initialize github config".
user-invocable: true
argument-hint: "[--user | --project | --both]"
allowed-tools: Bash, Read, Write
---

# GitHub Setup

## Core Principles

1. **Two levels** — user config (`~/.claude/github-kit.config.md`) is per-machine; project config (`.claude/github.config.md`) is per-repo and committed.
2. **Project config is committed** — all developers on the project share the same repo coordinates.
3. **User config is personal** — stays on this machine, never committed.
4. **Infer what you can** — detect `GITHUB_REPO` from `git remote get-url origin` before asking.
5. **Preview before write** — show the full config block and get confirmation before writing.

## Patterns

### Detect repo from remote

```bash
git remote get-url origin
# https://github.com/acme-corp/order-service.git
# → GITHUB_REPO=acme-corp/order-service
# → GITHUB_ORG=acme-corp (if not already set in user config)
```

### User-level config (`--user`)

Ask section by section — wait for each answer before continuing:

**Section 1 — GitHub Org**
- What is your GitHub org or username? (e.g. `acme-corp` or `johndoe`)
- Are you using GitHub Enterprise? If yes, what is the base URL? (default: `https://github.com`)

**Section 2 — PR Defaults**
- Open new PRs as drafts by default? (yes/no, default: no)
- Default reviewer username or team slug? (optional, leave blank to skip)

Write to `~/.claude/github-kit.config.md`:

```markdown
# GitHub Kit Config — User / Device Level
<!-- Configured {DATE} via /github-setup -->

## GitHub Identity
GITHUB_ORG={value}
GITHUB_BASE_URL={https://github.com or enterprise URL}

## Pull Request Defaults
GITHUB_PR_DRAFT_BY_DEFAULT={true|false}
GITHUB_DEFAULT_REVIEWER={value or blank}
```

### Project-level config (`--project`)

Run from the repo root. Detect what you can, then ask:

```bash
# Detect repo from remote
git remote get-url origin
# → derive GITHUB_REPO

# Detect default branch
git symbolic-ref refs/remotes/origin/HEAD 2>/dev/null | sed 's@^refs/remotes/origin/@@'
# → DEFAULT_BRANCH
```

Ask:
- Confirm detected `GITHUB_REPO` — correct?
- Confirm detected `DEFAULT_BRANCH` — correct?
- Override `GITHUB_PR_DRAFT_BY_DEFAULT` for this project? (leave blank to inherit user setting)
- Default reviewer for this project? (leave blank to inherit user setting)

Write to `.claude/github.config.md`, then:

```bash
git add .claude/github.config.md
git commit -m "chore: add github-kit project config"
```

## Decision Guide

| Scenario | Command |
|----------|---------|
| First time on a new machine | `/github-setup --user` |
| Starting a new project | `/github-setup --project` |
| Joining an existing project | `/github-setup --project` (reads existing config if present) |
| Config missing, hook triggered it | Run `/github-setup` — detects which level is missing |
| Both levels missing | `/github-setup --both` — runs user then project setup in sequence |

## Execution

Detect which config levels are missing:

```bash
USER_CONFIG_EXISTS=false
PROJECT_CONFIG_EXISTS=false
[[ -f ~/.claude/github-kit.config.md ]] && USER_CONFIG_EXISTS=true
[[ -f .claude/github.config.md ]] && PROJECT_CONFIG_EXISTS=true
```

- If `--user` or user config missing → run user setup
- If `--project` or project config missing → run project setup
- If `--both` → run user then project
- If neither flag and both configs exist → ask which to reconfigure

$ARGUMENTS
