---
name: rebase
description: >
  Interactive rebase for history cleanup — squash, fixup, reword, drop, and
  reorder commits. Also covers rebasing a branch onto another and resolving
  rebase conflicts. Load this skill when: "rebase", "interactive rebase",
  "squash commits", "fixup", "reword commit", "clean up history",
  "rebase onto", "squash and merge", "git rebase -i", "combine commits",
  "reorder commits", "drop commit".
user-invocable: true
argument-hint: "[--interactive HEAD~N | --onto <base> | --autosquash]"
allowed-tools: Bash, Read
---

# Rebase

## Core Principles

1. **Never rebase shared history** — only rebase commits that haven't been pushed, or that you've confirmed no one else has pulled. Rewriting shared history causes divergence for teammates.
2. **Create a backup branch first** — before any rebase, run `git branch backup/my-branch`. Free insurance.
3. **Interactive rebase is a history editor** — use it to produce a clean, readable commit history before merging.
4. **`--force-with-lease` after rebase** — if you must push after rebasing, always use `--force-with-lease` to avoid overwriting others' work.
5. **Abort is always safe** — `git rebase --abort` returns to the pre-rebase state cleanly.

## Patterns

### Interactive rebase (cleanup before merge)

```bash
# Backup first
git branch backup/my-branch

# Rebase last N commits interactively
git rebase -i HEAD~5
```

The editor opens with a list of commits. Change the action word:

```
pick a1b2c3 Add user model
pick d4e5f6 Fix typo in user model       # → fixup (silent squash)
pick g7h8i9 Add user validation
pick j0k1l2 WIP validation               # → fixup
pick m3n4o5 Add user tests
```

After editing:
```
pick a1b2c3 Add user model
fixup d4e5f6 Fix typo in user model
pick g7h8i9 Add user validation
fixup j0k1l2 WIP validation
pick m3n4o5 Add user tests
```

### Rebase actions reference

| Action | Effect |
|--------|--------|
| `pick` | Keep commit as-is |
| `reword` | Keep commit, edit message |
| `squash` | Merge into previous, combine messages |
| `fixup` | Merge into previous, discard message |
| `drop` | Delete commit entirely |
| `edit` | Pause to amend the commit |

### Autosquash (fixup commits)

```bash
# Create a fixup commit targeting a specific sha
git commit --fixup=a1b2c3

# Rebase with autosquash — fixup commits sort and merge automatically
git rebase -i --autosquash HEAD~N
```

### Update branch with latest main

```bash
git fetch origin
git rebase origin/main          # replay your commits on top of latest main
# Resolve any conflicts, then:
git rebase --continue
```

### Rebase onto a different base

```bash
# Move feature/sub-feature from feature/parent onto main
git rebase --onto main feature/parent feature/sub-feature
```

### Resolving rebase conflicts

```bash
# After a conflict is flagged:
git status                      # see which files conflict
# Edit the conflicted files to resolve
git add <resolved-file>         # mark as resolved
git rebase --continue           # proceed to next commit

# To abort and return to pre-rebase state:
git rebase --abort
```

## Anti-patterns

### Rebasing pushed shared branches

```
# BAD — teammates have pulled this branch
git rebase -i HEAD~10
git push --force

# GOOD — only rebase unpushed commits, or coordinate first
git log origin/feature..HEAD --oneline  # see unpushed commits only
git rebase -i origin/feature            # only rebase the unpushed portion
```

### No backup before complex rebase

```
# BAD — rebasing 20 commits with no safety net
git rebase -i HEAD~20

# GOOD — backup takes 1 second
git branch backup/my-branch
git rebase -i HEAD~20
# If something goes wrong:
git reset --hard backup/my-branch
```

### Using squash when fixup is right

```
# BAD — squash opens an editor to combine messages for every fixup
squash d4e5f6 Fix typo

# GOOD — fixup silently discards the trivial message
fixup d4e5f6 Fix typo
```

## Decision Guide

| Scenario | Command |
|----------|---------|
| Clean up last N commits before PR | `git rebase -i HEAD~N` |
| Update branch with latest main | `git fetch && git rebase origin/main` |
| Squash trivial commits silently | Change to `fixup` in the rebase editor |
| Edit a commit message | Change to `reword` in the rebase editor |
| Delete a commit | Change to `drop` in the rebase editor |
| Move sub-feature to different base | `git rebase --onto <new-base> <old-base> <branch>` |
| Conflict during rebase | Resolve → `git add` → `git rebase --continue` |
| Abort a rebase | `git rebase --abort` |
| Push after rebase | `git push --force-with-lease` |

## Execution

1. Parse `$ARGUMENTS` — detect mode: `--interactive HEAD~N`, `--onto <base>`, `--autosquash`
2. Create a backup branch first: `git branch backup/<current-branch>`
3. **interactive HEAD~N** → open `git rebase -i HEAD~N`; suggest appropriate actions (fixup/squash/reword/drop) based on commit messages
4. **--autosquash** → find all `fixup!` commits with `git log --oneline`, then run `git rebase -i --autosquash HEAD~N`
5. **--onto `<base>`** → run `git rebase --onto <new-base> <old-base> <branch>`
6. **update with main (no flag)** → `git fetch origin && git rebase origin/main`
7. If conflicts arise: resolve per file, `git add`, `git rebase --continue`
8. After completion: remind user to use `--force-with-lease` if pushing

$ARGUMENTS
