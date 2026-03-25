# Rule: Bitbucket PR Conventions

## DO
- Use the Bitbucket REST API v2 for all PR operations (`https://api.bitbucket.org/2.0/`)
- Read `BITBUCKET_WORKSPACE` and `BITBUCKET_REPO` from config — never hardcode them
- Resolve `BITBUCKET_API_TOKEN` from the `${BITBUCKET_API_TOKEN}` env var at runtime
- Include the Jira ticket key in the PR title when a Jira key is found in the branch name
- Check `.claude/git.config.md` PROTECTED_BRANCHES before targeting a base branch
- Use `--data-raw` with `curl` for API calls to avoid shell escaping issues with JSON
- Use Python for JSON parsing — `jq` is not available on this system
- Build JSON request bodies with Python to avoid quoting/escaping issues

## DON'T
- Don't store the API token in any config file — it must come from the system env var
- Don't merge PRs — merging requires human approval
- Don't perform DELETE operations without explicit user confirmation
- Don't use the Bitbucket Pipelines API — that's dev-kit `/ci-cd` territory
- Don't use `echo "$TOKEN"` or log the token value anywhere

## Reading Config

```bash
# User config
BITBUCKET_WORKSPACE=$(grep "^BITBUCKET_WORKSPACE=" ~/.claude/bitbucket-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DRAFT_DEFAULT=$(grep "^BITBUCKET_PR_DRAFT_BY_DEFAULT=" ~/.claude/bitbucket-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DRAFT_DEFAULT=${DRAFT_DEFAULT:-false}

# Project config (overrides user where set)
BITBUCKET_REPO=$(grep "^BITBUCKET_REPO=" .claude/bitbucket.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEFAULT_BRANCH=$(grep "^DEFAULT_BRANCH=" .claude/bitbucket.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEFAULT_BRANCH=${DEFAULT_BRANCH:-main}

# Fallback: derive from remote if project config missing
if [[ -z "$BITBUCKET_REPO" ]]; then
  BITBUCKET_REPO=$(git remote get-url origin 2>/dev/null | sed 's|.*bitbucket.org[:/]||;s|\.git$||')
fi

# API token from env — abort if missing
TOKEN="${BITBUCKET_API_TOKEN}"
[[ -z "$TOKEN" ]] && { echo "ERROR: BITBUCKET_API_TOKEN not set"; exit 1; }
```

## PR Title Format

Use conventional commit prefix + Jira key when available:

```
{type}: {short description} ({JIRA_KEY})

Examples:
  feat: add order cancellation (ORD-456)
  fix: payment null reference (ORD-789)
  chore: upgrade EF Core to 9.0
```

Extract Jira key from branch name:
```bash
BRANCH=$(git branch --show-current)
JIRA_KEY=$(echo "$BRANCH" | python3 -c "
import re, sys
m = re.search(r'[A-Z]+-\d+', sys.stdin.read())
print(m.group() if m else '')
")
```

## Building JSON Bodies

```bash
# Use Python to build request bodies — avoids shell quoting nightmares
BODY=$(python3 -c "
import json, sys
payload = {
    'title': sys.argv[1],
    'description': sys.argv[2],
    'source': {'branch': {'name': sys.argv[3]}},
    'destination': {'branch': {'name': sys.argv[4]}},
    'draft': False,
    'close_source_branch': True
}
print(json.dumps(payload))
" -- "${TITLE}" "${DESCRIPTION}" "${CURRENT_BRANCH}" "${DEFAULT_BRANCH}")

curl -s -X POST \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  "${REPO_API}/pullrequests" \
  --data-raw "$BODY"
```

## Parsing API Responses

```bash
# Extract a single field
PR_URL=$(curl -s -H "Authorization: Bearer ${TOKEN}" "${REPO_API}/pullrequests/${ID}" | python3 -c "
import json, sys
r = json.load(sys.stdin)
print(r['links']['html']['href'])
")

# List multiple items
curl -s -H "Authorization: Bearer ${TOKEN}" "${REPO_API}/pullrequests?state=OPEN" | python3 -c "
import json, sys
r = json.load(sys.stdin)
for pr in r.get('values', []):
    print(f'#{pr[\"id\"]:4d}  {pr[\"source\"][\"branch\"][\"name\"]:<30s}  {pr[\"title\"]}')
"
```

## Posting Comments

```bash
# Inline comment — 'to' is the new-file line number
BODY=$(python3 -c "
import json, sys
print(json.dumps({
    'content': {'raw': sys.argv[1]},
    'inline': {'path': sys.argv[2], 'to': int(sys.argv[3])}
}))" -- "${COMMENT_TEXT}" "${FILE_PATH}" "${LINE_NUMBER}")

curl -s -X POST \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  "${REPO_API}/pullrequests/${PR_ID}/comments" \
  --data-raw "$BODY"
```

## API Base URL

```bash
BASE_URL=$(grep "^BITBUCKET_BASE_URL=" ~/.claude/bitbucket-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
BASE_URL=${BASE_URL:-https://bitbucket.org}

if [[ "$BASE_URL" == "https://bitbucket.org" ]]; then
  API="https://api.bitbucket.org/2.0"
  REPO_API="${API}/repositories/${BITBUCKET_REPO}"
else
  # Bitbucket Server/Data Center
  PROJECT=$(echo "$BITBUCKET_REPO" | cut -d/ -f1)
  SLUG=$(echo "$BITBUCKET_REPO" | cut -d/ -f2)
  REPO_API="${BASE_URL}/rest/api/1.0/projects/${PROJECT}/repos/${SLUG}"
fi
```
