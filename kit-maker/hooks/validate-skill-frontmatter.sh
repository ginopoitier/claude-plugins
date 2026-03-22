#!/bin/bash
# validate-skill-frontmatter.sh
# PostToolUse hook: runs after Write/Edit tool calls to validate SKILL.md files
# Registered in settings.json PostToolUse for Write and Edit matchers
#
# Usage: called automatically by Claude Code hooks system
# Returns: exit 0 (pass) or exit 1 (fail with message)

set -e

# Get the file path that was just written from the hook context
FILE_PATH="${CLAUDE_TOOL_INPUT_PATH:-}"

# Only validate SKILL.md files
if [[ "$FILE_PATH" != *"SKILL.md" ]]; then
  exit 0
fi

if [ ! -f "$FILE_PATH" ]; then
  exit 0
fi

ERRORS=()

# Check frontmatter exists
if ! head -1 "$FILE_PATH" | grep -q "^---$"; then
  ERRORS+=("Missing YAML frontmatter (file must start with ---)")
fi

# Check required frontmatter fields
for field in "name:" "description:" "user-invocable:" "allowed-tools:"; do
  if ! grep -q "^${field}" "$FILE_PATH"; then
    ERRORS+=("Missing required frontmatter field: $field")
  fi
done

# Check argument-hint if user-invocable
if grep -q "^user-invocable: true" "$FILE_PATH"; then
  if ! grep -q "^argument-hint:" "$FILE_PATH"; then
    ERRORS+=("user-invocable: true requires argument-hint field")
  fi
fi

# Check required body sections
for section in "## Core Principles" "## Patterns" "## Anti-patterns" "## Decision Guide"; do
  if ! grep -q "^${section}$" "$FILE_PATH"; then
    ERRORS+=("Missing required section: $section")
  fi
done

# Check trigger keywords in description block
# Extract lines between 'description:' and next top-level YAML key (line starting with a letter, not a space)
DESC_BLOCK=$(awk '/^description:/{found=1} found && /^[a-zA-Z]/ && !/^description:/{exit} found{print}' "$FILE_PATH")
QUOTED_WORDS=$(echo "$DESC_BLOCK" | grep -o '"[^"]*"' | wc -l | tr -d '[:space:]')
if [ "${QUOTED_WORDS:-0}" -lt 5 ]; then
  ERRORS+=("Fewer than 5 trigger keywords in description (found $QUOTED_WORDS quoted strings — need 5+)")
fi

# Report
if [ ${#ERRORS[@]} -gt 0 ]; then
  echo ""
  echo "⚠️  SKILL.md Validation: $FILE_PATH"
  for err in "${ERRORS[@]}"; do
    echo "   ✗ $err"
  done
  echo ""
  echo "   Run /skill-auditor for a full quality report."
  echo ""
  # Don't fail — warn only (exit 0 to not block the write)
  exit 0
fi

echo "✓ SKILL.md valid: $FILE_PATH"
exit 0
