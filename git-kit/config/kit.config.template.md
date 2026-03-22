# Git Kit Config

## User / Device Level
# Copy to: ~/.claude/git-kit.config.md
# Run /git-setup to configure interactively

# Personal git identity
GIT_USER_NAME=
GIT_USER_EMAIL=

# Default branch name for new repos
GIT_DEFAULT_BRANCH=main

# GPG signing key fingerprint (leave blank to disable signing)
GIT_SIGNING_KEY=

# Preferred merge strategy: merge | rebase | squash
GIT_MERGE_STRATEGY=merge

# Commit message style: conventional | freeform
# conventional = enforces feat/fix/docs/chore/etc. prefixes
# freeform = imperative mood only, no type prefix required
COMMIT_STYLE=freeform

---

## Project Level
# Copy to: .claude/git.config.md in each repo
# Run /git-setup --project to configure interactively

# Branch strategy: gitflow | trunk | github-flow
BRANCH_STRATEGY=github-flow

# Default/main branch for this repo
DEFAULT_BRANCH=main

# Comma-separated protected branch names (never force-push, never commit directly)
PROTECTED_BRANCHES=main,master,release

# Commit convention for this repo (overrides user COMMIT_STYLE)
# conventional | freeform | semantic
COMMIT_CONVENTION=

# Default remote name
GIT_REMOTE=origin
