# Git Workflow

## DO
- Fetch before starting new work: `git fetch --prune`
- Keep feature branches short-lived — merge frequently
- Prefer `git switch` and `git restore` over the overloaded `git checkout`
- Use `git worktree` for parallel work on multiple branches without stashing
- Pull with rebase to keep linear local history: `git pull --rebase`
- Use `--force-with-lease` when you must force-push — it checks the remote hasn't changed

## DON'T
- Don't mix merge strategies within a project — pick one and apply it consistently
- Don't leave the repo in a detached HEAD state without noting the commit SHA first
- Don't let `git merge` and `git rebase` coexist randomly — agree on one workflow per project
- Don't skip `git fetch` before basing work on a remote branch

## Modern Command Preferences

| Old | Preferred | Reason |
|-----|-----------|--------|
| `git checkout <branch>` | `git switch <branch>` | Dedicated, unambiguous |
| `git checkout -b <branch>` | `git switch -c <branch>` | Clearer intent |
| `git checkout -- <file>` | `git restore <file>` | Explicit file restore |
| `git checkout HEAD~2 -- <file>` | `git restore --source HEAD~2 <file>` | Explicit source |

## Branch Strategy Reference

Check `BRANCH_STRATEGY` in `.claude/git.config.md`:
- `github-flow` — branch from main, PR to main, deploy on merge (default, simplest)
- `gitflow` — feature → develop → release → main, with hotfix branches
- `trunk` — direct commits to main with feature flags, minimal branches

## Deep Reference
@~/.claude/knowledge/git-kit/branch-strategies.md
