#!/usr/bin/env bash
# check-settings.sh — Advisory hook for kit-maker
# Checks that kit.config.md exists. Exits 0 always (advisory only).

CONFIG_FILE="${HOME}/.claude/kit.config.md"

if [[ ! -f "$CONFIG_FILE" ]]; then
  echo "NOTICE: ~/.claude/kit.config.md is missing. Run /kit-setup to configure the kit-maker before using skills that depend on config (author name, marketplace username, kit base path)."
fi

exit 0
