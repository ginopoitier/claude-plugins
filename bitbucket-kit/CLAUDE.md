# Bitbucket Kit

> **Config:** @~/.claude/bitbucket-kit.config.md — run `/bitbucket-setup` if missing.

## Scope
- **Platform:** Bitbucket — pull requests, workspace/repo context, Bitbucket API
- **Covers:** PR creation · PR review diff · Bitbucket-specific workflows
- **Does NOT cover:** Local git operations (commits, branches, rebase) — those are git-kit
- **Does NOT cover:** Bitbucket Pipelines — CI/CD is handled by dev-kit `/ci-cd`

## Always-Active Rules

@~/.claude/rules/bitbucket-kit/pr-conventions.md

## Two-Level Config System

### User / Device Level — `~/.claude/bitbucket-kit.config.md`
Bitbucket workspace and credentials for this machine (typically the work machine):
- `BITBUCKET_WORKSPACE` · `BITBUCKET_BASE_URL` · `BITBUCKET_PR_DRAFT_BY_DEFAULT`

### Project Level — `.claude/bitbucket.config.md` (in each repo)
Repo-specific identifiers. Committed to version control:
- `BITBUCKET_REPO` · `DEFAULT_BRANCH`

Run `/bitbucket-setup` to configure. Project config **overrides** user config where values overlap.

When config is missing → the `check-settings` hook will prompt automatically.
When a skill needs config and it is missing → tell user to run `/bitbucket-setup`.

## Authentication

Bitbucket uses a **Personal Access Token** stored as a system environment variable — never in this config file.

```bash
# Windows (run in CMD, persists across sessions)
setx BITBUCKET_API_TOKEN "your-token-here"

# bash
export BITBUCKET_API_TOKEN="your-token-here"  # add to ~/.bashrc
```

Get your token: Bitbucket → Personal settings → Personal access tokens

## Skills Available

### Pull Requests
- `/pr` — create, view, list, and diff PRs using the Bitbucket REST API

### Setup
- `/bitbucket-setup` — configure user-level and project-level bitbucket-kit settings
