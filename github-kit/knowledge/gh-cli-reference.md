# GitHub CLI (`gh`) Reference

> Quick reference for `gh` commands used by github-kit skills. All commands run against the repo inferred from `git remote get-url origin` unless overridden.

## Pull Requests

### Create

```bash
# Basic — prompts for title/body interactively
gh pr create

# Non-interactive with heredoc body
gh pr create --title "feat: add order validation (ORD-456)" --body "$(cat <<'EOF'
## Summary
- Added FluentValidation to CreateOrder command
- Validates: non-empty CustomerId, at least 1 item, positive quantities

## Test plan
- [ ] Run integration tests: `dotnet test --filter Category=Integration`
- [ ] Verify 400 returned on invalid input
EOF
)"

# Draft PR
gh pr create --draft --title "WIP: order validation"

# Target a specific base branch
gh pr create --base develop

# Assign reviewer and add labels
gh pr create --reviewer "teamlead-username" --label "feature,needs-review"
```

### View and Diff

```bash
# View PR summary
gh pr view 42
gh pr view 42 --web          # open in browser

# Get the raw diff for a PR (used by /review skill)
gh pr diff 42

# List open PRs in current repo
gh pr list
gh pr list --state open --author "@me"
gh pr list --label "needs-review"
```

### Manage

```bash
# Check PR status (CI, reviews, merge-ability)
gh pr checks 42

# Request a review
gh pr review 42 --request "username"

# Approve (human action — don't do this from Claude)
gh pr review 42 --approve --body "LGTM"

# Merge (human action — always confirm before running)
gh pr merge 42 --squash --delete-branch
gh pr merge 42 --merge

# Close without merging
gh pr close 42
```

### Labels and Milestones

```bash
# List available labels
gh label list

# Add label to a PR
gh pr edit 42 --add-label "breaking-change"

# Set milestone
gh pr edit 42 --milestone "v2.0"
```

---

## Releases

### Create a Release

```bash
# Auto-generate release notes from commits since last tag
gh release create v1.2.0 --generate-notes

# With a custom title and body
gh release create v1.2.0 \
  --title "v1.2.0 — Order Validation" \
  --notes "$(cat CHANGELOG.md)"

# Pre-release
gh release create v1.2.0-rc.1 --prerelease --generate-notes

# Draft release (not published until edited)
gh release create v1.2.0 --draft --generate-notes
```

### Upload Assets

```bash
# Attach build artifacts
gh release create v1.2.0 ./dist/app-linux.tar.gz ./dist/app-win.zip --generate-notes

# Upload to existing release
gh release upload v1.2.0 ./dist/new-asset.zip
```

### View Releases

```bash
gh release list
gh release view v1.2.0
gh release view v1.2.0 --web
```

---

## Issues

```bash
# List issues
gh issue list
gh issue list --label "bug" --state open

# View an issue
gh issue view 123
gh issue view 123 --comments

# Create an issue
gh issue create --title "Fix null ref in payment" --body "..." --label "bug"

# Close an issue
gh issue close 123 --comment "Fixed in #456"
```

---

## Repository

```bash
# View repo info
gh repo view
gh repo view owner/repo

# Clone a repo
gh repo clone owner/repo

# List workflows
gh workflow list

# Run a workflow manually
gh workflow run ci.yml --ref feature/my-branch

# View workflow run status
gh run list
gh run view 12345678
gh run watch 12345678   # stream output live
```

---

## Configuration Reading Patterns

Used by github-kit skills to extract config values from markdown files:

```bash
# Read GITHUB_ORG from user config
GITHUB_ORG=$(grep "^GITHUB_ORG=" ~/.claude/github-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')

# Read GITHUB_REPO from project config (preferred — more specific)
GITHUB_REPO=$(grep "^GITHUB_REPO=" .claude/github.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')

# Fallback: infer from git remote
GITHUB_REPO=$(git remote get-url origin 2>/dev/null | sed 's/.*github.com[:/]\(.*\)\.git/\1/' | sed 's/.*github.com[:/]\(.*\)/\1/')

# Read DEFAULT_BRANCH
DEFAULT_BRANCH=$(grep "^DEFAULT_BRANCH=" .claude/github.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEFAULT_BRANCH=${DEFAULT_BRANCH:-main}

# Read draft preference
GITHUB_PR_DRAFT=$(grep "^GITHUB_PR_DRAFT_BY_DEFAULT=" ~/.claude/github-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
```

---

## Branch Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Feature | `feature/{jira-or-description}` | `feature/ORD-456-order-validation` |
| Bug fix | `fix/{jira-or-description}` | `fix/PAY-89-null-ref` |
| Hotfix | `hotfix/{version}-{description}` | `hotfix/1.2.1-payment-crash` |
| Release | `release/{version}` | `release/2.0.0` |
| Chore | `chore/{description}` | `chore/upgrade-ef-core` |

---

## PR Title Conventions

```
{type}: {short description} [{JIRA_KEY} or #{issue}]

Types: feat | fix | chore | docs | refactor | test | perf | ci | build

Examples:
  feat: add order cancellation (ORD-456)
  fix: payment null reference on empty cart (#123)
  chore: upgrade EF Core to 10.0
  refactor: extract OrderValidator to separate class
```

---

## Useful Flags Reference

| Flag | Applies to | Effect |
|------|-----------|--------|
| `--draft` | `pr create` | Mark as draft |
| `--web` | `pr view`, `release view` | Open in browser |
| `--generate-notes` | `release create` | Auto changelog from commits |
| `--squash` | `pr merge` | Squash-merge |
| `--delete-branch` | `pr merge` | Delete branch after merge |
| `--json {fields}` | most commands | Machine-readable JSON output |
| `--jq {expr}` | most commands | JQ filter on JSON output |

### JSON output examples

```bash
# Get PR number and title as JSON
gh pr list --json number,title

# Extract just the PR URL
gh pr view 42 --json url --jq '.url'

# Get all reviewers on a PR
gh pr view 42 --json reviewRequests --jq '[.reviewRequests[].login]'
```
