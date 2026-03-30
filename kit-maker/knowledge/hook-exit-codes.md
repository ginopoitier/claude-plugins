# Hook Exit Codes & Hook Types

Complete reference for Claude Code hook behavior: exit codes, hook types, lifecycle events, and structured output format.

## Exit Code Contract

| Code | Meaning | Claude Behavior |
|------|---------|-----------------|
| `0` | Success | Proceed normally; stdout added to Claude's context |
| `2` | Blocking error | Action paused; stderr fed to Claude as context |
| other | Non-blocking error | Action proceeds; stderr logged in verbose mode |

**Rule:** Default to exit 0. Only exit 2 when Claude **must** act on the message. An advisory hook that blocks is worse than no hook.

```bash
# Advisory — always exit 0
if [[ ! -f "$CONFIG_FILE" ]]; then
  echo "NOTICE: Config missing. Run /kit-setup." >&2
fi
exit 0

# Blocking — exit 2 only when Claude must fix it
if grep -q "TODO:" PLAN.md; then
  echo "PLAN.md has unresolved TODOs. Resolve before proceeding." >&2
  exit 2
fi
exit 0
```

Always log to **stderr** (`>&2`). Stdout is consumed by Claude Code as context injection.

## Hook Types

Four types, each with different power:

### 1. `command` — Shell script (most common)

```json
{
  "type": "command",
  "command": "bash ${CLAUDE_PLUGIN_ROOT}/hooks/validate.sh",
  "timeout": 30000
}
```

Input: JSON event payload on stdin. Output: exit code + stderr message.

### 2. `prompt` — LLM evaluation

Runs a single-turn Claude call to validate a condition. Use when shell logic isn't sufficient.

```json
{
  "type": "prompt",
  "prompt": "Check if all tasks in the task list are complete. If not, respond {\"ok\": false, \"reason\": \"what remains to be done\"}.",
  "model": "claude-haiku-4-5-20251001"
}
```

Response format:
- `{"ok": true}` — proceed
- `{"ok": false, "reason": "..."}` — block; reason fed to Claude

Default model: Haiku. Use `model` to override.

### 3. `agent` — Multi-turn verification with tool access

Runs a full Claude agent (up to 50 tool-use turns) for deep validation. Use for complex checks like running tests.

```json
{
  "type": "agent",
  "prompt": "Verify all unit tests pass. Run the test suite and check results. $ARGUMENTS",
  "timeout": 120
}
```

Same `ok`/`reason` response format. Default timeout: 60 seconds.

### 4. `http` — POST to HTTP endpoint

Posts event data to a URL. Use for webhooks, audit logging, external CI gates.

```json
{
  "type": "http",
  "url": "http://localhost:8080/hooks/tool-use",
  "headers": {
    "Authorization": "Bearer $MY_TOKEN"
  },
  "allowedEnvVars": ["MY_TOKEN"]
}
```

Must return 2xx with `hookSpecificOutput` JSON to block. Environment variables in headers require `allowedEnvVars` declaration.

## Structured JSON Output (exit 0)

Command hooks can return structured decisions via stdout on exit 0:

```json
{
  "hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "permissionDecision": "deny",
    "permissionDecisionReason": "Use rg instead of grep for better performance"
  }
}
```

Auto-approve `ExitPlanMode` example:
```bash
echo '{"hookSpecificOutput": {"hookEventName": "PermissionRequest", "decision": {"behavior": "allow"}}}'
exit 0
```

## Lifecycle Events

| Event | When | Matcher |
|-------|------|---------|
| `SessionStart` | Session begins, resumes, or compacts | `startup`, `resume`, `compact`, `clear` |
| `UserPromptSubmit` | Before Claude processes prompt | — |
| `PreToolUse` | Before tool executes | Tool name regex |
| `PermissionRequest` | Permission dialog appears | Tool name |
| `PostToolUse` | After tool succeeds | Tool name regex |
| `PostToolUseFailure` | After tool fails | Tool name regex |
| `Notification` | Claude sends notification | `permission_prompt`, `idle_prompt`, `auth_success` |
| `SubagentStart` | Subagent spawned | Agent name |
| `SubagentStop` | Subagent finishes | Agent name |
| `TaskCreated` | Task created via TaskCreate | — |
| `TaskCompleted` | Task marked complete | — |
| `Stop` | Claude finishes responding | — |
| `StopFailure` | Turn ends due to API error | — |
| `TeammateIdle` | Agent team member about to idle | — |
| `PreCompact` | Before context compaction | — |
| `PostCompact` | After context compaction | — |
| `ConfigChange` | Settings file changes | `user_settings`, `project_settings`, `skills` |
| `CwdChanged` | Working directory changes | — |
| `FileChanged` | Watched file changes on disk | Filename pattern |
| `WorktreeCreate` / `WorktreeRemove` | Worktree lifecycle | — |
| `Elicitation` / `ElicitationResult` | MCP server requests user input | — |
| `SessionEnd` | Session terminates | `clear`, `resume`, `logout` |
| `InstructionsLoaded` | CLAUDE.md or rules loaded | — |

## Conditional Filtering with `if`

The `if` field filters within a matched event before running the hook:

```json
{
  "matcher": "Bash",
  "hooks": [
    {
      "type": "command",
      "if": "Bash(git *)",
      "command": "${CLAUDE_PROJECT_DIR}/.claude/hooks/check-git-policy.sh"
    }
  ]
}
```

## hooks.json Format (Kit-Level)

```json
{
  "hooks": {
    "UserPromptSubmit": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "bash ${CLAUDE_PLUGIN_ROOT}/hooks/check-settings.sh"
          }
        ]
      }
    ],
    "PostToolUse": [
      {
        "matcher": "Write|Edit",
        "hooks": [
          {
            "type": "command",
            "command": "bash ${CLAUDE_PLUGIN_ROOT}/hooks/validate-skill-frontmatter.sh"
          }
        ]
      }
    ]
  }
}
```

Use `${CLAUDE_PLUGIN_ROOT}` — never hardcode absolute paths.

## Hook Scope

| Location | Scope |
|----------|-------|
| `~/.claude/settings.json` | All projects (user-level) |
| `.claude/settings.json` | Single project |
| `{plugin}/hooks/hooks.json` | When plugin enabled |
| Skill/agent frontmatter | While skill/agent is active |

## Platform Notes

- Always use `#!/usr/bin/env bash` shebang (not `/bin/bash`)
- Always `set -euo pipefail` for safety
- Always `INPUT=$(cat)` to read stdin event payload
- Test on target platform: Git Bash on Windows behaves differently than WSL or Mac

## Debugging

- Toggle verbose mode: `Ctrl+O` in Claude Code
- Run with `claude --debug` for full execution trace
- Check `/hooks` menu to verify registered hooks and matchers
- Test manually: `echo '{"tool_name":"Bash","tool_input":{"command":"ls"}}' | bash ./my-hook.sh`

**Hook not firing?** Check: matcher is case-sensitive, correct event name, hook file is executable (`chmod +x`).

**JSON validation failed?** Check `~/.zshrc`/`~/.bashrc` for unconditional `echo` statements — wrap them: `if [[ $- == *i* ]]; then echo "..."; fi`
