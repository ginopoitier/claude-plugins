# Bitbucket Kit Config — User / Device Level
<!--
  This file lives at ~/.claude/bitbucket-kit.config.md
  Run /bitbucket-setup to configure interactively.

  This config is per-machine and can differ across devices.

  SECRETS — never store your API token here.
  Set it once as a system environment variable:
    Windows:  setx BITBUCKET_API_TOKEN "your-token"
    bash:     export BITBUCKET_API_TOKEN="your-token"  (add to ~/.bashrc)

  Get your token: Bitbucket → Personal settings → Personal access tokens
-->

## Bitbucket Identity
```
BITBUCKET_WORKSPACE=                      # your Bitbucket workspace slug, e.g. acme-corp
BITBUCKET_BASE_URL=https://bitbucket.org  # override only for Bitbucket Server/Data Center
BITBUCKET_API_TOKEN=${BITBUCKET_API_TOKEN} # SECRET — resolved from system env at runtime
```

## Pull Request Defaults
```
BITBUCKET_PR_DRAFT_BY_DEFAULT=false       # true = open all new PRs as drafts
BITBUCKET_DEFAULT_REVIEWER=               # default reviewer username (optional)
```
