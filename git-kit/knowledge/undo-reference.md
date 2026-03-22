# Undo Reference

## Quick Decision Tree

```
What do you want to undo?
│
├── Uncommitted changes in working tree
│   └── git restore <file>          # specific file
│   └── git restore .               # everything (irreversible)
│
├── Staged changes (not yet committed)
│   └── git restore --staged <file> # unstage (keep changes)
│
├── Local commits (NOT pushed)
│   ├── Keep the changes
│   │   └── git reset --soft HEAD~N
│   ├── Discard the changes
│   │   └── git reset --hard HEAD~N
│   └── Undo a specific commit (not the last)
│       └── git revert <sha>        # creates inverse commit
│
├── Pushed commits (shared with others)
│   └── git revert <sha>            # ALWAYS — never reset pushed commits
│   └── git revert -m 1 <merge-sha> # for merge commits
│
└── Lost something (reflog recovery)
    └── git reflog                  # find the sha
    └── git switch -c recovery <sha>
```

## Command Reference

| Command | Scope | Destroys Work | Safe for Pushed |
|---------|-------|---------------|-----------------|
| `git restore <file>` | Working tree | Yes (working changes) | n/a |
| `git restore --staged <file>` | Staging area | No | n/a |
| `git reset --soft HEAD~N` | Commits | No (moves to staged) | No |
| `git reset HEAD~N` | Commits | No (moves to unstaged) | No |
| `git reset --hard HEAD~N` | Commits + working tree | Yes | No |
| `git revert <sha>` | Creates new commit | No | Yes |
| `git revert -m 1 <sha>` | Merge commit revert | No | Yes |

## Reflog — The Ultimate Safety Net

Git records every movement of HEAD in the reflog. It's kept for 90 days by default.

```bash
git reflog                          # see all HEAD movements
git reflog show feature/my-branch   # reflog for a specific branch
```

Example reflog output:
```
a1b2c3d HEAD@{0}: commit: add user validation
e4f5g6h HEAD@{1}: reset: moving to HEAD~1      ← the mistake
i7j8k9l HEAD@{2}: commit: add login page        ← the lost commit
m0n1o2p HEAD@{3}: checkout: moving from main
```

To recover `i7j8k9l`:
```bash
git switch -c recovery/lost-work i7j8k9l
# or
git cherry-pick i7j8k9l
```

## ORIG_HEAD

Git saves the previous HEAD before merges and rebases as `ORIG_HEAD`:

```bash
# Undo a merge immediately after it happened
git reset --hard ORIG_HEAD

# Undo a rebase immediately after it finished
git reset --hard ORIG_HEAD
```

This only works once — `ORIG_HEAD` is overwritten by the next merge/rebase.
