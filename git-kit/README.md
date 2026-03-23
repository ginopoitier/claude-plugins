# Git Kit

Git CLI toolkit for Claude Code — smarter commits, branching, rebase, conflict resolution, stash, undo, repo health, and hooks.

## What's Included

### Skills (10 user-invokable + 7 auto-active = 17 total)

| Category | Skills |
|----------|--------|
| **Commit Workflow** | `/commit`, `/commit-history` |
| **Branch Management** | `/branch`, `/rebase` |
| **Conflict Resolution** | `/conflict-resolve` |
| **Stash Management** | `/stash` |
| **Safe Undo** | `/undo` |
| **Repo Health** | `/repo-health` |
| **Hooks & Automation** | `/hooks-setup` |
| **Setup** | `/git-setup` |
| **Meta (auto-active)** | `instinct-system`, `self-correction-loop`, `autonomous-loops`, `learning-log`, `context-discipline`, `model-selection`, `verification-loop` |

### Agents (2)

| Agent | Role |
|-------|------|
| `git-historian` | Inspect, search, and understand commit history — log formats, blame, bisect, pickaxe |
| `git-surgeon` | Precise history rewriting — rebase, cherry-pick, squash, fixup, onto operations |

### Rules (always-active)

| Rule | Enforces |
|------|---------|
| `commit-conventions` | Conventional commit format, atomic commits, message structure |
| `branch-conventions` | Branch naming patterns, base branch selection |
| `safety` | Destructive command guards, force-push warnings, confirmation requirements |
| `workflow` | Branch lifecycle, PR hygiene, stale branch cleanup |

### Knowledge (deep reference)

- `commit-patterns.md` — conventional commits spec, anatomy, anti-pattern messages
- `branch-strategies.md` — github-flow vs gitflow vs trunk-based comparison
- `rebase-patterns.md` — interactive rebase cheatsheet, autosquash, `--onto`
- `undo-reference.md` — decision tree: restore vs reset vs revert vs reflog
- `conflict-resolution.md` — conflict marker anatomy, resolution strategies by type
- `stash-patterns.md` — stash internals, named stash workflow, recovery patterns
- `hooks-reference.md` — hook execution order, installation methods, common patterns
- `repo-health-guide.md` — health indicators, branch hygiene, GC, integrity check

### Hooks (installable)

- `validate-commit-msg.sh` — enforces conventional commit format on every commit
- `pre-push-guard.sh` — blocks force pushes to protected branches

## Install

### Via Claude Code plugin system (recommended)

```
/plugin marketplace add ginopoitier/claude-plugins
/plugin install git-kit@ginopoitier-plugins
```

### Direct install (local development)

```bash
git clone https://github.com/ginopoitier/claude-plugins.git
/plugin install ./claude-plugins/git-kit
```

## First-Time Setup

### 1. Run git-setup

```
/git-setup
```

Configures `~/.claude/git-kit.config.md` with your default branch, commit style, and signing key.

### 2. Configure a project (optional)

Inside any repo:

```
/git-setup --project
```

Creates `.claude/git.config.md` with repo-specific branch strategy, protected branches, and commit convention.

### 3. Install git hooks (optional)

```
/hooks-setup
```

Installs `commit-msg` validator and `pre-push` guard into the current repo's `.git/hooks/`.

## Two-Level Config System

| Level | File | Contains |
|-------|------|----------|
| User / Device | `~/.claude/git-kit.config.md` | Default branch, signing key, merge strategy, commit style |
| Project | `.claude/git.config.md` | Branch strategy, protected branches, commit convention |

Run `/git-setup` once per machine. Run `/git-setup --project` once per repo.

## Requirements

- Claude Code 1.0.0+
- Git 2.23+

## License

MIT
