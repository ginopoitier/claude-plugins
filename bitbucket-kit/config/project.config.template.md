# Bitbucket Kit Config — Project Level
<!--
  This file lives at .claude/bitbucket.config.md inside each repository.
  Commit it to version control — it documents the repo's Bitbucket coordinates
  for all developers on the project.

  Run /bitbucket-setup --project to generate interactively.
-->

## Repository
```
BITBUCKET_REPO=                           # workspace/repo-slug, e.g. acme-corp/order-service
DEFAULT_BRANCH=main                       # default branch for PRs
```

## Pull Request Defaults (project-specific overrides)
```
BITBUCKET_PR_DRAFT_BY_DEFAULT=           # leave blank to use user-level setting
BITBUCKET_DEFAULT_REVIEWER=              # reviewer username (optional)
```
