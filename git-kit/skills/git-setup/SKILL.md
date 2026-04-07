---
name: git-setup
description: >
  Configure git-kit user-level and project-level settings. Sets up git identity,
  commit style, branch strategy, and protected branches. Load this skill when:
  "git setup", "configure git", "git-setup", "setup git kit",
  "git config", "configure commit style", "set up git conventions",
  "initialize git config", "git kit config", "setup branch strategy".
user-invocable: true
argument-hint: "[--user | --project | --both]"
allowed-tools: Bash, Read, Write
---

# Git Setup

## Core Principles

1. **Two levels of config** — user level is per-machine (identity, preferences); project level is per-repo (strategy, conventions). Both are needed.
2. **Project config is committed** — `.claude/git.config.md` belongs in the repo so all developers on the project share the same conventions.
3. **User config is personal** — `~/.claude/git-kit.config.md` stays on the machine. Never commit it.
4. **Defaults are safe** — reasonable defaults are provided. Only override what you need to change.
5. **Skills read config automatically** — once set, all git-kit skills read these values without prompting.

## Patterns

### User-level setup (`--user`)

Walk through user-level settings:

```bash
# 1. Confirm git identity
git config --global user.name
git config --global user.email

# 2. Set default branch
git config --global init.defaultBranch main

# 3. Configure commit style
# Write to ~/.claude/git-kit.config.md:
GIT_DEFAULT_BRANCH=main
GIT_SIGNING_KEY=          # leave blank to disable
GIT_MERGE_STRATEGY=merge  # merge | rebase | squash
COMMIT_STYLE=freeform     # freeform | conventional
```

### Project-level setup (`--project`)

Run from the root of the repo:

```bash
# Detect current settings from repo
git remote get-url origin
git symbolic-ref refs/remotes/origin/HEAD | sed 's@^refs/remotes/origin/@@'

# Create .claude/ directory if needed
mkdir -p .claude

# Write .claude/git.config.md with detected + prompted values:
BRANCH_STRATEGY=github-flow    # gitflow | trunk | github-flow
DEFAULT_BRANCH=main
PROTECTED_BRANCHES=main,master
COMMIT_CONVENTION=             # overrides user COMMIT_STYLE if set
GIT_REMOTE=origin

# Add .claude/git.config.md to version control
git add .claude/git.config.md
git commit -m "chore: add git-kit project config"
```

### Git global settings (applied during user setup)

```bash
# Pull strategy
git config --global pull.rebase false        # or true for rebase-based teams

# Better diff output
git config --global diff.colorMoved zebra

# Push only current branch
git config --global push.default current

# Auto-setup remote tracking
git config --global push.autoSetupRemote true

# Better merge conflict markers (show base too)
git config --global merge.conflictstyle diff3

# Trim whitespace in patches
git config --global apply.whitespace fix
```

## Anti-patterns

### Hardcoding config values in skills

```
# BAD — skill assumes defaults
DEFAULT_BRANCH=main   # hardcoded in skill logic

# GOOD — skill reads from config
DEFAULT_BRANCH=$(grep "^DEFAULT_BRANCH=" .claude/git.config.md | cut -d= -f2-)
```

### Committing user-level config

```
# BAD — committing personal settings (exposes signing key, paths)
git add ~/.claude/git-kit.config.md   # wrong directory anyway
git add git-kit.config.md             # never commit user config

# GOOD — only commit project config
git add .claude/git.config.md
```

## Decision Guide

| Scenario | Command |
|----------|---------|
| First time on a new machine | `/git-setup --user` |
| Starting a new project | `/git-setup --project` |
| Joining an existing project | `/git-setup --project` (reads existing config) |
| Changing commit style for a project | Edit `.claude/git.config.md` → `COMMIT_CONVENTION=conventional` |
| Changing personal preferences | Edit `~/.claude/git-kit.config.md` |
| Config missing, skill fails | Run `/git-setup` — it detects which level is missing |

## Execution

1. Parse `$ARGUMENTS` — detect scope: `--user`, `--project`, `--both` (default: both)
2. **--user**: check `~/.claude/git-kit.config.md` — create or update with identity + preferences
3. **--project**: check `.claude/git.config.md` in cwd — detect from repo, prompt for values, write file
4. For user setup: confirm `git config --global user.name` and `user.email` are set
5. For project setup: detect `BRANCH_STRATEGY` from existing branches (main/develop/release pattern)
6. Apply recommended global git settings (pull.rebase, push.default, merge.conflictstyle)
7. If project config is new: `git add .claude/git.config.md` and suggest committing it
8. Confirm setup is complete by echoing final config values

$ARGUMENTS
