---
name: repo-health
description: >
  Audit and maintain git repository health — prune stale branches, find large
  objects, run garbage collection, check integrity, and clean up noise.
  Load this skill when: "repo health", "git gc", "large files", "repository size",
  "prune branches", "stale branches", "git fsck", "clean up repo",
  "orphaned branches", "repo maintenance", "git large objects", "git bloat".
user-invocable: true
argument-hint: "[--branches | --size | --gc | --integrity | --all]"
allowed-tools: Bash, Read
---

# Repo Health

## Core Principles

1. **Fetch with prune regularly** — `git fetch --prune` removes remote-tracking branches for deleted remotes. Run it before any branch audit.
2. **Large objects accumulate silently** — accidentally committed binaries or generated files stay in history forever unless explicitly removed. Check periodically.
3. **GC is safe and fast on most repos** — `git gc` compresses loose objects and packs refs. It's a maintenance operation, not a cleanup that loses data.
4. **fsck finds corruption** — `git fsck` verifies object integrity. Run after unusual operations (force-push, disk errors, clone failures).
5. **Report before acting** — always show the user what will be removed before running any cleanup.

## Patterns

### Prune stale remote-tracking refs

```bash
git fetch --prune                  # remove refs to deleted remote branches
git remote prune origin            # prune only origin's stale refs
```

### Audit local branches

```bash
# Branches already merged into main (safe to delete)
git branch --merged main | grep -v "^\*\|main\|master\|develop"

# Branches NOT merged (may contain unmerged work)
git branch --no-merged main

# Branches by last commit date (find stale ones)
git branch -v | sort -k3          # sort by last activity
git for-each-ref --sort=committerdate refs/heads/ \
  --format="%(committerdate:short) %(refname:short)"
```

### Find large objects in history

```bash
# List top 10 largest objects in the repo (by size)
git rev-list --objects --all \
  | git cat-file --batch-check='%(objecttype) %(objectname) %(objectsize) %(rest)' \
  | sort -k3 -rn \
  | head -10

# Quick: check total pack size
git count-objects -vH
```

### Run garbage collection

```bash
git gc                             # standard GC (safe, fast)
git gc --aggressive                # deeper optimization (slower, run occasionally)
git gc --prune=now                 # prune loose objects immediately
```

### Integrity check

```bash
git fsck                           # check object integrity
git fsck --lost-found              # also write dangling objects to .git/lost-found/
```

### Clean untracked files

```bash
git clean -nd                      # dry run — see what would be removed
git clean -fd                      # remove untracked files and directories
git clean -fdx                     # also remove gitignored files (e.g., build output)
```

### Check repo size

```bash
git count-objects -vH             # loose objects + pack files + total size
du -sh .git                       # total .git directory size
```

## Anti-patterns

### Deleting branches without checking for unmerged commits

```
# BAD — force-deleting potentially valuable work
git branch --merged main | xargs git branch -d   # this is safe
git branch | xargs git branch -D                  # this is NOT — deletes unmerged too

# GOOD — check unmerged branches before acting on them
git branch --no-merged main       # review each one manually
```

### Running git clean without a dry run

```
# BAD — permanently deleting untracked files without preview
git clean -fd

# GOOD — dry run first, then confirm
git clean -nd    # shows what would be deleted
git clean -fd    # run only after reviewing
```

### Ignoring large file warnings at commit time

```
# BAD — committing large binaries to history
git add assets/large-video.mp4   # 250MB in history forever
git commit -m "add video"

# GOOD — use .gitignore or git-lfs for large files
echo "*.mp4" >> .gitignore
# or set up git-lfs for binary assets
```

## Decision Guide

| Scenario | Command |
|----------|---------|
| Remove stale remote refs | `git fetch --prune` |
| Find merged local branches | `git branch --merged main` |
| Delete all merged local branches | `git branch --merged main \| grep -v main \| xargs git branch -d` |
| Find what's eating disk space | `git count-objects -vH` |
| Find large objects in history | `git rev-list --objects --all \| git cat-file --batch-check=...` |
| Routine maintenance | `git gc` |
| Deep cleanup (monthly) | `git gc --aggressive --prune=now` |
| Check for corruption | `git fsck` |
| Preview untracked file cleanup | `git clean -nd` |
| Remove untracked files | `git clean -fd` (after dry run) |

## Execution

1. Parse `$ARGUMENTS` — detect scope: `--branches`, `--size`, `--gc`, `--integrity`, `--all`
2. **--branches**: fetch --prune, list merged branches, show stale branch report, confirm before deleting
3. **--size**: run `git count-objects -vH` and large-object rev-list scan; report top 10 by size
4. **--gc**: run `git gc` (suggest `--aggressive` if repo is large or fragmented)
5. **--integrity**: run `git fsck` and report any dangling or corrupt objects
6. **--all (default)**: run all checks in sequence, produce summary report
7. Always show what will be removed before any destructive operation
8. For untracked file cleanup: `git clean -nd` first, then prompt before `git clean -fd`

$ARGUMENTS
