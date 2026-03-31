---
name: design-tokens
description: >
  Extract, transform, sync, and manage design tokens. Pull from Figma, convert to
  Tailwind v4 @theme or CSS variables, and keep code in sync with Figma source.
  Load this skill when: "design tokens", "extract tokens", "figma tokens", "sync tokens",
  "tailwind theme", "css variables", "token sync", "color tokens", "spacing tokens",
  "typography tokens", "token extraction", "figma variables".
user-invocable: true
argument-hint: "[figma:<file-key>] [--output css|json|tailwind] [--mode light|dark|all]"
allowed-tools: [Read, Write, Edit, Bash, Grep]
---

# Design Tokens

Extract, transform, and sync design tokens between Figma and code.

## Workflow

### Step 0: Read hook-injected status

Read `UI_UX_KIT: FIGMA_CONNECTOR=...` from the injected context (set by `check-settings.sh` before this prompt — no tool call needed).

- `FIGMA_CONNECTOR=available` + file key provided → use Option A
- `FIGMA_CONNECTOR=unavailable` or no file key → use Option B

---

### Option A: Extract from Figma

When `FIGMA_CONNECTOR=available` and user provides a file key or URL:

1. **Parse the file key** from URL if needed: `https://www.figma.com/design/{KEY}/...`
2. **Fetch variables** via `mcp__claude_ai_Figma__figma_get_local_variables`
3. **Fetch styles** via `mcp__claude_ai_Figma__figma_get_styles` (fallback for older files)
4. **Pass to ux-ui-mcp** `generate_tailwind_theme` to transform to CSS/JSON
5. **Preview tokens** — show summary table before writing
6. **Generate outputs** based on `--output` flag:
   - `tailwind` (default) → `@theme {}` block
   - `css` → CSS custom properties
   - `json` → W3C Design Token JSON file

### Option B: Create from scratch

When user has no Figma file, guide them through defining tokens:

1. **Base color palette** — ask for primary color + derive scale using oklch lightness steps
2. **Semantic aliases** — map primary/error/warning/success/surface/on-surface
3. **Typography** — ask for font family + define size scale (xs → 4xl)
4. **Spacing** — confirm use of Tailwind default 4px base scale or custom
5. **Radius** — sm/md/lg/full

### Output File Structure

```
DESIGN_SYSTEM_PATH/
  tokens/
    base.css          # @theme {} with raw values
    semantic.css      # @theme {} with aliases
    tokens.json       # W3C format (source of truth for tooling)
```

### Tailwind v4 Output Example

```css
/* src/design-system/tokens/base.css */
@theme {
  --color-primary-50: oklch(97% 0.02 255);
  --color-primary-500: oklch(55% 0.2 255);
  --color-primary-900: oklch(25% 0.12 255);
  --color-neutral-0: #ffffff;
  --color-neutral-100: #f5f5f5;
  --color-neutral-900: #171717;
  --font-family-sans: 'Inter', ui-sans-serif, system-ui;
  --font-size-sm: 0.875rem;
  --font-size-md: 1rem;
  --font-size-lg: 1.125rem;
}

/* src/design-system/tokens/semantic.css */
@theme {
  --color-semantic-primary: var(--color-primary-500);
  --color-semantic-surface: var(--color-neutral-0);
  --color-semantic-on-surface: var(--color-neutral-900);
  --color-semantic-error: oklch(55% 0.25 25);
  --color-semantic-border: var(--color-neutral-200);
}
```

### Sync Workflow (keep Figma → code in sync)

When running `/design-tokens figma:<key>` on an existing project:
1. Extract current tokens from Figma
2. Compare to existing `tokens.json`
3. Report additions, changes, removals
4. Ask for confirmation before writing changes
5. Update CSS files and JSON

## Execution

$ARGUMENTS

Follow the workflow for the appropriate option. Output a token summary table, write files, and confirm with the user.
