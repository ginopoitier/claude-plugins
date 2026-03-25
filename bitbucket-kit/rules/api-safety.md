# Rule: Bitbucket API Safety

## Shell-First for All API Calls

Always use `curl` via the Bash tool for Bitbucket API operations. Never simulate API responses in model context — this wastes tokens and produces hallucinated data.

```bash
# CORRECT — shell executes the actual API call
curl -s -H "Authorization: Bearer ${TOKEN}" "${REPO_API}/pullrequests?state=OPEN"

# WRONG — do not describe what the response "would look like"
# Just run the curl and parse the real output
```

Use Python for JSON parsing since `jq` is not available:

```bash
curl -s -H "Authorization: Bearer ${TOKEN}" "${URL}" | python3 -c "
import json, sys
r = json.load(sys.stdin)
print(r['field'])
"
```

## Never Call These Without Explicit User Confirmation

- `DELETE` on any resource (branches, tags, repos, PRs)
- `POST /pullrequests/{id}/merge` — merging is a human decision
- Any destructive operation on a protected branch

If a user's request would require a destructive API call, state clearly what it would do and ask for confirmation before proceeding.

## Never Store or Log the API Token

```bash
# CORRECT — reference from env only
TOKEN="${BITBUCKET_API_TOKEN}"

# WRONG — do not echo, print, or log the token value
echo "Token: ${BITBUCKET_API_TOKEN}"   # never do this
```

## Abort Clearly When Token Is Missing

```bash
TOKEN="${BITBUCKET_API_TOKEN}"
if [[ -z "$TOKEN" ]]; then
  echo "ERROR: BITBUCKET_API_TOKEN is not set."
  echo "Set it with: setx BITBUCKET_API_TOKEN \"your-token\" (Windows) or export BITBUCKET_API_TOKEN=\"your-token\" (bash)"
  exit 1
fi
```

## Check HTTP Status on Every API Response

Do not silently ignore failed API calls:

```bash
RESPONSE=$(curl -s -w "\n%{http_code}" -H "Authorization: Bearer ${TOKEN}" "${URL}")
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -n -1)
if [[ "$HTTP_CODE" != "200" && "$HTTP_CODE" != "201" ]]; then
  echo "ERROR: HTTP ${HTTP_CODE}: $BODY"
  exit 1
fi
```

## Use --data-raw for JSON Bodies

Always use `--data-raw` (not `-d` or `--data`) when sending JSON to avoid shell interpretation of `@` and special characters:

```bash
curl -s -X POST \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  "${REPO_API}/pullrequests" \
  --data-raw "$BODY"
```

Build request bodies with Python to avoid quoting issues:

```bash
BODY=$(python3 -c "
import json, sys
print(json.dumps({
    'title': sys.argv[1],
    'source': {'branch': {'name': sys.argv[2]}},
    'destination': {'branch': {'name': sys.argv[3]}}
}))" -- "${TITLE}" "${SOURCE}" "${DEST}")
```

## Rate Limit Awareness

Bitbucket Cloud: 1000 requests/hour per token. When making bulk requests (pagination loops, diffstat per PR):
- Add a short delay between requests if fetching more than 50 resources
- Never retry the same failed request more than once — investigate the error instead

## Scope of bitbucket-kit

bitbucket-kit only touches these API areas:
- Pull requests (CRUD, comments, approvals)
- Tags / refs
- File content (read-only, for review context)

It does **not** touch:
- Bitbucket Pipelines — use dev-kit `/ci-cd`
- Repository settings or branch permissions
- User/group management
- Webhooks
