#!/usr/bin/env bash
# Post-edit hook: auto-format changed .ts and .vue files using Prettier.
# Runs after Claude edits a file. Always exits 0 — non-blocking.

set -euo pipefail

FILE="${1:-${CLAUDE_EDITED_FILE:-}}"

# Fallback: parse file_path from PostToolUse stdin JSON
if [[ -z "$FILE" ]] && [[ ! -t 0 ]]; then
    STDIN=$(cat)
    FILE=$(echo "$STDIN" | grep -o '"file_path" *: *"[^"]*"' | head -1 | grep -o '"[^"]*"$' | tr -d '"') || true
fi

if [[ -z "$FILE" ]]; then
    exit 0
fi

# Only format TypeScript and Vue files
if [[ "$FILE" != *.ts && "$FILE" != *.vue && "$FILE" != *.tsx ]]; then
    exit 0
fi

if [[ ! -f "$FILE" ]]; then
    exit 0
fi

# Find nearest package.json to locate the Prettier binary
DIR=$(dirname "$FILE")
PRETTIER=""
while [[ "$DIR" != "/" && "$DIR" != "." ]]; do
    LOCAL_PRETTIER="$DIR/node_modules/.bin/prettier"
    if [[ -f "$LOCAL_PRETTIER" ]]; then
        PRETTIER="$LOCAL_PRETTIER"
        break
    fi
    DIR=$(dirname "$DIR")
done

if [[ -n "$PRETTIER" ]]; then
    "$PRETTIER" --write "$FILE" 2>/dev/null || true
else
    echo "Prettier not found for $FILE, skipping format"
fi
