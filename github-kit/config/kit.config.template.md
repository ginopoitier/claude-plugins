# GitHub Kit Config — User / Device Level
<!--
  This file lives at ~/.claude/github-kit.config.md
  Run /github-setup to configure interactively.

  This is the HOME MACHINE config. Work machine uses bitbucket-kit instead.

  Non-sensitive values (URLs, org names) can be plain text.
  Secrets are never stored here — use system env vars if needed.
-->

## GitHub Identity
```
GITHUB_ORG=                               # your GitHub org or username, e.g. acme-corp or johndoe
GITHUB_BASE_URL=https://github.com        # override only for GitHub Enterprise
```

## Pull Request Defaults
```
GITHUB_PR_DRAFT_BY_DEFAULT=false          # true = open all new PRs as drafts
GITHUB_DEFAULT_REVIEWER=                  # default reviewer username or team slug (optional)
```
