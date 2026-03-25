# Bitbucket Concepts

## Resource Hierarchy

```
Workspace  (org-level — "acme-corp")
└── Project  (optional grouping — "ORDER SERVICE")
    └── Repository  (code repo — "order-service")
        ├── Pull Requests
        ├── Branches
        ├── Tags
        └── Pipelines (CI/CD — out of scope for bitbucket-kit)
```

- **Workspace** — the top-level container, equivalent to a GitHub org. Identified by its slug (e.g. `acme-corp`).
- **Repository** — identified by `{workspace}/{repo-slug}` (e.g. `acme-corp/order-service`). This is `BITBUCKET_REPO` in config.
- **Project** — optional grouping within a workspace. Not required for API operations.

> **Bitbucket Server** uses `Project Key` + `Repository Slug` instead of workspace/repo. The Project Key is typically uppercase (e.g. `ORD`).

## Pull Request States

| State | Meaning |
|-------|---------|
| `OPEN` | Active, awaiting review/merge |
| `MERGED` | Accepted and merged into the destination branch |
| `DECLINED` | Closed without merging |
| `SUPERSEDED` | Replaced by another PR |

## PR Participant Roles

Each PR participant has a role that affects what they can do:

| Role | Meaning |
|------|---------|
| `AUTHOR` | Created the PR |
| `REVIEWER` | Explicitly added as a reviewer |
| `PARTICIPANT` | Commented or interacted but not a formal reviewer |

Participant states:
- `approved` — approved the PR
- `needs_work` (Server) / via `request-changes` endpoint (Cloud) — requested changes
- `unapproved` — no verdict yet

## PR Diff Format

The diff endpoint returns a **unified diff** (standard GNU diff format). Key conventions:
- Lines starting with `+` were added
- Lines starting with `-` were removed
- `@@` markers show the hunk position in the file: `@@ -{old_start},{old_count} +{new_start},{new_count} @@`
- File boundary markers: `diff --git a/{file} b/{file}`

For inline comment `to` position: use the new file's line number, not the diff hunk offset.

## Branch Naming Conventions (common)

| Pattern | Type |
|---------|------|
| `feature/{JIRA-KEY}-{desc}` | New feature linked to a ticket |
| `bugfix/{JIRA-KEY}-{desc}` | Bug fix |
| `hotfix/{JIRA-KEY}-{desc}` | Urgent fix to production |
| `release/{version}` | Release preparation branch |
| `chore/{desc}` | Non-functional changes |

Extract the Jira ticket key from a branch name:
```bash
BRANCH=$(git branch --show-current)
JIRA_KEY=$(echo "$BRANCH" | python3 -c "import re,sys; m=re.search(r'[A-Z]+-\d+', sys.stdin.read()); print(m.group() if m else '')")
```

## Token Scopes

Bitbucket Personal Access Token scopes required for bitbucket-kit operations:

| Operation | Scope needed |
|-----------|-------------|
| Read PRs, diffs | Repositories → Read |
| Create/update PRs, post comments | Pull requests → Write |
| Create tags | Repositories → Write |
| Approve PRs | Pull requests → Write |

## Bitbucket Cloud vs Server/DC Differences

| Feature | Cloud | Server / Data Center |
|---------|-------|---------------------|
| API base | `https://api.bitbucket.org/2.0` | `{BASE_URL}/rest/api/1.0` |
| Pagination | `next` URL in response | `isLastPage` + `nextPageStart` |
| PR query filter | `?q=state="MERGED"` | `?state=MERGED` (no query language) |
| Request changes | `/pullrequests/{id}/request-changes` | Not available; use comment instead |
| Auth header | `Authorization: Bearer {token}` | `Authorization: Bearer {token}` (v7.0+), or Basic (older) |
| Inline comment position | `inline.to` = file line number | `anchor.line` = file line number |
| Tag creation | `POST /refs/tags` | `POST /tags` |
| Pipelines | Available, separate API | Not available (use Bamboo or Jenkins) |

## Config File Locations

| Level | File | Contains |
|-------|------|----------|
| User/device | `~/.claude/bitbucket-kit.config.md` | Workspace, base URL, PR defaults |
| Project | `.claude/bitbucket.config.md` | Repo slug, default branch |

Project config overrides user config for any overlapping keys. Both are plain text with `KEY=value` lines that can be parsed with `grep`.
