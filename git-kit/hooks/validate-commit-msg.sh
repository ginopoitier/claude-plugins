#!/bin/bash
# validate-commit-msg.sh
# commit-msg hook: validates commit message format
# Install: cp hooks/validate-commit-msg.sh .git/hooks/commit-msg && chmod +x .git/hooks/commit-msg
# Or with shared hooks: git config core.hooksPath hooks/

MSG_FILE="$1"
MSG=$(cat "$MSG_FILE")

# Skip merge commits, fixup commits, and squash commits
if echo "$MSG" | grep -qE "^(Merge|fixup!|squash!) "; then
  exit 0
fi

# Skip commits that start with # (comments only)
STRIPPED=$(echo "$MSG" | grep -v "^#" | head -1)
if [ -z "$STRIPPED" ]; then
  exit 0
fi

ERRORS=()

# Check subject line length
SUBJECT=$(echo "$MSG" | grep -v "^#" | head -1)
if [ ${#SUBJECT} -gt 72 ]; then
  ERRORS+=("Subject line too long (${#SUBJECT} chars, max 72)")
fi

# Check for generic/bad messages
BAD_PATTERNS="^(wip|fix|update|changes|stuff|temp|tmp|test|asdf|xxx)$"
if echo "${SUBJECT,,}" | grep -qiE "$BAD_PATTERNS"; then
  ERRORS+=("Commit message is too vague: '$SUBJECT'")
fi

# Check COMMIT_CONVENTION from project config if present
CONFIG_FILE=".claude/git.config.md"
CONVENTION=""
if [ -f "$CONFIG_FILE" ]; then
  CONVENTION=$(grep "^COMMIT_CONVENTION=" "$CONFIG_FILE" | cut -d= -f2- | tr -d '[:space:]')
fi

# Also check user config
USER_CONFIG="$HOME/.claude/git-kit.config.md"
if [ -z "$CONVENTION" ] && [ -f "$USER_CONFIG" ]; then
  CONVENTION=$(grep "^COMMIT_STYLE=" "$USER_CONFIG" | cut -d= -f2- | tr -d '[:space:]')
fi

# Validate conventional format if configured
if [ "$CONVENTION" = "conventional" ]; then
  PATTERN='^(feat|fix|docs|style|refactor|test|chore|perf|ci)(\(.+\))?!?: .{1,72}'
  if ! echo "$SUBJECT" | grep -qE "$PATTERN"; then
    ERRORS+=("Conventional commit format required: type(scope): subject")
    ERRORS+=("  Types: feat fix docs style refactor test chore perf ci")
    ERRORS+=("  Example: feat(auth): add JWT refresh token rotation")
  fi
fi

# Report
if [ ${#ERRORS[@]} -gt 0 ]; then
  echo ""
  echo "❌ Commit message validation failed:"
  for err in "${ERRORS[@]}"; do
    echo "   ✗ $err"
  done
  echo ""
  echo "   Your message: $SUBJECT"
  echo ""
  exit 1
fi

exit 0
