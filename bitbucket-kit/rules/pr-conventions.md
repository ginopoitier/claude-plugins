# Rule: Bitbucket PR Conventions

## DO
- Use the Bitbucket REST API v2 for all PR operations (`https://api.bitbucket.org/2.0/`)
- Read `BITBUCKET_WORKSPACE` and `BITBUCKET_REPO` from config — never hardcode them
- Resolve `BITBUCKET_API_TOKEN` from the `${BITBUCKET_API_TOKEN}` env var at runtime
- Include the Jira ticket reference in the PR title when `PM_PROVIDER=jira` is set in dev-kit config
- Check `.claude/git.config.md` PROTECTED_BRANCHES before targeting a base branch
- Use `--data-raw` with `curl` for API calls to avoid shell escaping issues with JSON

## DON'T
- Don't store the API token in any config file — it must come from the system env var
- Don't merge PRs — merging requires human approval
- Don't use the Bitbucket Pipelines API — that's dev-kit `/ci-cd` territory

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
  BITBUCKET_REPO=$(git remote get-url origin 2>/dev/null | sed 's|.*bitbucket.org[:/]||' | sed 's|\.git$||')
fi

# API token from env
TOKEN="${BITBUCKET_API_TOKEN}"
```

## PR Title Format

```
{type}: {short description} ({JIRA_KEY})

Examples:
  feat: add order cancellation (ORD-456)
  fix: payment null reference (ORD-789)
  chore: upgrade EF Core to 9.0
```

## API Base URL

```bash
API="https://api.bitbucket.org/2.0"
# Bitbucket Server/Data Center: read BITBUCKET_BASE_URL from config
# API base becomes: ${BITBUCKET_BASE_URL}/rest/api/1.0
```
