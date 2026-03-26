#!/usr/bin/env bash
# Pre-Bash Guard — Block destructive operations (vue-kit variant).
# Exit 2 = block, exit 0 = allow.

COMMAND="${CLAUDE_TOOL_INPUT}"

# ── Destructive git operations ──────────────────────────────────────────────

if echo "$COMMAND" | grep -qE 'git\s+push\s+.*--force|git\s+push\s+-f\b'; then
  echo "BLOCKED: Force push detected. Discuss with the user first."
  exit 2
fi

if echo "$COMMAND" | grep -qE 'git\s+reset\s+--hard'; then
  echo "BLOCKED: git reset --hard will discard all uncommitted changes. Discuss with the user first."
  exit 2
fi

# ── Dangerous file operations ───────────────────────────────────────────────

if echo "$COMMAND" | grep -qE 'rm\s+-[a-zA-Z]*r[a-zA-Z]*f|rm\s+-[a-zA-Z]*f[a-zA-Z]*r'; then
  if echo "$COMMAND" | grep -qE 'rm\s+-rf\s+(node_modules|dist|\.nuxt|\.vite|/tmp)'; then
    : # safe targets, allow
  else
    echo "WARNING: rm -rf detected. Verify the target path is intentional."
    exit 2
  fi
fi

exit 0
