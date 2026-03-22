---
name: branch
description: >
  Manage git branches — naming conventions, creation from the correct base,
  remote tracking, listing, switching, and cleanup of stale branches.
  Load this skill when: "branch", "create branch", "switch branch",
  "rename branch", "delete branch", "stale branches", "list branches",
  "remote tracking", "branch cleanup", "prune branches", "branch naming".
user-invocable: true
argument-hint: "[create <name> | switch <name> | cleanup | list | rename <old> <new>]"
allowed-tools: Bash, Read
---

# Branch

## Core Principles

1. **Branch from the right base** — features from `main`/`develop`, hotfixes from the release tag. Never branch from a stale local state.
2. **Name the work, not the person** — `feature/user-auth` is searchable; `johns-stuff` is not.
3. **Short-lived branches merge cleanly** — branches open longer than 2 weeks diverge and conflict. Keep them focused and small.
4. **Delete after merging** — merged branches are noise. Clean up local and remote.
5. **Check `BRANCH_STRATEGY`** — gitflow, github-flow, and trunk all have different branching rules. Read project config first.

## Patterns

### Create a branch from latest

```bash
git fetch --prune                   # get latest remote state
git switch main                     # start from the correct base
git pull                            # ensure local is up to date
git switch -c feature/user-auth     # create and switch
```

### List branches

```bash
git branch                          # local branches
git branch -r                       # remote branches
git branch -a                       # all (local + remote)
git branch -v                       # with last commit info
git branch --merged main            # branches already merged into main
git branch --no-merged main         # branches NOT yet merged into main
```

### Rename a branch

```bash
git branch -m old-name new-name     # rename local branch
git push origin --delete old-name   # delete old remote branch
git push -u origin new-name         # push renamed branch
```

### Track a remote branch

```bash
git switch -c feature/x --track origin/feature/x  # create local tracking branch
git branch -u origin/feature/x                    # set tracking on existing branch
```

### Cleanup stale branches

```bash
# Prune remote-tracking refs that no longer exist on the remote
git fetch --prune

# List local branches merged into main (safe to delete)
git branch --merged main | grep -v "^\*\|main\|master\|develop"

# Delete merged local branches
git branch --merged main | grep -v "^\*\|main\|master\|develop" | xargs git branch -d

# Delete a remote branch
git push origin --delete feature/old-branch
```

### Check branch divergence

```bash
git log main..feature/my-branch --oneline   # commits in feature, not in main
git log feature/my-branch..main --oneline   # commits in main, not in feature (behind by N)
```

## Anti-patterns

### Branching without fetching first

```
# BAD — branching from stale local state
git switch -c feature/new-thing   # may be 20 commits behind origin/main

# GOOD — always fetch first
git fetch --prune
git switch main && git pull
git switch -c feature/new-thing
```

### Leaving merged branches around

```
# BAD — remote and local both keep old branches
git merge feature/login   # merge happens
# ... branch stays around forever

# GOOD — clean up after merging
git branch -d feature/login
git push origin --delete feature/login
```

### Force-deleting without checking

```
# BAD — losing unmerged work
git branch -D feature/risky   # -D skips the merge check

# GOOD — check first
git branch -d feature/risky   # fails if unmerged → investigate
git log main..feature/risky   # see what would be lost
```

## Decision Guide

| Scenario | Command |
|----------|---------|
| Start new feature | `git fetch --prune && git switch main && git pull && git switch -c feature/<name>` |
| Switch to existing branch | `git switch <branch>` |
| Rename current branch | `git branch -m <new-name>` |
| Delete merged local branch | `git branch -d <branch>` |
| Delete remote branch | `git push origin --delete <branch>` |
| Clean up stale remote refs | `git fetch --prune` |
| See merged/unmerged branches | `git branch --merged main` / `--no-merged` |
| Check if branch is behind main | `git log feature..main --oneline` |
