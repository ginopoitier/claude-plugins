#!/usr/bin/env bash
# auto-capture.sh — Advisory capture nudge after Claude's response
# Runs on: Stop
# Detects correction and discovery patterns; writes a pending-capture flag
# that memory-inject.sh picks up on the next UserPromptSubmit.
# Always exits 0 (advisory only — never blocks).

set -euo pipefail

INPUT=$(cat)  # Read Stop JSON payload

MEMORY_BASE="${MEMORY_BASE_PATH:-$HOME/.claude/projects}"
FLAG_DIR="${HOME}/.claude/memory-kit"
FLAG_FILE="${FLAG_DIR}/.pending-capture"

# Ensure flag directory exists
mkdir -p "$FLAG_DIR"

# Extract response text from payload if available
RESPONSE_TEXT=""
if command -v python3 &>/dev/null; then
  RESPONSE_TEXT=$(echo "$INPUT" | python3 -c "
import sys, json
try:
    d = json.load(sys.stdin)
    for key in ['response', 'content', 'message', 'output', 'text']:
        if key in d and isinstance(d[key], str):
            print(d[key][:2000])
            break
except Exception:
    pass
" 2>/dev/null || true)
fi

# Patterns that suggest a correction or important insight occurred
CORRECTION_PATTERNS=(
  "got it"
  "added to memory"
  "captured to memory"
  "from now on"
  "going forward"
  "i'll remember"
  "noted"
  "i'll use"
  "updated memory"
  "stored as"
)

DISCOVERY_PATTERNS=(
  "root cause"
  "discovered"
  "turns out"
  "the reason is"
  "undocumented"
  "workaround"
  "gotcha"
)

if [[ -n "$RESPONSE_TEXT" ]]; then
  LOWER=$(echo "$RESPONSE_TEXT" | tr '[:upper:]' '[:lower:]')
  for PATTERN in "${CORRECTION_PATTERNS[@]}"; do
    if echo "$LOWER" | grep -q "$PATTERN"; then
      echo "correction" > "$FLAG_FILE"
      exit 0
    fi
  done
  for PATTERN in "${DISCOVERY_PATTERNS[@]}"; do
    if echo "$LOWER" | grep -q "$PATTERN"; then
      echo "discovery" > "$FLAG_FILE"
      exit 0
    fi
  done
fi

# Clean up stale flag if nothing detected this turn
if [[ -f "$FLAG_FILE" ]]; then
  FILE_AGE=$(( $(date +%s) - $(date -r "$FLAG_FILE" +%s 2>/dev/null || echo 0) ))
  if [[ "$FILE_AGE" -gt 300 ]]; then  # 5 minutes stale
    rm -f "$FLAG_FILE"
  fi
fi

exit 0
