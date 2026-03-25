#!/usr/bin/env bash
# check-settings.sh — Verify bitbucket-kit config exists and required fields are set.
# Also checks that the BITBUCKET_API_TOKEN env var is present in the shell.
# When inside a git repo with a Bitbucket remote, also checks for project-level config.
# Runs on UserPromptSubmit. Outputs notices injected as context by the harness.
# Always exits 0 — advisory only, never blocks the user's prompt.

USER_CONFIG="${HOME}/.claude/bitbucket-kit.config.md"

# ── 1. User config file missing ─────────────────────────────────────────────

if [[ ! -f "$USER_CONFIG" ]]; then
  echo "SETUP REQUIRED: ~/.claude/bitbucket-kit.config.md is missing. Run /bitbucket-setup now to configure your Bitbucket workspace and API token before using any bitbucket-kit skills."
  exit 0
fi

# ── 2. Required user config fields empty ────────────────────────────────────

MISSING=()

check_field() {
  local field="$1"
  local value
  value=$(grep -m1 "^${field}=" "$USER_CONFIG" 2>/dev/null | cut -d= -f2- | sed 's/[[:space:]]*#.*//' | tr -d '[:space:]')
  # Treat empty or unresolved placeholder (${...}) as missing
  if [[ -z "$value" ]] || [[ "$value" =~ ^\$\{[^}]+\}$ ]]; then
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

# ── 4. Project-level config check (when inside a Bitbucket repo) ────────────

PROJECT_CONFIG=".claude/bitbucket.config.md"

# Only check if we're in a git repo with a Bitbucket remote
if git rev-parse --git-dir > /dev/null 2>&1; then
  REMOTE_URL=$(git remote get-url origin 2>/dev/null || echo "")
  if echo "$REMOTE_URL" | grep -qi "bitbucket"; then
    if [[ ! -f "$PROJECT_CONFIG" ]]; then
      echo "PROJECT CONFIG MISSING: This repo has a Bitbucket remote but .claude/bitbucket.config.md is missing. Run /bitbucket-setup --project to generate it and commit it to the repo."
    else
      # Check that BITBUCKET_REPO is set in project config
      REPO_VAL=$(grep -m1 "^BITBUCKET_REPO=" "$PROJECT_CONFIG" 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
      if [[ -z "$REPO_VAL" ]]; then
        echo "PROJECT CONFIG INCOMPLETE: BITBUCKET_REPO is not set in .claude/bitbucket.config.md. Run /bitbucket-setup --project to complete project setup."
      fi
    fi
  fi
fi

exit 0
