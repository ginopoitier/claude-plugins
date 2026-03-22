# Rebase Patterns

## Interactive Rebase Cheatsheet

Open the rebase editor with: `git rebase -i HEAD~N`

### Actions

| Action | Shorthand | Effect |
|--------|-----------|--------|
| `pick` | `p` | Keep commit as-is |
| `reword` | `r` | Keep commit, edit message |
| `edit` | `e` | Pause to amend the commit |
| `squash` | `s` | Merge into previous, combine messages |
| `fixup` | `f` | Merge into previous, discard message |
| `drop` | `d` | Delete the commit entirely |
| `exec` | `x` | Run a shell command after the commit |

### Common Transformations

**Before cleanup:**
```
pick a1b Add user model
pick b2c Fix typo in user model
pick c3d Add email validation
pick d4e WIP email validation
pick e5f Add password validation
pick f6g temp debug logging
```

**After cleanup:**
```
pick a1b Add user model
fixup b2c Fix typo in user model
pick c3d Add email validation
fixup d4e WIP email validation
pick e5f Add password validation
drop f6g temp debug logging
```

Result: 4 clean commits instead of 6 noisy ones.

## Autosquash Workflow

Best for ongoing work where you accumulate fixup commits as you go:

```bash
# While working on a feature, create targeted fixups:
git commit --fixup=a1b2c3d        # targets specific commit

# At the end, autosquash sorts and merges everything:
git rebase -i --autosquash HEAD~N
# fixup commits appear automatically below their targets
```

## Rebase vs Merge

| Scenario | Use Rebase | Use Merge |
|----------|-----------|-----------|
| Updating feature branch with main | ✓ (linear history) | — |
| Integrating feature into main | — | ✓ (preserves context) |
| History already pushed/shared | Never | ✓ |
| Solo feature branch cleanup | ✓ | — |
| Team agrees on rebase workflow | ✓ | — |

## Rebase Conflict Resolution Loop

```bash
git rebase origin/main

# Conflict flagged:
git status                     # see conflicted files
# Edit each file — resolve conflict markers
git add <resolved-file>        # mark resolved
git rebase --continue          # move to next commit

# If stuck:
git rebase --abort             # return to pre-rebase state
```

## The `--onto` Flag

Moves a range of commits to a new base:

```bash
# Scenario: feature/b was branched from feature/a, but should be on main
git rebase --onto main feature/a feature/b
#                  ↑ new base  ↑ old base  ↑ branch to move
```

Before:
```
main → A → B → C
               └── feature/a → D → E
                                    └── feature/b → F → G
```

After `git rebase --onto main feature/a feature/b`:
```
main → A → B → C → F' → G'
               └── feature/a → D → E
```
