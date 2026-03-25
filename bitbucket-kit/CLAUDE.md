# Bitbucket Kit

> **Config:** @~/.claude/bitbucket-kit.config.md — run `/bitbucket-setup` if missing.

## Scope
- **Platform:** Bitbucket — pull requests, workspace/repo context, Bitbucket API
- **Covers:** PR creation · PR review · PR comments · tags/releases · Bitbucket-specific workflows
- **Does NOT cover:** Local git operations (commits, branches, rebase) — those are git-kit
- **Does NOT cover:** Bitbucket Pipelines — CI/CD is handled by dev-kit `/ci-cd`

## Always-Active Rules

@~/.claude/rules/bitbucket-kit/pr-conventions.md
@~/.claude/rules/bitbucket-kit/api-safety.md

## Knowledge

@~/.claude/knowledge/bitbucket-kit/bitbucket-api.md
@~/.claude/knowledge/bitbucket-kit/bitbucket-concepts.md

## Agents

See `AGENTS.md` for full routing table. Spawn via `Agent` tool with `subagent_type: "bitbucket-kit:{name}"`.

| Agent | When |
|-------|------|
| `bb-reviewer` | Deep PR review + post comments to Bitbucket (large diffs, Opus context) |
| `bb-historian` | PR analytics and history reports |

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

Bitbucket uses a **Personal Access Token** stored as a system environment variable — never in a config file.

```bash
# Windows (run in CMD, persists across sessions)
setx BITBUCKET_API_TOKEN "your-token-here"

# bash (add to ~/.bashrc or ~/.zshrc)
export BITBUCKET_API_TOKEN="your-token-here"
```

Get your token: Bitbucket → Personal settings → Personal access tokens
Required scopes: Repositories (Read), Pull requests (Write)

## API Pattern

All Bitbucket API operations use `curl` via the Bash tool. Use Python for JSON parsing — `jq` is not available.

```bash
curl -s -H "Authorization: Bearer ${BITBUCKET_API_TOKEN}" "${URL}" | python3 -c "import json,sys; ..."
```

See `knowledge/bitbucket-api.md` for full patterns including pagination, error handling, and Cloud vs Server differences.

## Skills Available

### Pull Requests
- `/pr` — create, view, list, and diff PRs using the Bitbucket REST API
- `/review` — tech lead code review with `--mentoring` (coaching) or `--gatekeeper` (strict) modes

### Releases
- `/release` — create a Bitbucket tag and auto-generate release notes from merged PRs

### Setup
- `/bitbucket-setup` — configure user-level and project-level bitbucket-kit settings
