# GitHub Kit

> **Config:** @~/.claude/github-kit.config.md — run `/github-setup` if missing.

## Scope
- **Platform:** GitHub — pull requests, releases, org/repo context
- **Covers:** PR creation · PR review diff · Releases · GitHub-specific workflows
- **Does NOT cover:** Local git operations (commits, branches, rebase) — those are git-kit

## Always-Active Rules

@~/.claude/rules/github-kit/pr-conventions.md

## Two-Level Config System

### User / Device Level — `~/.claude/github-kit.config.md`
GitHub identity and org for this machine. Different on home vs. work machine:
- `GITHUB_ORG` · `GITHUB_BASE_URL` · `GITHUB_PR_DRAFT_BY_DEFAULT`

### Project Level — `.claude/github.config.md` (in each repo)
Repo-specific identifiers. Committed to version control:
- `GITHUB_REPO` · `DEFAULT_BRANCH`

Run `/github-setup` to configure. Project config **overrides** user config where values overlap.

When config is missing → the `check-settings` hook will prompt automatically.
When a skill needs config and it is missing → tell user to run `/github-setup`.

## Skills Available

### Pull Requests
- `/pr` — create, view, list, and diff PRs using the `gh` CLI or GitHub MCP

### Releases
- `/release` — tag, create GitHub release, auto-generate release notes from commits

### Setup
- `/github-setup` — configure user-level and project-level github-kit settings
