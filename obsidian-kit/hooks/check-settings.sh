#!/usr/bin/env bash
# check-settings.sh — Verify obsidian-kit config exists, required fields are set,
# and the vault directory actually exists on this machine.
# Runs on UserPromptSubmit. Always exits 0 — advisory only.

USER_CONFIG="${HOME}/.claude/obsidian-kit.config.md"

# ── 1. Config file missing ──────────────────────────────────────────────────

if [[ ! -f "$USER_CONFIG" ]]; then
  echo "SETUP REQUIRED: ~/.claude/obsidian-kit.config.md is missing. Run /obsidian-setup now to configure your vault path before using note-taking skills."
  exit 0
fi

# ── 2. Required field empty ─────────────────────────────────────────────────

VAULT=$(grep -m1 "^OBSIDIAN_VAULT_PATH=" "$USER_CONFIG" 2>/dev/null | sed 's/^[^=]*=//' | sed 's/[[:space:]]*#.*//' | tr -d '[:space:]')

if [[ -z "$VAULT" ]]; then
  echo "SETUP INCOMPLETE: OBSIDIAN_VAULT_PATH is not set in ~/.claude/obsidian-kit.config.md. Run /obsidian-setup to configure your vault path."
  exit 0
fi

# ── 3. Vault directory missing ───────────────────────────────────────────────

# Expand ~ if present
VAULT_EXPANDED="${VAULT/#\~/$HOME}"

if [[ ! -d "$VAULT_EXPANDED" ]]; then
  echo "VAULT NOT FOUND: The configured Obsidian vault path does not exist: ${VAULT}. Check the path in ~/.claude/obsidian-kit.config.md or run /obsidian-setup to reconfigure."
fi

exit 0
