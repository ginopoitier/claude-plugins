# Git Kit

> **Config:** @~/.claude/git-kit.config.md — run `/git-setup` if missing.

## Scope
- **VCS:** Git CLI — local repository operations
- **Covers:** Commits · Branches · Rebase · Conflicts · Stash · Undo · Hooks · Repo health

## Always-Active Rules

@~/.claude/rules/git-kit/commit-conventions.md
@~/.claude/rules/git-kit/branch-conventions.md
@~/.claude/rules/git-kit/safety.md
@~/.claude/rules/git-kit/workflow.md

## Meta — Always Apply

@~/.claude/skills/context-discipline/SKILL.md
@~/.claude/skills/model-selection/SKILL.md
@~/.claude/skills/verification-loop/SKILL.md

## Two-Level Config System

Config is split into two levels — **never hardcode values**:

### User / Device Level — `~/.claude/git-kit.config.md`
Personal git identity and preferences. Same across all repos on this machine:
- `GIT_DEFAULT_BRANCH` · `GIT_SIGNING_KEY` · `GIT_MERGE_STRATEGY` · `COMMIT_STYLE`

### Project Level — `.claude/git.config.md` (in each repo)
Repo-specific conventions. Commit to version control:
- `BRANCH_STRATEGY` · `DEFAULT_BRANCH` · `PROTECTED_BRANCHES` · `COMMIT_CONVENTION`

Run `/git-setup` to configure. Project config **overrides** user config where values overlap.

When a skill needs config and `~/.claude/git-kit.config.md` is missing → tell user to run `/git-setup`.
When a skill needs project config and `.claude/git.config.md` is missing → tell user to run `/git-setup --project`.

## Self-Improvement — Auto-Active

@~/.claude/skills/instinct-system/SKILL.md
@~/.claude/skills/self-correction-loop/SKILL.md
@~/.claude/skills/autonomous-loops/SKILL.md
@~/.claude/skills/learning-log/SKILL.md

## Skills Available

### Commit Workflow
- `/commit` — craft atomic commits with conventional messages, stage hunks, amend safely
- `/commit-history` — inspect history with log formats, blame, bisect, and pickaxe search

### Branch Management
- `/branch` — naming conventions, creation from correct base, remote tracking, stale cleanup
- `/rebase` — interactive rebase: squash, fixup, reword, drop, reorder, onto

### Conflict Resolution
- `/conflict-resolve` — step-by-step merge/rebase conflict resolution with strategy selection

### Stash Management
- `/stash` — named stashes, partial stash, inspection, recovery, stash-to-branch

### Safe Undo
- `/undo` — choose the right tool: restore, reset, revert, or reflog recovery

### Repo Health
- `/repo-health` — prune stale branches, find large objects, gc, fsck, integrity check

### Hooks & Automation
- `/hooks-setup` — install pre-commit, commit-msg, pre-push hooks with common patterns

### Setup
- `/git-setup` — configure user-level and project-level git-kit settings

### Knowledge (deep reference)
- `commit-patterns.md` — conventional commits spec, anatomy, anti-pattern messages
- `branch-strategies.md` — github-flow vs gitflow vs trunk-based comparison
- `rebase-patterns.md` — interactive rebase cheatsheet, autosquash, `--onto`
- `undo-reference.md` — decision tree: restore vs reset vs revert vs reflog
- `conflict-resolution.md` — conflict marker anatomy, resolution strategies by type
- `stash-patterns.md` — stash internals, named stash workflow, recovery patterns
- `hooks-reference.md` — hook execution order, installation methods, common patterns
- `repo-health-guide.md` — health indicators, branch hygiene, GC, integrity check

### Meta (auto-active, not user-invoked)
- `instinct-system` — learns project-specific git conventions (branch prefixes, commit style)
- `self-correction-loop` — captures corrections → permanent rules
- `autonomous-loops` — bounded retry loops for git operations
- `learning-log` — captures git discoveries and decisions per session
