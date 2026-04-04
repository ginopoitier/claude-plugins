# bitbucket-kit

Bitbucket platform toolkit for Claude Code — PR creation, review, and Bitbucket-specific workflows. Local repository operations are outside this kit's scope.

## What's Included

| Category | Skills |
|----------|--------|
| Pull Requests | `/pr` — create, view, list, and diff PRs using the Bitbucket REST API |
| Pull Requests | `/review` — tech lead code review with `--mentoring` (coaching) or `--gatekeeper` (strict) modes |
| Releases | `/release` — create a Bitbucket tag and auto-generate release notes from merged PRs |
| Setup | `/bitbucket-setup` — configure user-level and project-level settings |

## Install

```
/plugin marketplace add ginopoitier/claude-plugins
/plugin install bitbucket-kit@ginopoitier-plugins
```

Then run `/bitbucket-setup` in Claude Code.

## Authentication

Bitbucket uses a Personal Access Token stored as an environment variable — never in config files.

```bash
# Windows (CMD — persists across sessions)
setx BITBUCKET_API_TOKEN "your-token-here"

# bash/zsh (add to ~/.bashrc or ~/.zshrc)
export BITBUCKET_API_TOKEN="your-token-here"
```

Get your token: Bitbucket → Personal settings → Personal access tokens

## Configuration

| Level | File | Contains |
|-------|------|----------|
| User / Device | `~/.claude/bitbucket-kit.config.md` | Workspace, base URL, PR defaults |
| Project | `.claude/bitbucket.config.md` | Repo name, default branch |

## Requirements

- Claude Code 1.0.0+
- Bitbucket Personal Access Token set as `BITBUCKET_API_TOKEN`
- Local repository operations are outside this kit's scope
