---
name: conflict-resolve
description: >
  Step-by-step merge and rebase conflict resolution with strategy selection.
  Load this skill when: "merge conflict", "resolve conflict", "conflict markers",
  "git conflict", "<<<<<<< HEAD", "rebase conflict", "conflict resolution",
  "accept ours", "accept theirs", "conflicted files", "git mergetool".
user-invocable: true
argument-hint: "[--ours | --theirs | --manual | --abort]"
allowed-tools: Bash, Read, Grep
---

# Conflict Resolve

## Core Principles

1. **Understand both sides before resolving** — read both the incoming and current change. Context matters; don't blindly pick one.
2. **Abort is always safe** — `git merge --abort` or `git rebase --abort` returns to the pre-operation state cleanly.
3. **Not every conflict is 50/50** — often one side is clearly correct. Don't merge both when only one is right.
4. **Test after resolving** — a conflict that compiles doesn't mean the logic is correct. Run tests.
5. **Mark resolved with `git add`** — git doesn't auto-detect resolution. You must explicitly stage resolved files.

## Patterns

### Identify conflicted files

```bash
git status                        # shows files with "both modified"
git diff --name-only --diff-filter=U  # list only conflicted files
```

### Read conflict markers

```
<<<<<<< HEAD (or ours)
current branch content
=======
incoming branch content
>>>>>>> feature/incoming (or theirs)
```

- **HEAD / ours** = the branch you're merging INTO (your current branch)
- **theirs** = the branch being merged IN (the incoming branch)

### Resolution strategies

**Manual resolution (most conflicts):**
```bash
# Open the file, read both sides, edit to the correct final state
# Remove ALL conflict markers (<<<<<<<, =======, >>>>>>>)
git add <resolved-file>
git merge --continue   # or git rebase --continue
```

**Accept ours entirely (current branch wins):**
```bash
git checkout --ours <file>
git add <file>
```

**Accept theirs entirely (incoming branch wins):**
```bash
git checkout --theirs <file>
git add <file>
```

**Resolve all conflicts using one strategy:**
```bash
# Accept all as ours
git merge -X ours

# Accept all as theirs
git merge -X theirs
```

### After resolving all files

```bash
git status                        # confirm no more conflicts
git merge --continue              # complete the merge
# or
git rebase --continue             # complete the rebase
```

### Abort if overwhelmed

```bash
git merge --abort                 # return to pre-merge state
git rebase --abort                # return to pre-rebase state
```

### Use a visual merge tool

```bash
git mergetool                     # opens configured merge tool
# Common tools: vimdiff, vscode, meld, kdiff3
# Configure: git config --global merge.tool vscode
```

## Anti-patterns

### Blindly accepting one side without reading

```
# BAD — accepting "theirs" for every conflict without review
git checkout --theirs .
git add .
git merge --continue
# Silent data loss — your changes are gone

# GOOD — read each conflict, understand what changed and why
git diff HEAD...MERGE_HEAD -- conflicted-file.ts
# Then decide per-conflict
```

### Leaving conflict markers in the file

```
# BAD — committing with markers still present
<<<<<<< HEAD
const timeout = 5000;
=======
const timeout = 10000;
>>>>>>> feature/config
# This compiles in some languages but is logically broken

# GOOD — search for markers before staging
git diff --check   # warns about conflict markers
grep -rn "<<<<<<" .
```

### Not testing after resolution

```
# BAD — resolve conflicts → commit → push → CI fails
git add . && git merge --continue

# GOOD — resolve → run tests → commit
git add <resolved>
npm test           # or dotnet test, pytest, etc.
git merge --continue
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| See which files conflict | `git status` or `git diff --name-only --diff-filter=U` |
| Need to understand what changed | `git diff HEAD...MERGE_HEAD -- <file>` |
| Current branch is clearly right | `git checkout --ours <file>` → `git add <file>` |
| Incoming branch is clearly right | `git checkout --theirs <file>` → `git add <file>` |
| Need to merge both sides manually | Edit file → remove markers → `git add <file>` |
| Too complex — want to restart | `git merge --abort` or `git rebase --abort` |
| Conflict in binary file | `git checkout --ours <file>` or `--theirs` (can't merge manually) |
| All conflicts resolved | `git merge --continue` or `git rebase --continue` |

## Execution

1. Run `git status` to list conflicted files
2. Parse `$ARGUMENTS` — detect strategy flag: `--ours`, `--theirs`, `--manual`, `--abort`
3. **--abort** → run `git merge --abort` or `git rebase --abort` immediately
4. **--ours / --theirs** → apply strategy to all conflicts, stage, continue
5. **--manual (default)** → for each conflicted file:
   - Read the file and identify each conflict block
   - Show both sides with context, explain the difference
   - Apply the resolution, remove all conflict markers
   - Stage with `git add <file>`
6. After all files resolved: run `git diff --check` to confirm no stray markers
7. Prompt user to run tests before `git merge --continue` / `git rebase --continue`

$ARGUMENTS
