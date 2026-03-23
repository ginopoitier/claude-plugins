# Git Kit Config — Project Level
<!--
  This file lives at .claude/git.config.md inside each repository.
  Commit it to version control — it documents the repo's git conventions for the whole team.

  Run /git-setup --project to generate interactively.
  Settings here override the user-level ~/.claude/git-kit.config.md where they overlap.
-->

## Branch Strategy
```
BRANCH_STRATEGY=github-flow              # gitflow | trunk | github-flow
DEFAULT_BRANCH=main                      # default/main branch for this repo
PROTECTED_BRANCHES=main,master           # comma-separated — never force-push or commit directly
```

## Commit Convention
```
COMMIT_CONVENTION=                       # conventional | freeform | semantic
                                         # leave blank to inherit from user GIT_MERGE_STRATEGY
```

## Remote
```
GIT_REMOTE=origin                        # default remote name
```
