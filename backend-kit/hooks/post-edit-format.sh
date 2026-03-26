#!/usr/bin/env bash
# Post-edit hook: auto-format changed .cs files
# Runs dotnet format on specific files after Claude edits them.
#
# Usage:
#   Called automatically by Claude Code PostToolUse hook after Edit/Write on .cs files.
#   Accepts file path via:
#     1. First argument ($1)
#     2. CLAUDE_EDITED_FILE env var
#     3. PostToolUse stdin JSON ({"tool_input":{"file_path":"..."}})

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

# Only format C# files
if [[ "$FILE" != *.cs ]]; then
    exit 0
fi

# Skip if file doesn't exist (deleted)
if [[ ! -f "$FILE" ]]; then
    exit 0
fi

# Find the nearest .csproj or .sln to scope the format
DIR=$(dirname "$FILE")
PROJECT=""
while [[ "$DIR" != "/" && "$DIR" != "." ]]; do
    # Check for .csproj first (more specific)
    CSPROJ=$(find "$DIR" -maxdepth 1 -name "*.csproj" -print -quit 2>/dev/null || true)
    if [[ -n "$CSPROJ" ]]; then
        PROJECT="$CSPROJ"
        break
    fi
    # Check for .sln
    SLN=$(find "$DIR" -maxdepth 1 -name "*.sln" -print -quit 2>/dev/null || true)
    if [[ -n "$SLN" ]]; then
        PROJECT="$SLN"
        break
    fi
    DIR=$(dirname "$DIR")
done

if [[ -n "$PROJECT" ]]; then
    dotnet format "$PROJECT" --include "$FILE" --no-restore 2>/dev/null || true
else
    echo "No .csproj or .sln found for $FILE, skipping format"
fi
