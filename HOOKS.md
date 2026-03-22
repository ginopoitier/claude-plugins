# Hook Registration

Claude Code hooks run shell scripts automatically on lifecycle events. Since `install.sh` is no longer used, register hooks manually after installing a plugin via `/plugin install`.

## dev-kit Hooks

### Claude Code Hooks (settings.json)

Add to `~/.claude/settings.json`:

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Bash",
        "hooks": [
          {
            "type": "command",
            "command": "~/.claude/hooks/dev-kit/pre-bash-guard.sh"
          }
        ]
      }
    ],
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "~/.claude/hooks/dev-kit/post-edit-format.sh"
          },
          {
            "type": "command",
            "command": "~/.claude/hooks/dev-kit/post-scaffold-restore.sh"
          }
        ]
      }
    ]
  }
}
```

Or use `jq` to merge non-destructively (won't clobber existing hooks):

```bash
settings=~/.claude/settings.json
[[ ! -f "$settings" ]] && echo '{}' > "$settings"
tmp=$(mktemp)
jq '
  .hooks.PreToolUse = ((.hooks.PreToolUse // []) + [{
    "matcher": "Bash",
    "hooks": [{"type": "command", "command": "~/.claude/hooks/dev-kit/pre-bash-guard.sh"}]
  }] | unique_by(.hooks[0].command)) |
  .hooks.PostToolUse = ((.hooks.PostToolUse // []) + [{
    "matcher": "Edit|Write",
    "hooks": [
      {"type": "command", "command": "~/.claude/hooks/dev-kit/post-edit-format.sh"},
      {"type": "command", "command": "~/.claude/hooks/dev-kit/post-scaffold-restore.sh"}
    ]
  }] | unique_by(.hooks[0].command))
' "$settings" > "$tmp" && mv "$tmp" "$settings"
```

### Hook Reference — dev-kit

| File | Event | Matcher | Purpose |
|------|-------|---------|---------|
| `pre-bash-guard.sh` | `PreToolUse` | `Bash` | Blocks destructive operations: force push, `git reset --hard`, `git clean -f`, `rm -rf` on non-safe targets. Warns on `dotnet run`. |
| `post-edit-format.sh` | `PostToolUse` | `Edit\|Write` | Auto-runs `dotnet format` on any `.cs` file Claude edits. Finds the nearest `.csproj` or `.sln` to scope the format. |
| `post-scaffold-restore.sh` | `PostToolUse` | `Edit\|Write` | Runs `dotnet restore` when a `.csproj` file is created or modified. |

### Git Hooks (optional, project-level)

These are standalone scripts, not Claude Code hooks. Copy them into a project's `.git/hooks/` directory or wire via Husky/lefthook:

| File | Git Event | Purpose |
|------|-----------|---------|
| `pre-commit-antipattern.sh` | `pre-commit` | Scans staged `.cs` files for: `DateTime.Now`, `new HttpClient()`, `async void`, sync-over-async (`.Result`, `.GetAwaiter().GetResult()`). Blocks commit on findings. |
| `pre-commit-format.sh` | `pre-commit` | Runs `dotnet format --verify-no-changes`. Blocks commit if files need formatting. |
| `pre-build-validate.sh` | manual / CI | Validates project structure: solution file present, `Directory.Build.props` for multi-project solutions, `global.json`, `.editorconfig`, test projects present. |

Install git hooks into a project:

```bash
# from your project root
cp ~/.claude/hooks/dev-kit/pre-commit-antipattern.sh .git/hooks/pre-commit
cp ~/.claude/hooks/dev-kit/pre-commit-format.sh .git/hooks/pre-commit-format
chmod +x .git/hooks/pre-commit .git/hooks/pre-commit-format
```

---

## kit-maker Hooks

Add to `~/.claude/settings.json`:

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Write|Edit",
        "hooks": [
          {
            "type": "command",
            "command": "~/.claude/hooks/kit-maker/validate-skill-frontmatter.sh"
          }
        ]
      }
    ],
    "Stop": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "~/.claude/hooks/kit-maker/auto-sync-skills-index.sh"
          }
        ]
      }
    ]
  }
}
```

Or use `jq`:

```bash
settings=~/.claude/settings.json
[[ ! -f "$settings" ]] && echo '{}' > "$settings"
tmp=$(mktemp)
jq '
  .hooks.PostToolUse = ((.hooks.PostToolUse // []) + [{
    "matcher": "Write|Edit",
    "hooks": [{"type": "command", "command": "~/.claude/hooks/kit-maker/validate-skill-frontmatter.sh"}]
  }] | unique_by(.hooks[0].command)) |
  .hooks.Stop = ((.hooks.Stop // []) + [{
    "hooks": [{"type": "command", "command": "~/.claude/hooks/kit-maker/auto-sync-skills-index.sh"}]
  }] | unique_by(.hooks[0].command))
' "$settings" > "$tmp" && mv "$tmp" "$settings"
```

### Hook Reference — kit-maker

| File | Event | Matcher | Purpose |
|------|-------|---------|---------|
| `validate-skill-frontmatter.sh` | `PostToolUse` | `Write\|Edit` | After every SKILL.md write, validates: frontmatter present, required fields (`name`, `description`, `user-invocable`, `allowed-tools`), `argument-hint` when user-invocable, all 4 body sections present, 5+ trigger keywords. Warns (does not block). |
| `auto-sync-skills-index.sh` | `Stop` | — | After Claude finishes responding, checks if any skill directories exist that aren't mentioned in `CLAUDE.md`. Prints a reminder if any are missing from the index. |

---

## Verify Registration

After editing `settings.json`, restart Claude Code. Hooks are active if you see output from them during tool use.

To check current hook registration:

```bash
cat ~/.claude/settings.json | jq '.hooks'
```
