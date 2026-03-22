# Branch Conventions

## DO
- Prefix branches by type: `feature/`, `fix/`, `chore/`, `release/`, `hotfix/`
- Use kebab-case: `feature/user-auth`, not `feature/userAuth` or `feature/user_auth`
- Keep names short and descriptive (3–5 words max after the prefix)
- Branch from the correct base: features from `main`/`develop`, hotfixes from the release tag
- Delete branches after merging — keep the remote clean
- Run `git fetch --prune` before branching to start from latest

## DON'T
- Don't commit directly to `main`, `master`, or any branch listed in `PROTECTED_BRANCHES`
- Don't reuse branch names after merging
- Don't name branches by person: `johns-fix` — branches belong to the work, not the author
- Don't let branches grow stale (> 2 weeks without activity → rebase or close)
- Don't include only a ticket number: `feature/ABC-123` is ok, `ABC-123` alone is not

## Naming Patterns

```
feature/{short-description}      # new functionality
fix/{bug-description}            # bug fixes
chore/{task-description}         # tooling, deps, config
refactor/{area}                  # restructuring without behavior change
hotfix/{critical-fix}            # emergency production fix
release/{version}                # release preparation (e.g. release/2.4.0)
```

## Deep Reference
@~/.claude/knowledge/git-kit/branch-strategies.md
