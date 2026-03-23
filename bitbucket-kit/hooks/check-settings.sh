#!/usr/bin/env bash
# check-settings.sh — Verify bitbucket-kit config exists and required fields are set.
# Also checks that the BITBUCKET_API_TOKEN env var is present in the shell.
# Runs on UserPromptSubmit. Outputs a notice injected as context by the harness.
# Always exits 0 — advisory only, never blocks the user's prompt.

USER_CONFIG="${HOME}/.claude/bitbucket-kit.config.md"

# ── 1. Config file missing ──────────────────────────────────────────────────

if [[ ! -f "$USER_CONFIG" ]]; then
  echo "SETUP REQUIRED: ~/.claude/bitbucket-kit.config.md is missing. Run /bitbucket-setup now to configure your Bitbucket workspace and API token before using any bitbucket-kit skills."
  exit 0
fi

# ── 2. Required fields empty ────────────────────────────────────────────────

MISSING=()

check_field() {
  local field="$1"
  local value
  value=$(grep -m1 "^${field}=" "$USER_CONFIG" 2>/dev/null | sed 's/^[^=]*=//' | sed 's/[[:space:]]*#.*//' | tr -d '[:space:]')
  # Skip placeholder values like ${VAR_NAME}
  if [[ -z "$value" || "$value" == "\${*}" ]]; then
    MISSING+=("$field")
  fi
}

check_field "BITBUCKET_WORKSPACE"

if [[ ${#MISSING[@]} -gt 0 ]]; then
  echo "SETUP INCOMPLETE: The following bitbucket-kit settings are not configured in ~/.claude/bitbucket-kit.config.md: ${MISSING[*]}. Run /bitbucket-setup to complete setup."
fi

# ── 3. API token env var missing ────────────────────────────────────────────

if [[ -z "${BITBUCKET_API_TOKEN}" ]]; then
  echo "TOKEN MISSING: The BITBUCKET_API_TOKEN environment variable is not set. PR operations will fail without it. Set it with: setx BITBUCKET_API_TOKEN \"your-token\" (Windows) or export BITBUCKET_API_TOKEN=\"your-token\" (bash), then restart your terminal."
fi

exit 0
