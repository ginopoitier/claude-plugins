#!/bin/bash
# auto-sync-skills-index.sh
# Stop hook: runs when Claude finishes responding
# Checks if new skills were added but not listed in CLAUDE.md
# Warns the user (does not auto-edit CLAUDE.md — that's too risky without context)
#
# Registered in settings.json Stop hook

set -e

# Find the kit being worked on (current directory)
KIT_DIR="${PWD}"

# Only run if this looks like a kit directory
if [ ! -f "$KIT_DIR/CLAUDE.md" ] || [ ! -d "$KIT_DIR/skills" ]; then
  exit 0
fi

CLAUDE_MD="$KIT_DIR/CLAUDE.md"
SKILLS_DIR="$KIT_DIR/skills"

# Get all skill directory names
SKILL_DIRS=()
while IFS= read -r -d '' skill_dir; do
  SKILL_DIRS+=("$(basename "$skill_dir")")
done < <(find "$SKILLS_DIR" -mindepth 1 -maxdepth 1 -type d -print0 2>/dev/null)

# Check which skills are not mentioned in CLAUDE.md
MISSING=()
for skill in "${SKILL_DIRS[@]}"; do
  if ! grep -q "$skill" "$CLAUDE_MD"; then
    MISSING+=("$skill")
  fi
done

# Report missing skills
if [ ${#MISSING[@]} -gt 0 ]; then
  echo ""
  echo "📋 Skills Index Sync Check"
  echo "   The following skills exist in skills/ but aren't listed in CLAUDE.md:"
  for skill in "${MISSING[@]}"; do
    echo "   - /$skill"
  done
  echo ""
  echo "   Run /scaffold-skill to add them, or update CLAUDE.md manually."
  echo ""
fi

exit 0
