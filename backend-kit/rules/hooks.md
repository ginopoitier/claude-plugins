# Rule: Claude Code Hooks

## Overview
Hooks are shell commands configured in `settings.json` that run automatically on lifecycle events. Use `/update-config` to configure them — they run in the harness, not in Claude's context.

## Available Hook Events
- `PreToolUse` — runs before a tool executes (can block with exit code != 0)
- `PostToolUse` — runs after a tool completes
- `Stop` — runs when Claude finishes responding
- `UserPromptSubmit` — runs when user submits a prompt

## DO
- Use hooks for **automated enforcement** that shouldn't require asking Claude each time
- Use `PreToolUse` on `Bash` to block dangerous commands (e.g., `rm -rf`, `DROP TABLE`)
- Use `PostToolUse` on `Write`/`Edit` to auto-format code after edits
- Use `Stop` hooks to run verification (build, lint) after Claude finishes a task
- Keep hook scripts **fast** — slow hooks block the UI
- Log hook output to a file for debugging: `>> ~/.claude/hooks.log 2>&1`
- Use `CLAUDE_TOOL_INPUT` / `CLAUDE_TOOL_OUTPUT` env vars in hook scripts for context

## DON'T
- Don't put business logic in hooks — hooks are for automation, not policy
- Don't run heavy operations (full test suite) in PreToolUse — it blocks Claude mid-task
- Don't add hooks without documenting what they do in a comment
- Don't use hooks to work around missing permissions — configure permissions directly

## Common Hook Patterns

### Auto-format after edits
```json
{
  "hooks": {
    "PostToolUse": [{
      "matcher": "Edit|Write",
      "hooks": [{"type": "command", "command": "dotnet format $CLAUDE_TOOL_INPUT_PATH --include $CLAUDE_TOOL_INPUT_PATH 2>/dev/null || true"}]
    }]
  }
}
```

### Block dangerous bash commands
```json
{
  "hooks": {
    "PreToolUse": [{
      "matcher": "Bash",
      "hooks": [{"type": "command", "command": "~/.claude/hooks/guard-bash.sh"}]
    }]
  }
}
```

### Show build status on stop
```json
{
  "hooks": {
    "Stop": [{"type": "command", "command": "~/.claude/hooks/notify-done.sh"}]
  }
}
```

## Deep Reference
Use `/update-config` to configure hooks — Claude cannot modify `settings.json` directly without explicit permission.
