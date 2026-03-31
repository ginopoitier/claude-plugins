#!/usr/bin/env bash
# memory-inject.sh — Inject memory context at session start
# Runs on: SessionStart
# Loads MEMORY.md for the active project and outputs a context summary.
# stdout is injected into Claude's context; stderr is logged only.

set -euo pipefail

INPUT=$(cat)  # Read SessionStart JSON payload (unused for now, reserved for future filtering)

CONFIG_FILE="${HOME}/.claude/memory-kit.config.md"
MEMORY_BASE="${MEMORY_BASE_PATH:-$HOME/.claude/projects}"

# Check if injection is disabled in config
if [[ -f "$CONFIG_FILE" ]]; then
  if grep -qi "AUTO_INJECT=false" "$CONFIG_FILE" 2>/dev/null; then
    exit 0
  fi
  # Read custom base path from config if set
  CONFIGURED_BASE=$(grep "^MEMORY_BASE_PATH=" "$CONFIG_FILE" 2>/dev/null | cut -d'=' -f2 | tr -d ' ' || true)
  if [[ -n "$CONFIGURED_BASE" ]]; then
    MEMORY_BASE="${CONFIGURED_BASE/#\~/$HOME}"
  fi
fi

# Detect project ID
PROJECT_ID=""
# Try git remote first
if command -v git &>/dev/null; then
  GIT_REMOTE=$(git remote get-url origin 2>/dev/null || true)
  if [[ -n "$GIT_REMOTE" ]]; then
    PROJECT_ID=$(echo "$GIT_REMOTE" | sed 's/.*[:/]//' | sed 's/\.git$//' | tr '[:upper:]' '[:lower:]' | tr ' ' '-')
  fi
fi
# Fall back to directory name
if [[ -z "$PROJECT_ID" ]]; then
  PROJECT_ID=$(basename "$(pwd)" | tr '[:upper:]' '[:lower:]' | tr ' ' '-')
fi

MEMORY_DIR="$MEMORY_BASE/$PROJECT_ID/memory"
MEMORY_INDEX="$MEMORY_BASE/$PROJECT_ID/MEMORY.md"

# Try memory-mcp CLI for rich injection (if installed)
if command -v memory-mcp &>/dev/null; then
  SUMMARY=$(memory-mcp list --project "$PROJECT_ID" --format summary 2>/dev/null || true)
  if [[ -n "$SUMMARY" ]]; then
    echo "MEMORY-KIT [project: $PROJECT_ID]: $SUMMARY"
    exit 0
  fi
fi

# Fallback: read MEMORY.md index directly
if [[ -f "$MEMORY_INDEX" ]]; then
  MEM_COUNT=$(grep -c "^-" "$MEMORY_INDEX" 2>/dev/null || echo "0")
  if [[ "$MEM_COUNT" -gt 0 ]]; then
    echo "MEMORY-KIT [project: $PROJECT_ID]: $MEM_COUNT memories loaded. Top entries:"
    grep "^-" "$MEMORY_INDEX" | head -8
    if [[ "$MEM_COUNT" -gt 8 ]]; then
      echo "  ... and $((MEM_COUNT - 8)) more. Use /memory-recall to search."
    fi
  fi
elif [[ -d "$MEMORY_DIR" ]]; then
  FILE_COUNT=$(find "$MEMORY_DIR" -name "*.md" 2>/dev/null | wc -l || echo "0")
  if [[ "$FILE_COUNT" -gt 0 ]]; then
    echo "MEMORY-KIT [project: $PROJECT_ID]: $FILE_COUNT memory files found but MEMORY.md index is missing. Run /memory-health to rebuild." >&2
  fi
fi

exit 0
