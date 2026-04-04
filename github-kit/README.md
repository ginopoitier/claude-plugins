# github-kit

GitHub platform toolkit for Claude Code — PR creation, management, releases, and GitHub-specific workflows. Local repository operations are outside this kit's scope.

## What's Included

| Category | Skills |
|----------|--------|
| Pull Requests | `/pr` — create, view, list, and diff PRs using the `gh` CLI |
| Releases | `/release` — tag, create GitHub release, auto-generate release notes from commits |
| Setup | `/github-setup` — configure user-level and project-level settings |

## Install

```
/plugin marketplace add ginopoitier/claude-plugins
/plugin install github-kit@ginopoitier-plugins
```

Then run `/github-setup` in Claude Code.

## Configuration

| Level | File | Contains |
|-------|------|----------|
| User / Device | `~/.claude/github-kit.config.md` | GitHub org, base URL, PR draft default |
| Project | `.claude/github.config.md` | Repo name, default branch |

## Requirements

- Claude Code 1.0.0+
- `gh` CLI installed and authenticated (`gh auth login`)
- Local repository operations are outside this kit's scope

## License

MIT
