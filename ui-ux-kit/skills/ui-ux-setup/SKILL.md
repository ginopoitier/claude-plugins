---
name: ui-ux-setup
description: >
  Configure the ui-ux-kit: design system output path, component prefix, Tailwind version,
  and accessibility target. Also guides enabling the Claude Figma connector. Run this first.
  Load this skill when: "setup", "configure", "ui-ux-setup", "first time",
  "kit setup", "get started", "configure kit", "figma connector", "initialize".
user-invocable: true
argument-hint: "[--output-path <path>] [--prefix <prefix>]"
allowed-tools: [Read, Write, Edit, Bash]
---

# UI/UX Kit Setup

Configure the kit for the current machine and project.

## Steps

### 1. Check existing config

Read `~/.claude/kit.config.md`. If it exists, show current values and ask what to update.
If missing, create it from the template at `config/kit.config.template.md`.

### 2. Prompt for required values

Ask the user for (or accept via $ARGUMENTS):

| Field | Prompt | Validation |
|-------|--------|-----------|
| `DESIGN_SYSTEM_PATH` | "Where should the design system files go? (default: ./src/design-system)" | Valid path |
| `COMPONENT_PREFIX` | "Component prefix? (default: Ds, e.g. DsButton, DsCard)" | 1–4 uppercase letters |
| `TAILWIND_VERSION` | "Tailwind version in use? (3 or 4, default: 4)" | Must be 3 or 4 |

Optional:
- `A11Y_TARGET` — "Accessibility target? (WCAG_2_1_AA default)"

### 3. Write config file

Write the values to `~/.claude/kit.config.md`:

```markdown
# UI/UX Kit Config

## Design system output
DESIGN_SYSTEM_PATH=./src/design-system
COMPONENT_PREFIX=Ds

## Tailwind
TAILWIND_VERSION=4
TAILWIND_CSS_FILE=./src/assets/main.css

## Accessibility
A11Y_TARGET=WCAG_2_1_AA
```

### 4. Report Figma connector status

Read `UI_UX_KIT: FIGMA_CONNECTOR=...` from hook-injected context (already in scope, zero cost).

**If `FIGMA_CONNECTOR=available`:** confirm it's working.

**If `FIGMA_CONNECTOR=unavailable`:** guide the user to enable it:
```
To use Figma features:
1. Go to claude.ai → Settings → Integrations
2. Find Figma and click Connect
3. Authorize Claude to access your Figma files
4. Restart this Claude Code session

No API token or local server needed — Claude handles authentication.
```

### 5. Confirm setup

Output a summary:
```
✓ Config written to ~/.claude/kit.config.md
✓ Design system path: ./src/design-system
✓ Component prefix: Ds
✓ Tailwind v4 mode
✓ Accessibility target: WCAG 2.1 AA
✓ Figma connector: [connected / not yet enabled — see instructions above]

Ready to use:
  /design-tokens      — extract and sync design tokens from Figma
  /style-guide        — generate a living style guide
  /scaffold-component — create a new Vue component
  /design-system      — scaffold a full design system
```

## Execution

$ARGUMENTS

Run through the steps above. If all required fields are already set, confirm and offer to update individual values.
