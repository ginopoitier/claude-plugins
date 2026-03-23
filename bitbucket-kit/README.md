# bitbucket-kit

Bitbucket platform toolkit for Claude Code — PR creation, review, and Bitbucket-specific workflows. Pairs with git-kit for local repository operations.

## What's Included

| Category | Skills |
|----------|--------|
| Pull Requests | `/pr` — create, view, list, and diff PRs using the Bitbucket REST API |
| Setup | `/bitbucket-setup` — configure user-level and project-level settings |

## Install

```bash
bash install.sh
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
- git-kit recommended for local repository operations
