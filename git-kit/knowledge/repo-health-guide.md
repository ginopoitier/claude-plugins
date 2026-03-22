# Repo Health Guide

## Health Indicators

| Metric | Healthy | Investigate |
|--------|---------|-------------|
| Stale local branches | 0–3 | > 5 |
| Stale remote refs | 0 (pruned regularly) | Any old ones |
| Pack file size | < 100MB for typical app | > 500MB |
| Loose objects | < 100 | > 1000 |
| Last `git gc` | < 1 week | > 1 month |

## Branch Hygiene

### Find and remove stale branches

```bash
# Show local branches with their last commit date
git for-each-ref --sort=committerdate refs/heads/ \
  --format="%(committerdate:short)  %(refname:short)"

# Branches already merged into main
git branch --merged main | grep -v "^\*\|main\|master\|develop"

# Delete all merged local branches (safe — only merged ones)
git branch --merged main \
  | grep -v "^\*\|main\|master\|develop" \
  | xargs -r git branch -d

# Prune remote-tracking refs that no longer exist on the remote
git fetch --prune
git remote prune origin
```

### List remote branches by age

```bash
git for-each-ref --sort=committerdate refs/remotes/ \
  --format="%(committerdate:short)  %(refname:short)" \
  | grep -v "HEAD"
```

## Object Store Health

### Check size breakdown

```bash
git count-objects -vH
# count: loose objects
# size: space used by loose objects
# in-pack: objects in pack files
# packs: number of pack files
# size-pack: space used by pack files
# prune-packable: loose objects that can be pruned
# garbage: objects in garbage files
```

### Find the largest objects

```bash
# Top 10 largest objects by compressed size
git rev-list --objects --all \
  | git cat-file --batch-check='%(objecttype) %(objectname) %(objectsize) %(rest)' \
  | grep '^blob' \
  | sort -k3 -rn \
  | head -10 \
  | awk '{printf "%s KB\t%s\n", int($3/1024), $4}'
```

### Find large files in current working tree

```bash
find . -not -path './.git/*' -size +1M | sort -k5 -rn
```

## Garbage Collection

```bash
# Routine GC (safe, fast — run weekly)
git gc

# Aggressive GC (slower, more thorough — run monthly)
git gc --aggressive

# Prune unreachable objects immediately (default is 2-week grace period)
git gc --prune=now

# Just repack without pruning
git repack -a -d
```

## Integrity Verification

```bash
# Verify all object SHA checksums and connectivity
git fsck

# Also write dangling objects to .git/lost-found/ (useful for recovery)
git fsck --lost-found

# Verify a specific object
git cat-file -e <sha> && echo "object exists"
```

Common fsck output:
- `dangling commit` — commit with no branch pointing to it (stash remnants, reflog)
- `dangling blob` — file content with no tree pointing to it
- `missing blob` — corruption warning — serious if not from a partial clone

## Working Tree Cleanup

```bash
# Preview untracked files that would be removed
git clean -nd

# Remove untracked files and directories
git clean -fd

# Also remove gitignored files (build output, caches)
git clean -fdx

# Remove only gitignored files (keep untracked source files)
git clean -fdX
```

## Routine Maintenance Checklist

Run monthly or before archiving a repo:

```bash
git fetch --prune                                    # remove stale remote refs
git branch --merged main | grep -v main | xargs git branch -d  # delete merged branches
git gc --aggressive --prune=now                      # deep cleanup
git fsck                                             # verify integrity
git count-objects -vH                                # confirm size reduced
```
