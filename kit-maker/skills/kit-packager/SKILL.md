---
name: kit-packager
description: >
  Package a Claude Code kit for distribution and marketplace publishing.
  Generates kit.manifest.json, install.sh script, README.md, and validates
  the kit is installable by someone who has never seen it.
  Load this skill when: "publish kit", "package kit", "distribute kit",
  "share kit", "kit manifest", "install script", "kit release", "kit for others",
  "marketplace", "kit version".
user-invocable: true
argument-hint: "[kit path and version number]"
allowed-tools: Read, Write, Edit, Bash, Glob
---

# Kit Packager

## Core Principles

1. **Installability is the only metric that matters** — A beautiful kit that can't be installed by a new user is worthless. Test the install path, not just the content.
2. **Manifest-first distribution** — Everything a user needs to install the kit comes from `kit.manifest.json`. No manual steps that aren't in the manifest.
3. **Semantic versioning** — `MAJOR.MINOR.PATCH`. Breaking changes to CLAUDE.md structure or skill APIs = major. New skills/rules = minor. Fixes = patch.
4. **README is for humans, manifest is for machines** — README explains what the kit does and why. Manifest defines how to install it.

## Patterns

### Pre-Package Checklist

Before generating distribution files, verify:

```bash
# 1. Kit health check passes (GPA ≥ 3.0)
# 2. All SKILL.md files have complete frontmatter
# 3. All @-references in CLAUDE.md point to existing files
# 4. All skill directories referenced in CLAUDE.md actually exist
# 5. No hardcoded absolute paths (no C:/, D:/, /home/user/)
# 6. Config template has no user-specific values
```

### Generate kit.manifest.json

```json
{
  "id": "{kit-name}",
  "name": "{Kit Display Name}",
  "version": "{MAJOR.MINOR.PATCH}",
  "description": "{One sentence: what this kit does and who it helps}",
  "author": "{author-name-or-org}",
  "homepage": "{optional URL}",
  "tags": ["{domain}", "{technology}", "{use-case}"],
  "requires": [],
  "install": {
    "claude_md": "~/.claude/CLAUDE.md",
    "rules": "~/.claude/rules/{kit-name}/",
    "skills": "~/.claude/skills/",
    "knowledge": "~/.claude/knowledge/{kit-name}/",
    "agents": "~/.claude/agents/",
    "hooks": "~/.claude/hooks/{kit-name}/"
  },
  "config": "config/kit.config.template.md",
  "entrypoint": "CLAUDE.md",
  "commands": [],
  "mcp": null,
  "min_claude_code_version": "1.0.0"
}
```

### Generate install.sh

```bash
#!/bin/bash
# install.sh — Kit Installer for {Kit Name}
# Usage: bash install.sh [--dry-run]

set -e
KIT_DIR="$(cd "$(dirname "$0")" && pwd)"
CLAUDE_DIR="${CLAUDE_CONFIG_DIR:-$HOME/.claude}"
DRY_RUN="${1:-}"

echo "Installing {Kit Name} to $CLAUDE_DIR..."

install_dir() {
  local src="$1" dest="$2"
  if [ "$DRY_RUN" = "--dry-run" ]; then
    echo "[DRY RUN] Would copy: $src → $dest"
  else
    mkdir -p "$dest"
    cp -r "$src/." "$dest/"
    echo "✓ $dest"
  fi
}

# Install rules
install_dir "$KIT_DIR/rules" "$CLAUDE_DIR/rules/{kit-name}"

# Install skills (flat — skills share namespace)
for skill_dir in "$KIT_DIR/skills"/*/; do
  skill_name=$(basename "$skill_dir")
  install_dir "$skill_dir" "$CLAUDE_DIR/skills/$skill_name"
done

# Install knowledge
install_dir "$KIT_DIR/knowledge" "$CLAUDE_DIR/knowledge/{kit-name}"

# Install agents
install_dir "$KIT_DIR/agents" "$CLAUDE_DIR/agents"

# Install hooks (make executable)
if [ -d "$KIT_DIR/hooks" ]; then
  install_dir "$KIT_DIR/hooks" "$CLAUDE_DIR/hooks/{kit-name}"
  chmod +x "$CLAUDE_DIR/hooks/{kit-name}"/*.sh 2>/dev/null || true
fi

# Merge CLAUDE.md entry
ENTRY="@~/.claude/rules/{kit-name}/*.md"
if ! grep -q "{kit-name}" "$CLAUDE_DIR/CLAUDE.md" 2>/dev/null; then
  echo "" >> "$CLAUDE_DIR/CLAUDE.md"
  echo "## {Kit Name} — Installed" >> "$CLAUDE_DIR/CLAUDE.md"
  echo "" >> "$CLAUDE_DIR/CLAUDE.md"
  echo "@~/.claude/rules/{kit-name}/*.md" >> "$CLAUDE_DIR/CLAUDE.md"
fi

echo ""
echo "✅ {Kit Name} installed successfully!"
echo "   Run /kit-setup to configure your personal settings."
```

### Generate README.md

```markdown
# {Kit Name}

> {One sentence description}

## What This Kit Does

{2-3 sentences explaining the problem it solves and who it's for}

## Skills

| Command | Description |
|---------|-------------|
{list of user-invocable skills from CLAUDE.md}

## Install

```bash
git clone {repo-url}
cd {kit-name}
bash install.sh
```

Then restart Claude Code.

## Configure

After installing, run `/kit-setup` to set your personal configuration.

## Requirements
- Claude Code {min_version}+
{any other requirements}
```

## Anti-patterns

### Don't Ship Without Running kit-health-check

```
# BAD — "looks good to me, let's publish"
→ Users install kit → skills don't load → trigger keywords wrong → bad experience

# GOOD — gate on health check
Run /kit-health-check → GPA ≥ 3.0 → then package
Any D/F grades = fix first
```

### Don't Hardcode Absolute Paths

```bash
# BAD — breaks on any machine except the author's
cp rules/ /Users/ginop/.claude/rules/

# GOOD — use $CLAUDE_DIR
cp -r rules/ "$CLAUDE_DIR/rules/{kit-name}/"
```

### Don't Skip the Config Template

```
# BAD — user installs kit, it references non-existent config
@~/.claude/kit.config.md  # file doesn't exist → every session errors

# GOOD — ship a template they can fill in
config/kit.config.template.md → user runs /kit-setup → saves to ~/.claude/kit.config.md
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| First release | Full package: manifest + install.sh + README |
| Minor update (new skill) | Update manifest version (patch), update README skills table |
| Breaking change (renamed skills) | Bump major version, add migration notes to README |
| Internal team distribution | install.sh + README, no marketplace metadata needed |
| Public marketplace release | Full package + homepage URL + tags + author field |
| Kit with MCP server | Add mcp field to manifest with server path and env vars |

## Deep Reference

For full marketplace spec, manifest schema, and distribution guidelines:
@~/.claude/knowledge/kit-maker/marketplace-spec.md
