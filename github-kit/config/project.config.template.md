# GitHub Kit Config — Project Level
<!--
  This file lives at .claude/github.config.md inside each repository.
  Commit it to version control — it documents the repo's GitHub coordinates
  for all developers on the project.

  Run /github-setup --project to generate interactively.
-->

## Repository
```
GITHUB_REPO=                              # org/repo, e.g. acme-corp/order-service
DEFAULT_BRANCH=main                       # default branch for PRs and releases
```

## Pull Request Defaults (project-specific overrides)
```
GITHUB_PR_DRAFT_BY_DEFAULT=              # leave blank to use user-level setting
GITHUB_DEFAULT_REVIEWER=                  # reviewer username or team slug (optional)
```
