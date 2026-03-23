#!/usr/bin/env bash
# check-settings.sh — Verify git-kit config exists and required identity fields are set.
# Runs on UserPromptSubmit. Always exits 0 — advisory only.

USER_CONFIG="${HOME}/.claude/git-kit.config.md"

# ── 1. Config file missing ──────────────────────────────────────────────────

if [[ ! -f "$USER_CONFIG" ]]; then
  echo "SETUP REQUIRED: ~/.claude/git-kit.config.md is missing. Run /git-setup now to configure your git identity and commit preferences before using any git-kit skills."
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

check_field "GIT_USER_NAME"
check_field "GIT_USER_EMAIL"

if [[ ${#MISSING[@]} -gt 0 ]]; then
  echo "SETUP INCOMPLETE: The following git-kit settings are not configured in ~/.claude/git-kit.config.md: ${MISSING[*]}. Run /git-setup to complete setup — git commits will use incorrect identity without these values."
fi

exit 0
