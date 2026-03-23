#!/usr/bin/env bash
# check-settings.sh — Verify github-kit config exists and required fields are set.
# Runs on UserPromptSubmit. Outputs a notice injected as context by the harness.
# If config is missing entirely, outputs a SETUP REQUIRED message that instructs
# Claude to immediately run /github-setup before doing anything else.
# Always exits 0 — advisory only, never blocks the user's prompt.

USER_CONFIG="${HOME}/.claude/github-kit.config.md"

# ── 1. Config file missing ──────────────────────────────────────────────────

if [[ ! -f "$USER_CONFIG" ]]; then
  echo "SETUP REQUIRED: ~/.claude/github-kit.config.md is missing. Run /github-setup now to configure your GitHub org and PR defaults before using any github-kit skills."
  exit 0
fi

# ── 2. Required fields empty ────────────────────────────────────────────────

MISSING=()

check_field() {
  local field="$1"
  local value
  value=$(grep -m1 "^${field}=" "$USER_CONFIG" 2>/dev/null | sed 's/^[^=]*=//' | sed 's/[[:space:]]*#.*//' | tr -d '[:space:]')
  if [[ -z "$value" ]]; then
    MISSING+=("$field")
  fi
}

check_field "GITHUB_ORG"

if [[ ${#MISSING[@]} -gt 0 ]]; then
  echo "SETUP INCOMPLETE: The following github-kit settings are not configured in ~/.claude/github-kit.config.md: ${MISSING[*]}. Run /github-setup to complete setup — skills that fetch PRs or repo data will not work without these values."
fi

exit 0
