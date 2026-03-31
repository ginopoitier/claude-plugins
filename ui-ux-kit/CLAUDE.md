# UI/UX Kit

> **Config:** @~/.claude/kit.config.md — run `/ui-ux-setup` if missing.

## Stack
- **Design:** Figma (via Claude Figma connector — **optional**) — tokens, component inspection, drift detection
- **Frontend:** Vue 3 Composition API · TypeScript · TailwindCSS v4
- **Design Tokens:** W3C Design Token format · CSS custom properties · Tailwind theme
- **MCP:** `ux-ui-mcp` — design token transformation, component analysis, style guide generation

> **Figma is optional.** All skills work without it — generate tokens from scratch, scaffold components from descriptions, build style guides from existing CSS. Figma unlocks extraction and sync; everything else is standalone.

## Always-Active Rules

@~/.claude/rules/ui-ux-kit/design-system-conventions.md
@~/.claude/rules/ui-ux-kit/component-patterns.md
@~/.claude/rules/ui-ux-kit/accessibility-standards.md
@~/.claude/rules/ui-ux-kit/tailwind-patterns.md

## Meta — Always Apply

@~/.claude/skills/context-discipline/SKILL.md
@~/.claude/skills/model-selection/SKILL.md

## Runtime Status (hook-injected, zero token cost)

`check-settings.sh` runs on every prompt and injects one line:
```
UI_UX_KIT: FIGMA_CONNECTOR=available|unavailable | UX_UI_MCP=available|unavailable
```

**Skills read this instead of probing tool availability.** No MCP call needed to check if Figma is present — the hook already answered that.

| Value | Meaning |
|-------|---------|
| `FIGMA_CONNECTOR=available` | Claude Figma connector is enabled; use Figma paths |
| `FIGMA_CONNECTOR=unavailable` | No connector; use scratch/description paths |
| `UX_UI_MCP=available` | Local ux-ui-mcp binary is built and ready |
| `UX_UI_MCP=unavailable` | ux-ui-mcp not built yet; tell user to run `npm run build` in `MCP/ux-ui-mcp/` |

## Config System

### User / Device Level — `~/.claude/kit.config.md`
Run `/ui-ux-setup` to configure:
- `DESIGN_SYSTEM_PATH` — default output path for generated design system files
- `COMPONENT_PREFIX` — prefix for generated components (e.g. `Ds`, `App`, `Ui`)

> **Figma auth** is handled by the Claude Figma connector — no API token needed.
> Enable it in Claude.ai Settings → Integrations → Figma.

When a skill needs config and `~/.claude/kit.config.md` is missing → tell user to run `/ui-ux-setup`.

## MCP Tools Available

### Claude Figma Connector (optional — enabled in Claude.ai Settings → Integrations)

| Tool | What it does |
|------|-------------|
| `mcp__claude_ai_Figma__figma_get_file` | Retrieve full file content, pages, and metadata |
| `mcp__claude_ai_Figma__figma_get_node` | Get a specific node (component, frame, group) by ID |
| `mcp__claude_ai_Figma__figma_get_component_sets` | List component sets (variant groups) in a file |
| `mcp__claude_ai_Figma__figma_get_local_variables` | Extract all local design variables/tokens |
| `mcp__claude_ai_Figma__figma_get_published_variables` | Get published library variables |
| `mcp__claude_ai_Figma__figma_search_components` | Search components by name across a file |
| `mcp__claude_ai_Figma__figma_get_styles` | Get color, text, and effect styles (legacy token system) |

### ux-ui-mcp (local, transforms Figma data → code)

| Tool | What it does |
|------|-------------|
| `generate_tailwind_theme` | Convert extracted tokens to a Tailwind v4 `@theme` block |
| `generate_css_variables` | Output tokens as CSS custom properties |
| `analyze_design_consistency` | Scan Vue components for token usage violations |
| `scaffold_component` | Generate a Vue SFC skeleton from a Figma component node |
| `generate_style_guide` | Build an HTML/Vue style guide page from current tokens |
| `audit_accessibility` | Check component markup against WCAG 2.1 AA rules |
| `detect_design_drift` | Compare Figma source to implemented components |

## Skills Available

### Design System
- `/design-system` — scaffold a complete design system (tokens + components + docs)
- `/design-tokens` — extract, transform, and sync design tokens
- `/style-guide` — generate a living style guide from the current token set
- `/figma-sync` — pull latest designs from Figma and reconcile with code

### Components & Templates
- `/scaffold-component` — create a typed Vue + Tailwind component from a spec or Figma node
- `/template-generator` — build page layout templates (landing page, dashboard, form page)
- `/integrate-design-system` — wire an existing design system into a Vue project

### Quality
- `/accessibility-audit` — WCAG 2.1 AA audit of Vue components
- `/design-audit` — consistency review: token usage, spacing, color violations

### Setup
- `/ui-ux-setup` — configure kit settings, Figma token, paths

### Meta (auto-active, not user-invoked)
- `instinct-system` — learns project-specific design patterns
- `self-correction-loop` — captures corrections → permanent MEMORY.md rules
- `autonomous-loops` — bounded iteration for multi-component generation
- `learning-log` — captures design decisions and gotchas per session
- `convention-learner` — detects project naming and structure conventions
- `verification-loop` — validates generated components against the design spec

## Key Patterns

### Design Token Naming
Tokens follow a three-tier hierarchy: `{category}.{scale}.{modifier}`
- Colors: `color.primary.500`, `color.neutral.100`, `color.semantic.error`
- Typography: `font.size.md`, `font.weight.semibold`, `font.family.sans`
- Spacing: `space.4`, `space.8`, `space.16`

### Component Structure
All generated components use `<script setup lang="ts">`. Never use Options API.
Tailwind classes reference design tokens via `var(--color-primary-500)` or `theme()`.

### Tailwind v4 Config
Design tokens live in `app.css` under `@theme {}`, not in `tailwind.config.js`.
Use CSS custom properties as the single source of truth.

### Figma → Code Workflow
1. Call `figma_get_local_variables` via Claude Figma connector to extract tokens
2. Pass tokens to `generate_tailwind_theme` → writes `@theme {}` block
3. Call `figma_get_node` for each component → pass to `scaffold_component`
4. Run `/accessibility-audit` on generated components
5. Run `/design-audit` to check consistency
