#!/usr/bin/env bash
# pre-bash-guard.sh — Block destructive Bitbucket API calls.
# Runs on PreToolUse for Bash tool calls.
# Reads the tool input JSON from stdin.
# Exit 2 to block with message; exit 0 to allow.

INPUT=$(cat)
# Extract the command from the Bash tool input JSON
COMMAND=$(echo "$INPUT" | python3 -c "import json,sys; print(json.load(sys.stdin).get('command',''))" 2>/dev/null || echo "")

# Block DELETE calls to the Bitbucket API
if echo "$COMMAND" | grep -qiE "curl[^|]*-X[[:space:]]+DELETE[^|]*bitbucket"; then
  echo "BLOCKED: Destructive Bitbucket API call (DELETE) requires explicit user confirmation. bitbucket-kit does not perform destructive operations automatically. If you intend to delete a Bitbucket resource, confirm the exact resource and run the command manually."
  exit 2
fi

# Block PR merge calls
if echo "$COMMAND" | grep -qiE "curl[^|]*/pullrequests/[0-9]+/merge"; then
  echo "BLOCKED: PR merge is a human decision. bitbucket-kit does not merge pull requests. Approve the PR in the Bitbucket UI or via the Bitbucket app."
  exit 2
fi

exit 0
