# Stash Patterns

## When to Stash vs Branch

| Situation | Use Stash | Use Branch |
|-----------|-----------|------------|
| Quick context switch, back in < 1 hour | ✓ | — |
| Interrupted mid-feature, resuming soon | ✓ | — |
| Work paused for > 1 day | — | ✓ |
| Experiment you might abandon | — | ✓ |
| Need to share the WIP with someone | — | ✓ |
| Multiple unrelated WIP items | — | ✓ (one each) |

## Stash Internals

A stash is a special commit object stored in `refs/stash`. It records:
- The working tree state (as a commit)
- The index/staging state (as a commit)
- Optionally untracked files (with `--include-untracked`)

This means stashed work survives branch switches, rebases, and even git gc (within the reflog window).

## Named Stash Workflow

```bash
# Save with a clear description
git stash push -m "WIP: order validation — missing edge case for zero quantity"

# Later, find it
git stash list
# stash@{0}: On feature/orders: WIP: order validation — missing edge case for zero quantity

# Inspect before applying
git stash show -p stash@{0}

# Apply and keep the stash (safe)
git stash apply stash@{0}

# Once confirmed clean, remove it
git stash drop stash@{0}
```

## Partial Stash

Stash only specific files or hunks — useful when you have mixed changes:

```bash
# Stash only specific files
git stash push -m "only auth changes" -- src/auth/ src/middleware/

# Interactive hunk selection (like git add -p but for stashing)
git stash push -p
```

## Stash Recovery

### From the stash list

```bash
git stash list                         # find the stash
git stash apply stash@{N}              # apply it
```

### Dropped stash recovery

Git stashes are commit objects. Even after `git stash drop`, the commit object remains in the object store until garbage collected (default: 90 days).

```bash
# Find dangling commit objects
git fsck --lost-found | grep "dangling commit"

# Inspect each to find your stash
git show <sha>                         # check the message and diff

# Apply the recovered stash
git stash apply <sha>
```

### Via reflog

```bash
git reflog show refs/stash            # see all stash operations including drops
```

## Common Mistakes

### Stash doesn't include new untracked files

```bash
# New file created but not staged:
touch src/new-feature.ts

git stash push -m "my work"   # new-feature.ts NOT included

# Fix: use --include-untracked
git stash push -m "my work" --include-untracked
```

### Stash pop conflicts

```bash
# If applying stash causes conflicts:
git stash pop   # conflicts flagged in working tree

# Resolve conflicts
git add <resolved>

# The stash was already removed by pop — nothing to drop
# If you used apply instead:
git stash drop stash@{0}   # clean up after manual resolve
```

### stash@{N} indices shift after each stash operation

```bash
# After git stash push, all existing indices increment by 1
# stash@{0} is always the most recent

# Use the message to find specific stashes, not just the index
git stash list | grep "order validation"
```
