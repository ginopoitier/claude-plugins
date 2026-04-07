#!/usr/bin/env bash
# budget-alert.sh — Advisory budget alert on UserPromptSubmit
# Runs on: UserPromptSubmit
# Checks if a monthly budget threshold is configured and warns when approaching.
# Always exits 0 (advisory only — never blocks prompts).

set -euo pipefail

INPUT=$(cat)  # Read UserPromptSubmit JSON payload

CONFIG_FILE="${HOME}/.claude/cost-optimizer-kit.config.md"

# Read budget threshold from config; default = 0 (disabled)
MONTHLY_BUDGET_USD=0
if [[ -f "$CONFIG_FILE" ]]; then
  CONFIGURED=$(grep "^MONTHLY_BUDGET_USD=" "$CONFIG_FILE" 2>/dev/null | cut -d'=' -f2 | tr -d ' ' || true)
  if [[ -n "$CONFIGURED" ]] && [[ "$CONFIGURED" =~ ^[0-9]+(\.[0-9]+)?$ ]]; then
    MONTHLY_BUDGET_USD="$CONFIGURED"
  fi
fi

# Budget = 0 means disabled; exit silently
if [[ "$MONTHLY_BUDGET_USD" == "0" ]] || [[ -z "$MONTHLY_BUDGET_USD" ]]; then
  exit 0
fi

# Check for usage log path
USAGE_LOG=""
if [[ -f "$CONFIG_FILE" ]]; then
  CONFIGURED_LOG=$(grep "^USAGE_LOG_PATH=" "$CONFIG_FILE" 2>/dev/null | cut -d'=' -f2 | tr -d ' ' || true)
  if [[ -n "$CONFIGURED_LOG" ]]; then
    USAGE_LOG="${CONFIGURED_LOG/#\~/$HOME}"
  fi
fi

# If no usage log configured, skip budget check silently
if [[ -z "$USAGE_LOG" ]] || [[ ! -f "$USAGE_LOG" ]]; then
  exit 0
fi

# Calculate current month's spend from usage log (expects lines: date,cost_usd)
CURRENT_MONTH=$(date +%Y-%m)
MONTH_SPEND=$(grep "^${CURRENT_MONTH}" "$USAGE_LOG" 2>/dev/null \
  | awk -F',' '{sum += $2} END {printf "%.2f", sum}' || echo "0")

# Calculate percentage of budget used
BUDGET_PCT=$(echo "$MONTH_SPEND $MONTHLY_BUDGET_USD" | awk '{printf "%.0f", ($1 / $2) * 100}' 2>/dev/null || echo "0")

# Warn at 80% and 100%
if [[ "$BUDGET_PCT" -ge 100 ]]; then
  echo "COST ALERT: Monthly budget exceeded. Spent: \$${MONTH_SPEND} / \$${MONTHLY_BUDGET_USD}. Consider switching to Haiku for simple tasks or compacting the conversation." >&2
elif [[ "$BUDGET_PCT" -ge 80 ]]; then
  echo "COST ADVISORY: ${BUDGET_PCT}% of monthly budget used (\$${MONTH_SPEND} / \$${MONTHLY_BUDGET_USD}). Prefer Haiku for lookups this session." >&2
fi

exit 0
