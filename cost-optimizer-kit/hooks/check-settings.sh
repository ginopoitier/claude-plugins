#!/usr/bin/env bash
# check-settings.sh — Advisory hook for cost-optimizer-kit
# Checks that cost-optimizer-kit.config.md exists. Exits 0 always (advisory only).
# Runs on: UserPromptSubmit

CONFIG_FILE="${HOME}/.claude/cost-optimizer-kit.config.md"

if [[ ! -f "$CONFIG_FILE" ]]; then
  echo "NOTICE: ~/.claude/cost-optimizer-kit.config.md is missing. Run /optimize-cost --setup to configure your budget threshold and model preferences." >&2
fi

exit 0
