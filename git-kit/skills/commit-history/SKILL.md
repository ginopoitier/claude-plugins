---
name: commit-history
description: >
  Inspect, search, and understand git commit history using log formats, blame,
  bisect, and content search. Load this skill when: "git log", "commit history",
  "who changed this", "git blame", "find commit", "when was this added",
  "search commits", "git bisect", "pickaxe search", "which commit broke",
  "annotate file", "history of a file".
user-invocable: true
argument-hint: "[log | blame <file> | bisect | search <term>]"
allowed-tools: Bash, Read, Grep
---

# Commit History

## Core Principles

1. **Format output for the task** — raw `git log` is noisy. Use `--oneline`, `--graph`, or custom formats for the job at hand.
2. **Blame shows who, log shows why** — `git blame` identifies the last person to touch each line; the commit message explains the reason.
3. **Bisect systematically** — binary search cuts search space in half each step. Don't guess when bisect can find it in O(log n).
4. **Pickaxe finds content changes** — `-S` finds commits that added/removed a string; `-G` matches by regex across the diff.
5. **Follow renames** — use `--follow` when inspecting file history to trace through renames and moves.

## Patterns

### Log formats

```bash
# One-line overview
git log --oneline

# With branch graph
git log --oneline --graph --all

# Full detail with diff stats
git log --stat

# Custom format: hash | author | date | subject
git log --pretty=format:"%h | %an | %ar | %s"

# Filter by author
git log --author="Name"

# Filter by date range
git log --after="2024-01-01" --before="2024-06-01"

# Filter by file
git log -- path/to/file.ts

# Follow renames
git log --follow -- path/to/renamed-file.ts

# Search by commit message keyword
git log --grep="login" --oneline
```

### Blame

```bash
# Annotate every line with last commit and author
git blame path/to/file.ts

# Show only line range (lines 40-60)
git blame -L 40,60 path/to/file.ts

# Ignore whitespace changes
git blame -w path/to/file.ts

# Find the commit that actually introduced the logic (follow moves)
git blame -C -C path/to/file.ts
```

### Pickaxe — search by content

```bash
# Find commits that added or removed the string "UserService"
git log -S "UserService" --oneline

# Find commits where the diff matches a regex
git log -G "function\s+createUser" --oneline

# Show the diff of those commits
git log -S "UserService" -p
```

### Bisect — find the breaking commit

```bash
git bisect start
git bisect bad                    # current commit is broken
git bisect good <known-good-sha>  # last known good state

# Git checks out the midpoint. Test it, then:
git bisect good   # if this commit is fine
git bisect bad    # if this commit is broken

# Repeat until git identifies the first bad commit
git bisect reset  # return to HEAD when done

# Automate with a test script
git bisect run ./scripts/test-regression.sh
```

### Diff between commits/branches

```bash
git diff main..feature/my-branch    # changes in feature not in main
git diff HEAD~3                     # changes since 3 commits ago
git diff <sha1>..<sha2>             # between two specific commits
git diff --stat HEAD~5              # stats only (files changed, lines)
```

## Anti-patterns

### Using raw git log without formatting

```
# BAD — wall of unreadable output
git log

# GOOD — formatted for the task
git log --oneline --graph --all
git log --stat -- path/to/file.ts
```

### Guessing the breaking commit instead of bisecting

```
# BAD
"Let me check the last 10 commits manually..."
*reads through 10 commits, still unsure*

# GOOD
git bisect start
git bisect bad        # now broken
git bisect good v1.4  # was working at this tag
# 6 bisect steps finds the culprit among 64 commits
```

### Blaming without reading the full commit

```
# BAD — stopping at who touched the line
git blame auth.ts → "Line 42 last changed by Alice in commit abc123"
→ "Alice broke it"

# GOOD — read the commit message and diff for context
git show abc123
# The commit message may reveal the change was intentional or part of a larger fix
```

## Decision Guide

| Scenario | Command |
|----------|---------|
| Overview of recent changes | `git log --oneline --graph` |
| Who last touched a specific line | `git blame -L <from>,<to> <file>` |
| History of one file including renames | `git log --follow -- <file>` |
| Find when a string was introduced | `git log -S "<string>" -p` |
| Find the commit that broke something | `git bisect start / bad / good` |
| Changes between two branches | `git diff main..<feature-branch>` |
| Search commit messages by keyword | `git log --grep="<keyword>"` |
| Changes by a specific author | `git log --author="<name>"` |
