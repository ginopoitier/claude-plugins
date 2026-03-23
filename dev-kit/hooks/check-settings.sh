#!/usr/bin/env bash
# check-settings.sh — Warn when kit.config.md is missing or has unconfigured required fields.
# Runs on UserPromptSubmit. Outputs a reminder message (injected as context by the harness).
# Always exits 0 — this is advisory only, never blocks the user's prompt.

KIT_CONFIG="${HOME}/.claude/kit.config.md"

# ── 1. Config file missing ──────────────────────────────────────────────────

if [[ ! -f "$KIT_CONFIG" ]]; then
  echo "NOTICE: ~/.claude/kit.config.md is missing. Run /kit-setup to configure the dev-kit before using skills that depend on config (VCS, CI/CD, PM, docs, scaffolding)."
  exit 0
fi

# ── 2. Required fields empty ────────────────────────────────────────────────
# These fields are left blank in the template and must be filled in by the user.

MISSING=()

check_field() {
  local field="$1"
  # Match lines like: FIELD=   or  FIELD=   # comment
  # A field is "unset" if its value is empty or just whitespace (before any #)
  local value
  value=$(grep -m1 "^${field}=" "$KIT_CONFIG" 2>/dev/null | sed 's/^[^=]*=//' | sed 's/[[:space:]]*#.*//' | tr -d '[:space:]')
  if [[ -z "$value" ]]; then
    MISSING+=("$field")
  fi
}

check_field "DEFAULT_NAMESPACE"

if [[ ${#MISSING[@]} -gt 0 ]]; then
  echo "NOTICE: The following dev-kit settings are not configured in ~/.claude/kit.config.md: ${MISSING[*]}. Run /kit-setup to fill them in. Skills that depend on these values may not work correctly."
fi

exit 0
