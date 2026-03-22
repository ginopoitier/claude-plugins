---
name: undo
description: >
  Choose the right undo tool for the situation — restore, reset, revert, or
  reflog recovery. Covers undoing working tree changes, staged changes, commits,
  and recovering from mistakes. Load this skill when: "undo", "revert",
  "git reset", "undo commit", "unstage", "discard changes", "go back",
  "undo last commit", "git restore", "reflog", "recover lost commit",
  "undo push", "reset to previous commit".
user-invocable: true
argument-hint: "[--working-tree | --staged | --commit | --pushed | --recover]"
allowed-tools: Bash, Read
---

# Undo

## Core Principles

1. **Match the tool to the scope** — restore (working tree), reset (local commits), revert (published commits). Using the wrong one is how history gets lost.
2. **Reflog is the safety net** — git keeps a reflog for 90 days. Almost any mistake can be recovered with `git reflog`.
3. **Revert is safe for shared history** — `git revert` creates a new commit that undoes changes. It never rewrites history.
4. **Reset is unsafe for shared history** — `git reset` rewrites history. Only use it on commits that haven't been pushed.
5. **Always check before destructive ops** — `git diff`, `git log`, `git status` first.

## Patterns

### Undo working tree changes (unstaged)

```bash
# Discard all changes in working tree (irreversible)
git restore .

# Discard changes to a specific file
git restore src/auth/login.ts

# Restore a file to a specific commit's version
git restore --source HEAD~2 src/auth/login.ts
```

### Undo staged changes (unstage without discarding)

```bash
# Unstage all files (keep changes in working tree)
git restore --staged .

# Unstage a specific file
git restore --staged src/auth/login.ts
```

### Undo local commits (not pushed)

```bash
# Undo last commit, keep changes staged
git reset --soft HEAD~1

# Undo last commit, keep changes unstaged
git reset HEAD~1

# Undo last N commits, keep changes unstaged
git reset HEAD~3

# Undo last commit, DISCARD changes (irreversible)
git reset --hard HEAD~1
```

### Undo pushed commits (safe — creates new commit)

```bash
# Revert a specific commit (creates an inverse commit)
git revert <sha>

# Revert a merge commit
git revert -m 1 <merge-sha>

# Revert a range of commits
git revert <oldest-sha>..<newest-sha>
```

### Recover lost commits via reflog

```bash
# See all recent HEAD movements (including deleted commits)
git reflog

# Find the commit sha before the mistake
# abc123 HEAD@{3}: commit: the thing I accidentally lost

# Recover it
git switch -c recovery/lost-work abc123  # create branch at lost commit
# or
git cherry-pick abc123                    # apply it to current branch
# or
git reset --hard abc123                   # return HEAD to that commit (local only)
```

### Undo a bad merge

```bash
# If not pushed — reset to before the merge
git reset --hard ORIG_HEAD

# If pushed — revert the merge commit
git revert -m 1 <merge-commit-sha>
```

## Anti-patterns

### Using `reset --hard` on pushed commits

```
# BAD — rewrites history that others have pulled
git reset --hard HEAD~3
git push --force

# GOOD — revert instead (safe, reversible, preserves history)
git revert HEAD~2..HEAD
git push
```

### Discarding changes without checking what will be lost

```
# BAD — losing hours of work
git restore .   # all uncommitted work gone

# GOOD — check first, stash if unsure
git diff        # see what would be lost
git stash push -m "safety save before restore"
git restore .
```

### Confusing restore and reset

```
# restore — operates on files in working tree or staging area
git restore src/file.ts              # discard working tree changes
git restore --staged src/file.ts     # unstage

# reset — operates on commits and moves HEAD
git reset HEAD~1                     # undo last commit
git reset --hard HEAD~1              # undo last commit + discard changes
```

## Decision Guide

| Scenario | Command | Safe for Pushed? |
|----------|---------|-----------------|
| Discard uncommitted file changes | `git restore <file>` | n/a |
| Unstage a file | `git restore --staged <file>` | n/a |
| Undo last commit, keep changes | `git reset --soft HEAD~1` | Only if not pushed |
| Undo last commit, discard changes | `git reset --hard HEAD~1` | Only if not pushed |
| Undo a pushed commit safely | `git revert <sha>` | Yes |
| Undo a bad merge (not pushed) | `git reset --hard ORIG_HEAD` | Only if not pushed |
| Undo a bad merge (pushed) | `git revert -m 1 <merge-sha>` | Yes |
| Recover a deleted commit | `git reflog` → `git switch -c recovery <sha>` | Yes |
| Recover deleted stash | `git fsck --lost-found` | n/a |
