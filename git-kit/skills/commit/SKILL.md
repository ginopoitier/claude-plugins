---
name: commit
description: >
  Craft atomic git commits with clear, conventional messages. Covers staging
  strategy, conventional commit format, subject line rules, amending, and
  fixup commits. Load this skill when: "commit", "git commit",
  "write a commit message", "stage changes", "amend commit",
  "conventional commits", "commit message", "stage hunks", "git add -p",
  "fixup commit", "squash commit".
user-invocable: true
argument-hint: "[message | --amend | --fixup <sha> | --conventional]"
allowed-tools: Bash, Read, Grep
---

# Commit

## Core Principles

1. **Atomic commits** — one logical change per commit. If you can't describe it in one sentence without "and", it should be split.
2. **Imperative mood** — "Add login page" not "Added login page". Reads as "If applied, this commit will: Add login page."
3. **Subject under 72 chars** — longer subjects are truncated in `git log --oneline` and GitHub UI.
4. **Why, not what** — the diff shows what changed; the message body explains *why*.
5. **Conventional format when configured** — check `COMMIT_CONVENTION` in `.claude/git.config.md` before writing.

## Patterns

### Read the current state first

```bash
git status                    # what's staged, unstaged, untracked
git diff                      # unstaged changes (working tree)
git diff --staged             # staged changes (what will be committed)
```

### Stage precisely

```bash
git add -p                    # interactive hunk staging — pick exactly what belongs
git add <file>                # stage a specific file
git add -u                    # stage all tracked changes (skips untracked)
git restore --staged <file>   # unstage a file without discarding changes
```

### Commit message format

**Free-form (COMMIT_STYLE=freeform):**
```
<imperative subject, max 72 chars>

<optional body — explain WHY, wrap at 72 chars>

<optional footer — Fixes #123, Co-authored-by: ...>
```

**Conventional (COMMIT_STYLE=conventional):**
```
<type>(<scope>): <subject>

<body>

<footer>
```
Types: `feat` `fix` `docs` `style` `refactor` `test` `chore` `perf` `ci`

### Amend safely (unpushed commits only)

```bash
git commit --amend                  # amend message and/or staged content
git commit --amend -m "New message" # amend message only
git add forgotten.ts && git commit --amend --no-edit  # add forgotten file
```

### Fixup for later squash

```bash
git commit --fixup=<sha>            # creates fixup! <original message>
git rebase -i --autosquash HEAD~N   # autosquashes fixups into their targets
```

## Anti-patterns

### Co-author trailers

Never append `Co-Authored-By:` or `Co-authored-by:` lines to commit messages. Commits belong to the developer — do not add AI attribution footers.

### Vague messages

```
# BAD
git commit -m "fix"
git commit -m "WIP"
git commit -m "update stuff"

# GOOD
git commit -m "fix(cart): prevent negative quantity on item removal"
git commit -m "refactor(auth): extract token validation into middleware"
```

### Kitchen-sink commits

```
# BAD — multiple unrelated changes in one commit
git add .
git commit -m "add login, fix cart bug, update README, upgrade deps"

# GOOD — stage and commit each logical unit
git add -p   # select only login-related hunks
git commit -m "feat(auth): add email/password login form"
# repeat for each logical group
```

### Amending pushed commits

```
# BAD — amending a commit others may have pulled
git commit --amend
git push --force

# GOOD — create a new commit or revert
git revert <sha>   # safe: creates an undo commit, preserves history
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Single logical change ready | `git add -p` → `git commit -m "..."` |
| Forgot to stage a file | `git add <file>` → `git commit --amend --no-edit` |
| Wrong message, not pushed | `git commit --amend -m "better message"` |
| Wrong message, already pushed | Create new explanatory commit or leave it |
| Multiple unrelated changes staged | `git restore --staged <file>` → commit in batches |
| Need conventional format | Check `COMMIT_CONVENTION` in `.claude/git.config.md` |
| Long explanation needed | `git commit` (no -m, opens editor for multi-line) |
| Targeting prior commit for cleanup | `git commit --fixup=<sha>` |
