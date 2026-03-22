#!/bin/bash
# pre-push-guard.sh
# pre-push hook: warns before pushing to protected branches
# Install: cp hooks/pre-push-guard.sh .git/hooks/pre-push && chmod +x .git/hooks/pre-push

REMOTE="$1"
CURRENT_BRANCH=$(git branch --show-current)

# Read protected branches from project config
CONFIG_FILE=".claude/git.config.md"
PROTECTED="main,master"
if [ -f "$CONFIG_FILE" ]; then
  PROJECT_PROTECTED=$(grep "^PROTECTED_BRANCHES=" "$CONFIG_FILE" | cut -d= -f2-)
  if [ -n "$PROJECT_PROTECTED" ]; then
    PROTECTED="$PROJECT_PROTECTED"
  fi
fi

# Check if current branch is protected
IFS=',' read -ra PROTECTED_LIST <<< "$PROTECTED"
for branch in "${PROTECTED_LIST[@]}"; do
  branch=$(echo "$branch" | tr -d '[:space:]')
  if [ "$CURRENT_BRANCH" = "$branch" ]; then
    echo ""
    echo "⚠️  Pushing directly to protected branch: $CURRENT_BRANCH"
    echo "   Remote: $REMOTE"
    echo ""
    read -p "   Are you sure? (yes/no): " CONFIRM
    if [ "$CONFIRM" != "yes" ]; then
      echo "   Push aborted."
      exit 1
    fi
    echo ""
  fi
done

exit 0
