---
name: stash
description: >
  Manage git stashes — named stashes, partial stash, inspection, applying,
  recovering lost stashes, and converting stashes to branches.
  Load this skill when: "stash", "git stash", "save changes", "stash pop",
  "stash apply", "stash list", "stash drop", "stash branch", "recover stash",
  "partial stash", "stash show", "stash untracked".
user-invocable: true
argument-hint: "[save <message> | list | show | pop | apply | drop | branch <name>]"
allowed-tools: Bash, Read
---

# Stash

## Core Principles

1. **Name your stashes** — `git stash push -m "description"` makes stashes identifiable. Anonymous stashes get confusing after more than two.
2. **Stash is temporary** — don't use stash as long-term storage. Create a branch for work you're setting aside for more than a day.
3. **`apply` over `pop` when unsure** — `pop` deletes the stash after applying. `apply` keeps it as a safety net until you're sure the apply succeeded cleanly.
4. **Stash includes staged by default** — untracked files need `--include-untracked`. New files you haven't staged are not stashed otherwise.
5. **Nothing is truly lost** — even dropped stashes can often be recovered via `git fsck`.

## Patterns

### Save a named stash

```bash
git stash push -m "WIP: user auth middleware"
git stash push -m "experiment: new cache layer" --include-untracked
```

### Partial stash (specific files)

```bash
git stash push -m "only auth changes" -- src/auth/ src/middleware/auth.ts
git stash push -p   # interactive — pick specific hunks to stash
```

### List and inspect stashes

```bash
git stash list                    # list all stashes with index and message
git stash show stash@{0}          # summary of what's in the latest stash
git stash show -p stash@{0}       # full diff of stash contents
git stash show -p stash@{2}       # inspect a specific stash by index
```

### Apply stashes

```bash
git stash pop                     # apply latest stash and remove it
git stash apply stash@{2}         # apply a specific stash, keep it in list
git stash pop stash@{1}           # apply and remove a specific stash
```

### Drop and clear

```bash
git stash drop stash@{1}          # remove a specific stash
git stash clear                   # remove ALL stashes (irreversible — confirm first)
```

### Convert a stash to a branch

```bash
# Creates a branch, checks it out, applies the stash, drops it if clean
git stash branch feature/my-experiment stash@{0}
```

### Recover a dropped stash

```bash
# Find dangling commit objects (stashes are commits internally)
git fsck --lost-found | grep commit

# Inspect each to find your stash content
git show <sha>

# If found, apply it
git stash apply <sha>
```

## Anti-patterns

### Anonymous stashes

```
# BAD — can't tell what this is after context switches
git stash
git stash
git stash
# stash@{0}: WIP on main: abc123 some commit
# stash@{1}: WIP on main: abc123 some commit
# stash@{2}: WIP on feature/x: def456 other commit

# GOOD — named stashes are immediately identifiable
git stash push -m "WIP: half-done error handling refactor"
```

### Popping into a dirty working tree

```
# BAD — pop fails or creates conflicts when working tree is dirty
git stash pop   # conflicts with current uncommitted changes

# GOOD — ensure clean state before applying
git status      # check nothing is staged/modified
git stash apply stash@{0}   # apply without deleting (safer)
# Resolve any conflicts, then:
git stash drop stash@{0}
```

### Using stash for long-lived work

```
# BAD — keeping unfinished work in stash for days
git stash push -m "big feature WIP"
# ... 4 days pass, diverges from main

# GOOD — branch instead
git switch -c feature/big-feature-wip
git add -p && git commit -m "WIP: big feature in progress"
```

## Decision Guide

| Scenario | Command |
|----------|---------|
| Quick context switch, return soon | `git stash push -m "<description>"` |
| Include untracked files | `git stash push -m "..." --include-untracked` |
| Stash only specific files | `git stash push -m "..." -- <files>` |
| See what's stashed | `git stash list` → `git stash show -p stash@{N}` |
| Apply and keep the stash | `git stash apply stash@{N}` |
| Apply and discard the stash | `git stash pop stash@{N}` |
| Long-term shelved work | `git stash branch <branch-name>` |
| Accidentally dropped a stash | `git fsck --lost-found` to recover |
| Clear all stashes | `git stash clear` (confirm first — irreversible) |

## Execution

1. Parse `$ARGUMENTS` — detect operation: `save <message>`, `list`, `show`, `pop`, `apply`, `drop`, `branch <name>`
2. **save** → `git stash push -m "<message>"` (if no message in args, ask for one); add `--include-untracked` if working tree has new files
3. **list** → `git stash list` with readable output
4. **show** → `git stash show -p stash@{N}` (prompt for index if not given)
5. **apply** → check working tree is clean first, then `git stash apply stash@{N}`
6. **pop** → warn it's destructive, suggest `apply` if unsure; then `git stash pop stash@{N}`
7. **drop** → confirm before `git stash drop stash@{N}`; warn about `git stash clear`
8. **branch `<name>`** → `git stash branch <name> stash@{N}` to convert stash to branch
9. If recovery requested: use `git fsck --lost-found` to find dangling commits

$ARGUMENTS
