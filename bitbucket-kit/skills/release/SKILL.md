---
name: release
description: >
  Create a Bitbucket tag and generate release notes from merged PRs since the last tag.
  All API calls use curl + Python — no jq required. Uses the Bitbucket REST API to fetch
  merged PR titles; groups them by conventional-commit type into formatted release notes.
  Load this skill when: "create release", "tag release", "release notes", "create tag",
  "new release", "generate release notes", "bump version", "tag version", "ship release".
user-invocable: true
argument-hint: "[<tag>] [--draft] [--since <tag>]"
allowed-tools: Bash, Read, Write
---

# Release

## Core Principles

1. **Shell for all API calls** — `curl` + Python for every Bitbucket query; no model tokens on API work
2. **Derive from git** — detect the last tag with `git describe`; suggest next semver patch bump
3. **Release notes from PRs** — fetch merged PRs since last tag via API; group by conventional-commit type
4. **Preview before tagging** — show proposed tag and notes; require confirmation before API call
5. **Never push git tags directly** — use the Bitbucket API so the tag appears in the UI

---

## Patterns

### Load config

```bash
BITBUCKET_REPO=$(grep "^BITBUCKET_REPO=" .claude/bitbucket.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
if [[ -z "$BITBUCKET_REPO" ]]; then
  BITBUCKET_REPO=$(git remote get-url origin 2>/dev/null | sed 's|.*bitbucket.org[:/]||;s|\.git$||')
fi
BASE_URL=$(grep "^BITBUCKET_BASE_URL=" ~/.claude/bitbucket-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
BASE_URL=${BASE_URL:-https://bitbucket.org}
TOKEN="${BITBUCKET_API_TOKEN}"
if [[ -z "$TOKEN" ]]; then
  echo "ERROR: BITBUCKET_API_TOKEN is not set. See /bitbucket-setup."
  exit 1
fi
if [[ "$BASE_URL" == "https://bitbucket.org" ]]; then
  REPO_API="https://api.bitbucket.org/2.0/repositories/${BITBUCKET_REPO}"
else
  PROJECT_KEY=$(echo "$BITBUCKET_REPO" | cut -d/ -f1)
  REPO_SLUG=$(echo "$BITBUCKET_REPO" | cut -d/ -f2)
  REPO_API="${BASE_URL}/rest/api/1.0/projects/${PROJECT_KEY}/repos/${REPO_SLUG}"
fi
```

### Detect last tag and suggest next version

```bash
LAST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "")

SUGGESTED_TAG=$(python3 -c "
import sys, re
last = sys.argv[1] if len(sys.argv) > 1 else ''
m = re.match(r'^v?(\d+)\.(\d+)\.(\d+)$', last)
if m:
    major, minor, patch = int(m.group(1)), int(m.group(2)), int(m.group(3))
    print(f'v{major}.{minor}.{patch+1}')
else:
    print('v1.0.0')
" -- "${LAST_TAG}")

echo "Last tag: ${LAST_TAG:-none}  →  Suggested: ${SUGGESTED_TAG}"
```

### Get HEAD commit SHA

```bash
HEAD_SHA=$(git rev-parse HEAD)
echo "Target commit: ${HEAD_SHA}"
```

### Fetch merged PRs since last tag

```bash
# Get last tag date (or epoch if no tags)
if [[ -n "$LAST_TAG" ]]; then
  LAST_TAG_DATE=$(git log -1 --format="%aI" "${LAST_TAG}" 2>/dev/null)
else
  LAST_TAG_DATE="2000-01-01T00:00:00+00:00"
fi

# Fetch merged PRs via API
curl -s -H "Authorization: Bearer ${TOKEN}" \
  "${REPO_API}/pullrequests?state=MERGED&pagelen=50" | python3 -c "
import json, sys
from datetime import datetime, timezone

since_str = '${LAST_TAG_DATE}'
# Parse the since date
try:
    since = datetime.fromisoformat(since_str.replace('Z','+00:00'))
except Exception:
    since = datetime.min.replace(tzinfo=timezone.utc)

r = json.load(sys.stdin)
prs = []
for pr in r.get('values', []):
    updated = datetime.fromisoformat(pr['updated_on'].replace('Z','+00:00'))
    if updated > since:
        prs.append({'id': pr['id'], 'title': pr['title'], 'url': pr['links']['html']['href']})

for pr in prs:
    print(f'#{pr[\"id\"]}\t{pr[\"title\"]}\t{pr[\"url\"]}')
"
```

