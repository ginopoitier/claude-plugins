# Bitbucket API Reference Patterns

## Authentication

All requests require an `Authorization: Bearer {token}` header. The token comes from the `BITBUCKET_API_TOKEN` environment variable — never from a file.

```bash
TOKEN="${BITBUCKET_API_TOKEN}"
[[ -z "$TOKEN" ]] && { echo "ERROR: BITBUCKET_API_TOKEN not set"; exit 1; }
```

## Base URLs

| Deployment | API base |
|------------|----------|
| Bitbucket Cloud | `https://api.bitbucket.org/2.0` |
| Bitbucket Server / Data Center | `${BITBUCKET_BASE_URL}/rest/api/1.0` |

Detect which is in use:
```bash
BASE_URL=$(grep "^BITBUCKET_BASE_URL=" ~/.claude/bitbucket-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
BASE_URL=${BASE_URL:-https://bitbucket.org}
if [[ "$BASE_URL" == "https://bitbucket.org" ]]; then
  API="https://api.bitbucket.org/2.0"
  REPO_API="${API}/repositories/${BITBUCKET_REPO}"
else
  # Server/DC: BITBUCKET_REPO format is "{projectKey}/{repoSlug}"
  PROJECT_KEY=$(echo "$BITBUCKET_REPO" | cut -d/ -f1)
  REPO_SLUG=$(echo "$BITBUCKET_REPO" | cut -d/ -f2)
  API="${BASE_URL}/rest/api/1.0"
  REPO_API="${API}/projects/${PROJECT_KEY}/repos/${REPO_SLUG}"
fi
```

## Key Endpoints — Bitbucket Cloud

| Operation | Method | Path |
|-----------|--------|------|
| List open PRs | GET | `/repositories/{ws}/{repo}/pullrequests?state=OPEN` |
| Get PR | GET | `/repositories/{ws}/{repo}/pullrequests/{id}` |
| Create PR | POST | `/repositories/{ws}/{repo}/pullrequests` |
| Get PR diff | GET | `/repositories/{ws}/{repo}/pullrequests/{id}/diff` |
| Get PR diffstat | GET | `/repositories/{ws}/{repo}/pullrequests/{id}/diffstat` |
| List PR comments | GET | `/repositories/{ws}/{repo}/pullrequests/{id}/comments` |
| Post PR comment | POST | `/repositories/{ws}/{repo}/pullrequests/{id}/comments` |
| Approve PR | POST | `/repositories/{ws}/{repo}/pullrequests/{id}/approve` |
| Request changes | POST | `/repositories/{ws}/{repo}/pullrequests/{id}/request-changes` |
| List tags | GET | `/repositories/{ws}/{repo}/refs/tags` |
| Create tag | POST | `/repositories/{ws}/{repo}/refs/tags` |
| Get file content | GET | `/repositories/{ws}/{repo}/src/{commit}/{path}` |

## Key Endpoints — Bitbucket Server / Data Center

| Operation | Method | Path |
|-----------|--------|------|
| List open PRs | GET | `/projects/{key}/repos/{slug}/pull-requests?state=OPEN` |
| Get PR | GET | `/projects/{key}/repos/{slug}/pull-requests/{id}` |
| Create PR | POST | `/projects/{key}/repos/{slug}/pull-requests` |
| Get PR diff | GET | `/projects/{key}/repos/{slug}/pull-requests/{id}/diff` |
| List PR activities | GET | `/projects/{key}/repos/{slug}/pull-requests/{id}/activities` |
| Post comment | POST | `/projects/{key}/repos/{slug}/pull-requests/{id}/comments` |
| Approve PR | POST | `/projects/{key}/repos/{slug}/pull-requests/{id}/approve` |
| Create tag | POST | `/projects/{key}/repos/{slug}/tags` |

> Server/DC uses `Authorization: Bearer {token}` for personal access tokens (v7.0+). Older versions use HTTP Basic auth.

## Pagination

Bitbucket Cloud responses include a `next` URL when more pages exist:

```python
import urllib.request, json, os

def fetch_all(url, token):
    """Fetch all pages from a paginated Bitbucket Cloud endpoint."""
    headers = {'Authorization': f'Bearer {token}'}
    results = []
    while url:
        req = urllib.request.Request(url, headers=headers)
        with urllib.request.urlopen(req) as r:
            data = json.loads(r.read())
        results.extend(data.get('values', []))
        url = data.get('next')
    return results
```

Bitbucket Server uses `start` (offset) + `isLastPage` for pagination:

```python
def fetch_all_server(base_url, token, limit=25):
    headers = {'Authorization': f'Bearer {token}'}
    results = []
    start = 0
    while True:
        url = f"{base_url}?start={start}&limit={limit}"
        req = urllib.request.Request(url, headers=headers)
        with urllib.request.urlopen(req) as r:
            data = json.loads(r.read())
        results.extend(data.get('values', []))
        if data.get('isLastPage', True):
            break
        start = data.get('nextPageStart', start + limit)
    return results
```

## JSON Serialisation (no jq available)

Use Python for all JSON parsing and construction:

```bash
# Parse a response field
VALUE=$(curl -s -H "Authorization: Bearer ${TOKEN}" "${URL}" | python3 -c "import json,sys; print(json.load(sys.stdin)['field'])")

# Build a request body safely (avoids shell escaping issues)
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
" -- "${TITLE}" "${BODY_TEXT}" "${SOURCE}" "${DEST}")

curl -s -X POST -H "Authorization: Bearer ${TOKEN}" -H "Content-Type: application/json" \
  "${REPO_API}/pullrequests" --data-raw "$BODY"
```

## Filtering (Bitbucket Cloud query language)

The `q` parameter supports field comparisons:

```bash
# Merged PRs after a date
?q=state="MERGED" AND updated_on>"2025-01-01T00:00:00+00:00"

# PRs from a specific source branch
?q=source.branch.name="feature/ORD-456"

# Open PRs by author
?q=state="OPEN" AND author.username="jsmith"
```

URL-encode the query when passing as a bash variable:

```bash
Q=$(python3 -c "import urllib.parse,sys; print(urllib.parse.quote(sys.argv[1]))" -- "state=\"MERGED\"")
curl -s -H "Authorization: Bearer ${TOKEN}" "${REPO_API}/pullrequests?q=${Q}"
```

## Error Handling

Check the HTTP status code in every response:

```bash
RESPONSE=$(curl -s -w "\n%{http_code}" -H "Authorization: Bearer ${TOKEN}" "${URL}")
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [[ "$HTTP_CODE" != "200" && "$HTTP_CODE" != "201" ]]; then
  echo "ERROR: HTTP ${HTTP_CODE}"
  echo "$BODY" | python3 -c "import json,sys; r=json.load(sys.stdin); print(r.get('error',{}).get('message','Unknown error'))" 2>/dev/null || echo "$BODY"
  exit 1
fi
```

## Rate Limits (Bitbucket Cloud)

- Unauthenticated: 60 requests/hour
- Authenticated: 1000 requests/hour per token
- Add `time.sleep(0.1)` between bulk requests to stay well under limits
