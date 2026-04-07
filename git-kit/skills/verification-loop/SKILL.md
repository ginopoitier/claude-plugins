---
name: verification-loop
description: >
  Pre-commit and pre-push verification checklist Claude runs before marking
  any git operation complete — staged content, commit message, secrets scan,
  branch safety, and diff review. Load this skill when: "verify", "before committing",
  "pre-commit check", "ready to push", "check before push", "verify changes",
  "pre-push verification", "quality gate", "review before commit".
user-invocable: false
allowed-tools: Bash, Grep, Read
---

# Verification Loop

## Core Principles

1. **Run before every commit hand-off** — never mark a git task complete without clearing at least Phase 1 and Phase 2.
2. **Staged content is what matters** — verify what's actually staged, not the working tree.
3. **Secrets in diffs are permanent** — a committed secret stays in history even after deletion. Check before every push.
4. **Branch safety before force operations** — confirm the branch and remote before any destructive push.
5. **Short-circuit on critical failures** — Phase 1 and Phase 2 failures stop the loop. Others are warnings.

## Patterns

### The 4-Phase Git Verification Pipeline

| # | Phase | Command | Critical? |
|---|-------|---------|-----------|
| 1 | Staged content review | `git diff --staged` | Yes — must match intent |
| 2 | Commit message quality | Check subject length, format, mood | Yes — reject vague messages |
| 3 | Secrets scan | Grep diff for credentials | Yes — halt if found |
| 4 | Branch safety | Confirm branch, remote, protection status | Yes before force ops |

### Phase 1 — Staged Content Review

```bash
git diff --staged --stat          # files and change volume
git diff --staged                 # full diff — confirm it matches intent
git diff --check                  # flag conflict markers and whitespace errors
```

Check:
- Only intended files are staged
- No unrelated changes snuck in via `git add .`
- No conflict markers (`<<<<<<<`, `=======`, `>>>>>>>`) remain
- No debug artifacts (console.log, print(), debugger, TODO left in)

### Phase 2 — Commit Message Quality

```bash
git log --oneline -1              # review the most recent message (if amending)
```

Check:
- Subject line ≤ 72 characters
- Imperative mood ("Add X", not "Added X")
- Not vague ("fix", "WIP", "update stuff")
- Conventional format if `COMMIT_CONVENTION=conventional` in project config

### Phase 3 — Secrets Scan

```bash
# Scan staged diff for common secret patterns
git diff --staged | grep -iE "(password|secret|api_key|apikey|token|private_key)\s*=\s*['\"]?[^\s'\"]{8,}"
git diff --staged | grep -iE "-----BEGIN (RSA|EC|OPENSSH) PRIVATE KEY"
git diff --staged | grep -iE "(aws_access_key|AKIA[A-Z0-9]{16})"
```

If any match found → **halt, do not commit, remove the secret**.

### Phase 4 — Branch Safety

```bash
git branch --show-current         # confirm you're on the right branch
git status                        # confirm clean state after staging
git log origin/<branch>..HEAD --oneline  # confirm what's local-only
```

Before force operations additionally check:
```bash
# Confirm remote hasn't changed since last fetch
git fetch --dry-run
```

## Anti-patterns

### Skipping the staged diff review

```
# BAD — committing without reviewing what's staged
git add . && git commit -m "feat: add login"
# Accidentally included .env file or debug statements

# GOOD — always review the staged diff
git diff --staged
git commit -m "feat(auth): add email login form"
```

### Committing without a secrets check before first push

```
# BAD — pushing a new repo without scanning
git push origin main
# AWS key was in config.ts, now in GitHub history forever

# GOOD — scan before first push to a new remote
git log --all -p | grep -iE "api_key|secret|password"
git push origin main
```

## Decision Guide

| Scenario | Phases to Run |
|----------|--------------|
| Routine local commit | Phase 1 + 2 |
| Commit before pushing | Phase 1 + 2 + 3 |
| Force push | All 4 phases |
| First push to new remote | Phase 3 (full history scan) + Phase 4 |
| Amending a commit | Phase 1 + 2 |
| Merging/rebasing complete | Phase 1 + 4 |

## Execution

1. Determine which phases to run based on operation context (see Decision Guide)
2. **Phase 1** — run `git diff --staged --stat` then `git diff --staged`; flag unintended files, debug artifacts, conflict markers
3. **Phase 2** — check subject line length (≤72), mood (imperative), specificity (not "fix" / "WIP")
4. **Phase 3** — scan staged diff with grep patterns for secrets; halt and report if any match
5. **Phase 4** — confirm current branch with `git branch --show-current`; verify remote hasn't changed with `git fetch --dry-run`
6. If any Phase 1–3 check fails: **stop**, report the issue, do not proceed
7. If all phases pass: report "Verification passed" and proceed with the git operation

$ARGUMENTS
