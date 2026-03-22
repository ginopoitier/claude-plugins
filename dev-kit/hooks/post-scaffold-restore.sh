#!/usr/bin/env bash
# Post-scaffold hook: restore NuGet packages after .csproj changes
# Triggered when a .csproj file is created or modified.
#
# Usage:
#   Called automatically by Claude Code PostToolUse hook after Edit/Write.
#   Accepts file path via:
#     1. First argument ($1)
#     2. CLAUDE_EDITED_FILE env var
#     3. PostToolUse stdin JSON ({"tool_input":{"file_path":"..."}})
#   When called with no input (manual use), restore runs unconditionally.

set -euo pipefail

FILE="${1:-${CLAUDE_EDITED_FILE:-}}"

# Fallback: parse file_path from PostToolUse stdin JSON
if [[ -z "$FILE" ]] && [[ ! -t 0 ]]; then
    STDIN=$(cat)
    FILE=$(echo "$STDIN" | grep -o '"file_path" *: *"[^"]*"' | head -1 | grep -o '"[^"]*"$' | tr -d '"') || true
fi

# If we have a file path, only restore for .csproj files
if [[ -n "$FILE" ]]; then
    if [[ "$FILE" != *.csproj ]]; then
        exit 0
    fi
fi

# Run restore
echo "Project file changed. Running dotnet restore..."

if dotnet restore --verbosity quiet 2>/dev/null; then
    echo "Restore completed."
else
    echo "Warning: dotnet restore failed. You may need to restore manually."
fi
