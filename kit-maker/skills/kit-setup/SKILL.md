---
name: kit-setup
description: >
  Interactive configuration wizard for setting up kit.config.md with user-specific
  values after installing a kit. Walks through each config key, explains its purpose,
  validates inputs, and optionally registers hooks in settings.json.
  Load this skill when: "kit setup", "configure kit", "setup wizard", "/kit-setup",
  "fill in config", "kit configuration", "after installing", "configure settings".
user-invocable: true
argument-hint: "[kit name, or blank for global kit config]"
allowed-tools: Read, Write, Edit, Bash, Glob
---

# Kit Setup

## Core Principles

1. **Ask before writing** — Never overwrite an existing config value without confirming. Show the current value and ask if the user wants to change it.
2. **Explain each field** — Users shouldn't need to read docs to understand what `OBSIDIAN_VAULT_PATH` means. Explain it inline during setup.
3. **Validate immediately** — When a user provides a path, check it exists. When they provide a URL, ping it. Don't let bad config values persist.
4. **Register hooks automatically** — After config is complete, offer to register the kit's hooks in `settings.json`. This is the step most users forget.
5. **End with a working test** — Close the wizard by running the first user-facing skill to confirm everything is wired correctly.

## Patterns

### Setup Wizard Flow

**Step 1 — Check current state**

```bash
# Check if config already exists
cat ~/.claude/kit.config.md 2>/dev/null || echo "No config found"

# Check which kit was just installed
ls ~/.claude/skills/ | grep -E "scaffold-|kit-"
```

**Step 2 — Read the config template**

```bash
# Find the template for the installed kit
cat ~/.claude/knowledge/{kit-name}/kit.config.template.md
# or
cat $KITS_BASE_PATH/{kit-name}/config/kit.config.template.md
```

**Step 3 — Walk through each key interactively**

For each `KEY=` line in the template:
1. Show the key name and its inline comment (the description)
2. Show the current value if one exists
3. Ask the user for the value
4. Validate the value
5. Write it

```markdown
Setting up Kit Maker configuration...

**KITS_BASE_PATH** — Where new kits are created on your filesystem.
Current value: (none)
Enter path (e.g. G:/Claude/Kits): _
```

**Step 4 — Validate each value**

| Value type | Validation |
|---|---|
| File/directory path | `ls` to check existence; offer to create if missing |
| URL | `curl -s -o /dev/null -w "%{http_code}"` — warn if not 200 |
| Username/handle | Accept any non-empty string |
| License identifier | Check against known SPDX identifiers |
| Optional field | Ask if they want to set it; skip if they say no |

```bash
# Path validation example
if [ -d "$VALUE" ]; then
  echo "  ✓ Path exists: $VALUE"
else
  echo "  ✗ Path not found: $VALUE"
  echo "  Create it? (y/n)"
fi
```

**Step 5 — Write the completed config**

```markdown
# Kit Maker — Configuration
## Kit Defaults
KITS_BASE_PATH=G:/Claude/Kits
KIT_AUTHOR=ginop
KIT_DEFAULT_LICENSE=MIT
## Marketplace Settings (optional)
MARKETPLACE_USERNAME=
MARKETPLACE_URL=
## Install Settings
CLAUDE_CONFIG_DIR=
```

**Step 6 — Register hooks (if the kit has hooks)**

```bash
# Check if hooks exist
ls ~/.claude/hooks/kit-maker/*.sh 2>/dev/null

# Read current settings.json
cat ~/.claude/settings.json
```

Show the user what hook entries will be added, confirm, then write to `settings.json`:

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Write|Edit",
        "hooks": [{"type": "command", "command": "~/.claude/hooks/kit-maker/validate-skill-frontmatter.sh"}]
      }
    ],
    "Stop": [
      {
        "hooks": [{"type": "command", "command": "~/.claude/hooks/kit-maker/auto-sync-skills-index.sh"}]
      }
    ]
  }
}
```

**Step 7 — Verify installation with a smoke test**

```markdown
Setup complete! Let's verify everything is working.

Running: /scaffold-skill test-skill

If the scaffold wizard launches, your installation is working correctly.
Press Ctrl+C to cancel the test scaffold.
```

### Reading and Merging config

When reading config values in a skill:

```bash
# Read a specific key from kit.config.md
CONFIG_FILE="$HOME/.claude/kit.config.md"
KITS_BASE_PATH=$(grep "^KITS_BASE_PATH=" "$CONFIG_FILE" | cut -d= -f2- | tr -d '[:space:]')

# Check if key is set
if [ -z "$KITS_BASE_PATH" ]; then
  echo "⚠️  KITS_BASE_PATH not configured. Run /kit-setup."
  exit 1
fi
```

## Anti-patterns

### Overwriting Without Asking

```markdown
# BAD — silently overwrites user's existing config
Write to ~/.claude/kit.config.md:
KITS_BASE_PATH=G:/Claude/Kits

# GOOD — check first, confirm before changing
Current KITS_BASE_PATH: C:/Users/user/kits
New value? (press Enter to keep current): _
```

### Skipping Validation

```markdown
# BAD — accepts any value without checking
Enter OBSIDIAN_VAULT_PATH: /nonexistent/path
✓ Config saved.   ← now every skill that uses this path will fail

# GOOD — validate immediately
Enter OBSIDIAN_VAULT_PATH: /nonexistent/path
✗ Path not found: /nonexistent/path
  Create directory? (y/n): y
  ✓ Created: /nonexistent/path
  ✓ Config saved.
```

### Not Registering Hooks

```markdown
# BAD — installation output says "edit settings.json manually"
# Most users never do this, so hooks never fire

# GOOD — setup wizard offers to register hooks automatically
Hooks found: validate-skill-frontmatter.sh, auto-sync-skills-index.sh

Register these hooks in ~/.claude/settings.json? (y/n): y
  ✓ PostToolUse hook registered: validate-skill-frontmatter.sh
  ✓ Stop hook registered: auto-sync-skills-index.sh
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| First-time install | Run full setup wizard: config + hooks |
| Re-configure one value | Ask which key, update just that key |
| Config file missing entirely | Copy from template, then run wizard |
| Hook already registered | Skip without duplicating the entry |
| Optional config field | Ask "do you want to configure X?" — default no |
| Path doesn't exist | Offer to create it (mkdir -p) |
| URL returns non-200 | Warn but don't block — services may be offline |
| settings.json missing | Create it with just the hooks block |
| settings.json already has hooks | Merge, don't overwrite the existing hooks |
