# Safety Rules

## DO
- Always run `git status` and `git diff` before any destructive operation
- Use `--dry-run` when available before bulk operations
- Create a backup branch before rebasing: `git branch backup/branch-name`
- Prefer `git revert` over `git reset --hard` for commits already pushed to shared branches
- Use `git reflog` to recover from mistakes — almost nothing is truly lost within 90 days
- Confirm the current branch before force-pushing: `git branch --show-current`
- Use `--force-with-lease` instead of `--force` when force-pushing is unavoidable

## DON'T
- Never `git push --force` to main, master, or any protected branch
- Never `git reset --hard` without first reviewing what will be discarded
- Never rebase commits that others have already pulled from a shared branch
- Never `git clean -fd` without first doing `git clean -nd` (dry run)
- Never run destructive commands autonomously — always confirm with the user

## Destructive Commands — Always Confirm First

Before running any of the following, explicitly confirm with the user:

| Command | Risk | Safer Alternative |
|---------|------|-------------------|
| `git reset --hard` | Discards uncommitted changes permanently | `git stash` first |
| `git push --force` | Overwrites remote history | `--force-with-lease` |
| `git clean -fd` | Deletes untracked files permanently | `git clean -nd` first |
| `git rebase` (pushed branch) | Rewrites shared history | `git merge` instead |
| `git branch -D` | Force-deletes unmerged branch | `git branch -d` first |
| `git stash drop/clear` | Permanently removes stashed work | `git stash list` first |
