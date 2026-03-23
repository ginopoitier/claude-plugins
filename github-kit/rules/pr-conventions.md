# Rule: GitHub PR Conventions

## DO
- Use `gh pr create` for PR creation — it reads from the local git remote automatically
- Include the Jira/GitHub issue reference in the PR title if PM_PROVIDER is set
- Set `--draft` when `GITHUB_PR_DRAFT_BY_DEFAULT=true` or the branch is a WIP
- Assign a reviewer from `GITHUB_DEFAULT_REVIEWER` unless the user specifies otherwise
- Use `gh pr view --web` to open the PR in the browser after creation
- Fetch PR diffs with `gh pr diff {number}` for review tasks

## DON'T
- Don't hardcode the org or repo — always read from `GITHUB_REPO` in project config or infer from `git remote get-url origin`
- Don't create PRs directly to protected branches (main, master, release/*) — check `.claude/git.config.md` PROTECTED_BRANCHES first
- Don't merge PRs from Claude — merging requires human approval

## Reading Config

```bash
# User config
GITHUB_ORG=$(grep "^GITHUB_ORG=" ~/.claude/github-kit.config.md | cut -d= -f2- | tr -d '[:space:]')
GITHUB_PR_DRAFT=$(grep "^GITHUB_PR_DRAFT_BY_DEFAULT=" ~/.claude/github-kit.config.md | cut -d= -f2- | tr -d '[:space:]')

# Project config (preferred — more specific)
GITHUB_REPO=$(grep "^GITHUB_REPO=" .claude/github.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEFAULT_BRANCH=$(grep "^DEFAULT_BRANCH=" .claude/github.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEFAULT_BRANCH=${DEFAULT_BRANCH:-main}
```

## PR Title Format

```
{type}: {short description} ({JIRA_KEY} or #{issue})

Examples:
  feat: add order cancellation (ORD-456)
  fix: payment null reference (#123)
  chore: upgrade EF Core to 9.0
```
