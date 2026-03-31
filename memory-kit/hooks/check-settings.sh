#!/usr/bin/env bash
# check-settings.sh — Advisory hook for memory-kit
# Checks that memory-kit.config.md exists. Exits 0 always (advisory only).
# Runs on: UserPromptSubmit

CONFIG_FILE="${HOME}/.claude/memory-kit.config.md"

if [[ ! -f "$CONFIG_FILE" ]]; then
  echo "MEMORY-KIT: ~/.claude/memory-kit.config.md is missing. Run /memory-setup to configure memory base path, project ID strategy, and auto-capture settings." >&2
fi

exit 0
