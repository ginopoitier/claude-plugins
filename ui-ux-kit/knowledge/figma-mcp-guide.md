# Figma — Claude Connector Guide

## Overview

The kit uses the **Claude Figma connector** — an official integration built into Claude.ai, the same way Gmail and Google Calendar connectors work. No local server, no npm package, no API token in your config.

**Figma is optional.** All kit skills work without it. Figma unlocks:
- Extracting design tokens directly from a Figma file
- Inspecting component node properties (variants, sizing, colors)
- Drift detection (comparing Figma design to implemented code)

Without Figma, you create tokens from scratch and scaffold components from descriptions — same output quality, no external dependency.

---

## Setup (one-time, Claude.ai account)

1. Go to [claude.ai](https://claude.ai) → **Settings** → **Integrations**
2. Find **Figma** in the list and click **Connect**
3. A Figma OAuth window opens — authorize Claude to access your files (read-only)
4. Done. Restart Claude Code to pick up the new connector.

No token needed in `~/.claude/kit.config.md`. No entry needed in `settings.json`.

---

## Available Tools (when connector is enabled)

| Tool | What it returns |
|------|----------------|
| `mcp__claude_ai_Figma__figma_get_file` | Full file structure, pages, and metadata |
| `mcp__claude_ai_Figma__figma_get_node` | A specific node by ID (component, frame, group) |
| `mcp__claude_ai_Figma__figma_get_component_sets` | All component sets (variant groups) in a file |
| `mcp__claude_ai_Figma__figma_get_local_variables` | All local design variables/tokens (W3C-compatible) |
| `mcp__claude_ai_Figma__figma_get_published_variables` | Published library variables available to consumers |
| `mcp__claude_ai_Figma__figma_get_styles` | Color, text, and effect styles (legacy token system) |
| `mcp__claude_ai_Figma__figma_search_components` | Search components by name |

---

## Key Concepts

### File Key
Every Figma file has a unique key in its URL:
`https://www.figma.com/design/{FILE_KEY}/My-Design-System`

### Node IDs
Every element in Figma has a node ID (e.g. `1234:5678`).
Visible in the Figma URL when you select an element.
Used by `figma_get_node` to fetch a specific component.

### Variables vs Styles
- **Variables** — Figma's modern token system. Use `figma_get_local_variables`. Supports modes (light/dark).
- **Styles** — Legacy system (Colors, Text Styles, Effects). Use `figma_get_styles`. No modes.

Prefer Variables if the file uses them; fall back to Styles for older files.

### Variable Modes
Variable Collections can have multiple modes (e.g. Light/Dark):
- Light mode → maps to `:root` CSS variables
- Dark mode → maps to `.dark` class overrides or `@media (prefers-color-scheme: dark)`

---

## Figma Data → CSS Transformation

The connector fetches raw Figma data. The `ux-ui-mcp` server handles transformation:

```
Figma connector          ux-ui-mcp
─────────────────────    ──────────────────────────
figma_get_local_variables → generate_tailwind_theme → @theme {} CSS
figma_get_styles          → generate_css_variables  → :root {} CSS
figma_get_node            → scaffold_component      → .vue SFC
```

### Color format gotcha
Figma returns colors as `{ r, g, b, a }` floats (0–1 range), not hex or oklch.
`ux-ui-mcp` converts them: `r=0.149, g=0.384, b=0.820` → `oklch(55% 0.20 255)`.

### Variable name → CSS variable name
Figma variable names use slashes and spaces: `"Brand Colors/Primary/500"`
`ux-ui-mcp` transforms: `--color-brand-primary-500`

Transformation rules:
1. Lowercase everything
2. Replace `/` with `-`
3. Replace spaces with `-`
4. Prepend `--` + category prefix

---

## Common Gotchas

**Variables API empty** — file uses Styles, not Variables. Fall back to `figma_get_styles`.

**Node ID changed** — happens when Figma file is duplicated. Re-run sync to update `figma-registry.json`.

**Published vs local** — `figma_get_published_variables` only returns variables published to a library. During development, use `figma_get_local_variables` on the source file directly.

**Connector not showing in Claude.ai** — Figma connector requires a Professional or higher Claude plan. Check plan settings.

---

## Working Without Figma

When the connector isn't available (home machine, offline, no plan):

| Figma skill | Fallback |
|------------|---------|
| `/figma-sync` | Not available — requires connector |
| `/design-tokens figma:<key>` | Use `/design-tokens` without key → guided scratch creation |
| `/scaffold-component figma:<id>` | Use `/scaffold-component <Name>` with description |
| `/design-system figma:<key>` | Use `/design-system` without key → scratch mode |

All non-sync skills have scratch modes that produce identical output quality.
