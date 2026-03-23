#!/usr/bin/env bash
# check-settings.sh — Verify confluence-kit config exists and required fields are set.
# Runs on UserPromptSubmit. Always exits 0 — advisory only.

USER_CONFIG="${HOME}/.claude/confluence-kit.config.md"

# ── 1. Config file missing ──────────────────────────────────────────────────

if [[ ! -f "$USER_CONFIG" ]]; then
  echo "SETUP REQUIRED: ~/.claude/confluence-kit.config.md is missing. Run /confluence-setup now to configure Confluence before using documentation skills (/adr, /sdr)."
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

check_field "CONFLUENCE_BASE_URL"
check_field "CONFLUENCE_DEFAULT_SPACE_KEY"

if [[ ${#MISSING[@]} -gt 0 ]]; then
  echo "SETUP INCOMPLETE: The following confluence-kit settings are not configured in ~/.claude/confluence-kit.config.md: ${MISSING[*]}. Run /confluence-setup to complete setup."
fi

exit 0
