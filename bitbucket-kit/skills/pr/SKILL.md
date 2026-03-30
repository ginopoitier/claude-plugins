---
name: pr
description: >
  Create, view, list, and diff Bitbucket pull requests using the Bitbucket REST API.
  Reads BITBUCKET_REPO and PR defaults from bitbucket-kit config.
  Load this skill when: "create pr", "open pr", "pull request", "pr", "make pr",
  "view pr", "list prs", "pr diff", "review pr", "pr description", "open pull request".
user-invocable: true
argument-hint: "[create | view <id> | list | diff <id>]"
allowed-tools: Bash, Read
---

# Pull Requests

## Core Principles

1. **Bitbucket REST API v2** — all operations go through `https://api.bitbucket.org/2.0/`.
2. **Token from env** — always read `BITBUCKET_API_TOKEN` from the environment, never from a file.
3. **Read config first** — load workspace, repo, branch, and PR defaults before acting.
4. **Infer from git** — detect current branch and remote automatically.
5. **Never merge** — Claude creates and describes PRs; humans approve and merge.

## Patterns

### Read config and token

```bash
# User config
BITBUCKET_WORKSPACE=$(grep "^BITBUCKET_WORKSPACE=" ~/.claude/bitbucket-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
BITBUCKET_BASE_URL=$(grep "^BITBUCKET_BASE_URL=" ~/.claude/bitbucket-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
BITBUCKET_BASE_URL=${BITBUCKET_BASE_URL:-https://bitbucket.org}
DRAFT_DEFAULT=$(grep "^BITBUCKET_PR_DRAFT_BY_DEFAULT=" ~/.claude/bitbucket-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DRAFT_DEFAULT=${DRAFT_DEFAULT:-false}

# Project config (overrides user where set)
BITBUCKET_REPO=$(grep "^BITBUCKET_REPO=" .claude/bitbucket.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEFAULT_BRANCH=$(grep "^DEFAULT_BRANCH=" .claude/bitbucket.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEFAULT_BRANCH=${DEFAULT_BRANCH:-main}

# Fallback: derive from remote
if [[ -z "$BITBUCKET_REPO" ]]; then
  BITBUCKET_REPO=$(git remote get-url origin 2>/dev/null | sed 's|.*bitbucket.org[:/]||' | sed 's|\.git$||')
fi

# Token — from system env (never from file)
TOKEN="${BITBUCKET_API_TOKEN}"
if [[ -z "$TOKEN" ]]; then
  echo "ERROR: BITBUCKET_API_TOKEN environment variable is not set."
  echo "Run: setx BITBUCKET_API_TOKEN \"your-token\"  (Windows) or export BITBUCKET_API_TOKEN=\"your-token\"  (bash)"
  exit 1
fi

API="https://api.bitbucket.org/2.0"
REPO_API="${API}/repositories/${BITBUCKET_REPO}"
CURRENT_BRANCH=$(git branch --show-current)
```

### Create PR

```bash
# Build request body
DRAFT_VAL="false"
[[ "$DRAFT_DEFAULT" == "true" ]] && DRAFT_VAL="true"

curl -s -X POST \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  "${REPO_API}/pullrequests" \
  --data-raw "{
    \"title\": \"{title}\",
    \"description\": \"{body}\",
    \"source\": {\"branch\": {\"name\": \"${CURRENT_BRANCH}\"}},
    \"destination\": {\"branch\": {\"name\": \"${DEFAULT_BRANCH}\"}},
    \"draft\": ${DRAFT_VAL},
    \"reviewers\": [{reviewer object if DEFAULT_REVIEWER set}],
    \"close_source_branch\": true
  }"
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
- Prefix the PR title: `feat: add order status (ORD-456)`
- Add `Closes ORD-456` at the bottom of the description

### View PR

```bash
curl -s \
  -H "Authorization: Bearer ${TOKEN}" \
  "${REPO_API}/pullrequests/{id}" | python3 -c "
import json, sys
d = json.load(sys.stdin)
print(f'#{d[\"id\"]} {d[\"title\"]} [{d[\"state\"]}]')
print(f'Author: {d[\"author\"][\"display_name\"]}')
print(f'URL: {d[\"links\"][\"html\"][\"href\"]}')
"
```

### List PRs

```bash
curl -s \
  -H "Authorization: Bearer ${TOKEN}" \
  "${REPO_API}/pullrequests?state=OPEN" | python3 -c "
import json, sys
for p in json.load(sys.stdin)['values']:
    print(f'#{p[\"id\"]} {p[\"title\"]} | {p[\"author\"][\"display_name\"]} | {p[\"source\"][\"branch\"][\"name\"]}')
"
```

### Diff PR (for review tasks)

```bash
curl -s \
  -H "Authorization: Bearer ${TOKEN}" \
  "${REPO_API}/pullrequests/{id}/diff"
```

## Decision Guide

| Input | Action |
|-------|--------|
| No argument | Detect current branch → create PR with prompts for title/body |
| `create` | Same as no argument |
| `view {id}` | Fetch and display PR summary |
| `list` | List open PRs |
| `diff {id}` | Fetch diff — used by `/review` skill in dev-kit |
| `--draft` flag | Override config, force draft |

## Execution

1. Read config and resolve `BITBUCKET_API_TOKEN` from env. Abort with clear message if token is missing.
2. Detect subcommand from `$ARGUMENTS` (create / view / list / diff). Default to `create`.
3. For `create`: draft title from branch name and recent commits, draft body, show preview, confirm, then POST to API.
4. After creation: print the PR URL from the API response (`links.html.href`).

$ARGUMENTS
