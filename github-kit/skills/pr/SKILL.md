---
name: pr
description: >
  Create, view, list, and diff GitHub pull requests using the gh CLI.
  Reads GITHUB_REPO and PR defaults from github-kit config.
  Load this skill when: "create pr", "open pr", "pull request", "pr", "make pr",
  "view pr", "list prs", "pr diff", "review pr", "pr description", "open pull request".
user-invocable: true
argument-hint: "[create | view <number> | list | diff <number>]"
allowed-tools: Bash, Read
---

# Pull Requests

## Core Principles

1. **`gh` CLI is the primary tool** — prefer `gh pr` commands over raw API calls.
2. **Read config first** — always load `GITHUB_REPO`, `DEFAULT_BRANCH`, and `GITHUB_PR_DRAFT_BY_DEFAULT` before acting.
3. **Infer from git** — detect current branch, base branch, and remote automatically.
4. **Never merge** — Claude creates and describes PRs; humans approve and merge.
5. **PR description explains why** — the diff shows what changed; the description explains the motivation and context.

## Patterns

### Read config

```bash
# User config
GITHUB_ORG=$(grep "^GITHUB_ORG=" ~/.claude/github-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DRAFT_DEFAULT=$(grep "^GITHUB_PR_DRAFT_BY_DEFAULT=" ~/.claude/github-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DRAFT_DEFAULT=${DRAFT_DEFAULT:-false}
DEFAULT_REVIEWER=$(grep "^GITHUB_DEFAULT_REVIEWER=" ~/.claude/github-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')

# Project config (overrides user where set)
GITHUB_REPO=$(grep "^GITHUB_REPO=" .claude/github.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEFAULT_BRANCH=$(grep "^DEFAULT_BRANCH=" .claude/github.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEFAULT_BRANCH=${DEFAULT_BRANCH:-main}

# Fallback: derive repo from remote if project config missing
if [[ -z "$GITHUB_REPO" ]]; then
  GITHUB_REPO=$(git remote get-url origin 2>/dev/null | sed 's|.*github.com[:/]||' | sed 's|\.git$||')
fi

# Current branch
CURRENT_BRANCH=$(git branch --show-current)
```

### Create PR

```bash
# Build gh command
DRAFT_FLAG=""
[[ "$DRAFT_DEFAULT" == "true" ]] && DRAFT_FLAG="--draft"

REVIEWER_FLAG=""
[[ -n "$DEFAULT_REVIEWER" ]] && REVIEWER_FLAG="--reviewer $DEFAULT_REVIEWER"

gh pr create \
  --base "$DEFAULT_BRANCH" \
  --title "{title}" \
  --body "{body}" \
  $DRAFT_FLAG \
  $REVIEWER_FLAG
```

**PR body template:**

```markdown
## Why

{motivation — what problem this solves or feature this enables}

## What changed

{high-level summary of the approach — the diff shows the details}

## Test plan

- [ ] {specific thing to verify}
- [ ] {another check}

## Notes

{anything reviewers should know: breaking changes, follow-up tickets, etc.}
```

If a Jira ticket key is found in the branch name (e.g. `feature/ORD-456-order-status`):
- Add `Closes ORD-456` or `Relates to ORD-456` at the bottom of the body
- Prefix the PR title with the key: `feat: add order status (ORD-456)`

### View PR

```bash
gh pr view {number}           # summary in terminal
gh pr view {number} --web     # open in browser
```

### List PRs

```bash
gh pr list                    # open PRs on current repo
gh pr list --author @me       # your own PRs
gh pr list --state all        # include closed
```

### Diff PR (for review tasks)

```bash
gh pr diff {number}           # full diff in terminal
```

## Decision Guide

| Input | Action |
|-------|--------|
| No argument | Detect current branch → `gh pr create` with prompts for title/body |
| `create` | Same as no argument |
| `view {n}` | `gh pr view {n}` |
| `list` | `gh pr list` |
| `diff {n}` | `gh pr diff {n}` — used by `/review` skill |
| `--draft` flag | Override config, force draft |

## Anti-patterns

```
# BAD — hardcoded repo
gh pr create --repo acme-corp/order-service

# GOOD — repo inferred from remote or config
gh pr create   # gh reads the remote automatically
```

```
# BAD — merging the PR
gh pr merge {number}

# GOOD — open in browser so a human can review and merge
gh pr view {number} --web
```

## Execution

Detect the subcommand from `$ARGUMENTS` (create / view / list / diff). If none given, default to `create`.

Load config, detect current branch and base, build the appropriate `gh pr` command. For `create`, draft a PR title from the branch name and current commits, draft a body, show a preview, and get confirmation before running.

$ARGUMENTS
