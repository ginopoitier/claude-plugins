#!/usr/bin/env bash
# check-settings.sh — Warn when kit.config.md is missing (vue-kit).
# Runs on UserPromptSubmit. Always exits 0 — advisory only.

KIT_CONFIG="${HOME}/.claude/kit.config.md"

if [[ ! -f "$KIT_CONFIG" ]]; then
  echo "NOTICE: ~/.claude/kit.config.md is missing. Run /kit-setup to configure the vue-kit before using skills that depend on config."
  exit 0
fi

exit 0