### Group PR titles into release notes

```python
import re, sys

TYPES = {
    'feat': 'Features',
    'fix': 'Bug Fixes',
    'perf': 'Performance',
    'refactor': 'Refactoring',
    'chore': 'Maintenance',
    'docs': 'Documentation',
    'test': 'Tests',
    'build': 'Build',
    'ci': 'CI/CD',
}

def group_prs(prs):
    groups = {v: [] for v in TYPES.values()}
    groups['Changes'] = []
    for pr_id, title, url in prs:
        m = re.match(r'^(\w+)[\(:]', title)
        if m and m.group(1) in TYPES:
            groups[TYPES[m.group(1)]].append((pr_id, title, url))
        else:
            groups['Changes'].append((pr_id, title, url))
    return {k: v for k, v in groups.items() if v}
```

### Release notes template

```markdown
## {TAG} — {DATE}

### Features
- feat: add order cancellation ([#42](url))

### Bug Fixes
- fix: payment null reference ([#43](url))

### Maintenance
- chore: upgrade EF Core to 9.0 ([#44](url))

**Full changelog:** {BITBUCKET_URL}/{BITBUCKET_REPO}/branches/compare/{TAG}%0D{LAST_TAG}
```

### Create tag — Bitbucket Cloud

```bash
BODY=$(python3 -c "
import json, sys
print(json.dumps({
    'name': sys.argv[1],
    'target': {'hash': sys.argv[2]},
    'message': f'Release {sys.argv[1]}'
}))" -- "${TAG}" "${HEAD_SHA}")

curl -s -X POST \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  "${REPO_API}/refs/tags" \
  --data-raw "$BODY" | python3 -c "
import json, sys
r = json.load(sys.stdin)
if 'name' in r:
    print(f'Tagged: {r[\"name\"]}')
    print(f'URL: {r.get(\"links\",{}).get(\"html\",{}).get(\"href\",\"\")}')
else:
    print('Error:', r.get('error',{}).get('message', json.dumps(r)))
"
```

### Create tag — Bitbucket Server / Data Center

```bash
BODY=$(python3 -c "
import json, sys
print(json.dumps({
    'name': sys.argv[1],
    'startPoint': sys.argv[2],
    'message': f'Release {sys.argv[1]}'
}))" -- "${TAG}" "${HEAD_SHA}")

curl -s -X POST \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  "${REPO_API}/tags" \
  --data-raw "$BODY"
```

---

## Decision Guide

| Scenario | Behaviour |
|----------|-----------|
| `/release` — no args | Detect last tag, suggest next semver patch, show PR list, confirm |
| `/release v2.0.0` | Use specified tag name, skip suggestion |
| `/release --since v1.3.0` | Override the "since" tag for PR collection |
| `/release --draft` | Show release notes only — do NOT call the tag API |
| First release (no tags) | Suggest `v1.0.0`, collect all merged PRs |

---

## Execution

1. Load config and resolve token — abort if missing.
2. Detect `LAST_TAG` from git. Suggest `SUGGESTED_TAG`.
3. If tag name provided in `$ARGUMENTS`, use it; otherwise prompt for confirmation of suggestion.
4. Determine `LAST_TAG_DATE` — use git log if tag exists, else collect all merged PRs.
5. Fetch merged PRs since `LAST_TAG_DATE` via API.
6. Group PR titles into release notes sections.
7. Show preview: proposed tag + target commit + release notes.
8. Confirm: `Tag {TAG} at {HEAD_SHA}? [yes/no]`
9. On confirmation, POST the tag via the API.
10. Print tag URL from the API response.

$ARGUMENTS
