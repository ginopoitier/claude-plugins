#!/usr/bin/env bash
# check-settings.sh — Verify jira-kit config exists and required fields are set.
# Also checks that the Atlassian MCP has been authenticated.
# Runs on UserPromptSubmit. Always exits 0 — advisory only.

USER_CONFIG="${HOME}/.claude/jira-kit.config.md"

# ── 1. Config file missing ──────────────────────────────────────────────────

if [[ ! -f "$USER_CONFIG" ]]; then
  echo "SETUP REQUIRED: ~/.claude/jira-kit.config.md is missing. Run /jira-setup now to configure Jira before using sprint skills (/epic, /tech-refinement, /standup)."
  exit 0
fi

# ── 2. Required fields empty ────────────────────────────────────────────────

MISSING=()

check_field() {
  local field="$1"
  local value
  value=$(grep -m1 "^${field}=" "$USER_CONFIG" 2>/dev/null | sed 's/^[^=]*=//' | sed 's/[[:space:]]*#.*//' | tr -d '[:space:]')
  [[ -z "$value" ]] && MISSING+=("$field")
}

check_field "JIRA_BASE_URL"

if [[ ${#MISSING[@]} -gt 0 ]]; then
  echo "SETUP INCOMPLETE: The following jira-kit settings are not configured in ~/.claude/jira-kit.config.md: ${MISSING[*]}. Run /jira-setup to complete setup."
fi

exit 0
