# UX/UI MCP Server — Requirements

This document defines the tools, inputs, and outputs required for the `ux-ui-mcp` Node.js MCP server that powers the `ui-ux-kit`.

## Overview

`ux-ui-mcp` is a **transformation and analysis server**. It does NOT call the Figma API directly.

Figma data is fetched by the **Claude Figma connector** (enabled in Claude.ai Settings → Integrations) and passed as input to `ux-ui-mcp` tools. This means:
- No Figma API token in this server
- No direct HTTP calls to Figma
- Works fully offline for non-Figma workflows (scratch token creation, component scaffolding from description, a11y audits, design consistency checks)

```
Claude Figma connector  →  raw Figma data
                              ↓
ux-ui-mcp tools         →  CSS / Vue / JSON output
```

**Runtime:** Node.js / TypeScript
**Transport:** stdio (same pattern as `vue-mcp`)
**Entry point:** `dist/index.js`
**Package name:** `@ginopoitier/ux-ui-mcp`

---

## Tool Definitions

### 1. `generate_tailwind_theme`

Convert a set of design tokens to a Tailwind v4 `@theme {}` block.

Receives tokens either from the Figma connector (passed by Claude) or from scratch input.

**Input:**
```ts
{
  tokens: DesignToken[];
  tailwindVersion: 3 | 4;
  outputFormat: 'css-theme' | 'js-config';  // v4 = css-theme, v3 = js-config
  prefix?: string;                           // Optional CSS var prefix (default: empty)
}
```

**Output:**
```ts
{
  content: string;           // The generated @theme {} or tailwind.config.js content
  variableCount: number;
  warnings: string[];        // e.g. name collisions, unsupported token types
}
```

**Implementation notes:**
- Tailwind v4: output CSS custom properties inside `@theme { --color-primary-500: #...; }`
- Tailwind v3: output `theme.extend` object with color/spacing/font keys
- Sanitize token names to valid CSS identifiers (replace `.` with `-`)
- Figma colors arrive as `{ r, g, b, a }` floats (0–1) — convert to oklch using `culori`

---

### 2. `generate_css_variables`

Output design tokens as raw CSS custom properties (framework-agnostic).

**Input:**
```ts
{
  tokens: DesignToken[];
  selector?: string;         // Default: ':root'
  modes?: string[];          // e.g. ['light', 'dark'] for media query variants
}
```

**Output:**
```ts
{
  content: string;           // CSS file content
  variableCount: number;
}
```

---

### 3. `scaffold_component`

Generate a Vue 3 SFC skeleton from a Figma node object (passed from connector) or a text description.

**Input:**
```ts
{
  source: 'figma-node' | 'description';
  // When source = 'figma-node': pass the raw node data from figma_get_node
  figmaNode?: FigmaNode;     // Raw node object from Claude Figma connector
  description?: string;      // Required when source = 'description'
  componentName: string;     // PascalCase, e.g. 'DsButton'
  propsSchema?: Record<string, string>;  // Optional explicit props
  useTokens?: boolean;       // Emit Tailwind classes referencing design tokens
}
```

**Output:**
```ts
{
  vueFileContent: string;    // Complete .vue SFC content
  storyContent?: string;     // Optional Storybook story
  testContent?: string;      // Optional Vitest test skeleton
}
```

**Generated SFC structure:**
```vue
<script setup lang="ts">
// typed props with cva variants, typed emits
</script>

<template>
  <!-- Semantic HTML root, v-bind="$attrs", Tailwind classes via design token vars -->
</template>
```

---

### 4. `generate_style_guide`

Build an HTML or Vue page documenting the current design system.
Works entirely from token data — no Figma connection needed.

**Input:**
```ts
{
  tokens: DesignToken[];
  sections: Array<'colors' | 'typography' | 'spacing' | 'shadows' | 'components'>;
  format: 'vue-page' | 'html' | 'markdown';
  title?: string;
}
```

**Output:**
```ts
{
  content: string;           // Generated page content
  assetCount: number;
}
```

---

### 5. `analyze_design_consistency`

Scan a Vue project for design token violations: hardcoded colors, arbitrary Tailwind values, missing token references.
Works entirely from local files — no Figma connection needed.

**Input:**
```ts
{
  projectPath: string;
  tokensPath: string;        // Path to tokens JSON or CSS vars file
  rules?: {
    noHardcodedColors?: boolean;    // Default: true
    noArbitraryValues?: boolean;    // Default: true
    requireTokenPrefix?: string;    // e.g. 'var(--color-'
  };
}
```

**Output:**
```ts
{
  violations: Array<{
    file: string;
    line: number;
    type: 'hardcoded-color' | 'arbitrary-value' | 'missing-token';
    value: string;
    suggestion?: string;
  }>;
  summary: {
    totalFiles: number;
    violatingFiles: number;
    totalViolations: number;
  };
}
```

---

### 6. `audit_accessibility`

Check Vue component templates against WCAG 2.1 AA rules.
Works entirely from local files — no Figma connection needed.

**Input:**
```ts
{
  componentPath: string;     // Path to .vue file or directory
  standard: 'WCAG_2_1_A' | 'WCAG_2_1_AA' | 'WCAG_2_2_AA';
  rules?: string[];          // Specific rule IDs to check (default: all)
}
```

**Output:**
```ts
{
  issues: Array<{
    rule: string;            // e.g. 'missing-alt', 'insufficient-contrast', 'missing-label'
    severity: 'error' | 'warning';
    component: string;
    line: number;
    description: string;
    wcagCriteria: string;    // e.g. '1.1.1 Non-text Content'
    fix: string;             // Suggested fix
  }>;
  passedRules: string[];
  score: number;             // 0–100
}
```

**Implementation notes:**
- Parse Vue SFC `<template>` with `@vue/compiler-sfc`
- Check for: missing `alt`, missing ARIA labels, insufficient color contrast, missing form labels, missing `role`, keyboard traps, focus management issues
- Use WCAG 2.1 Level AA as default standard

---

### 7. `detect_design_drift`

Compare a Figma node object (passed from connector) to an implemented Vue component.
Requires Figma connector to supply the node data, but the comparison logic is local.

**Input:**
```ts
{
  figmaNode: FigmaNode;      // Raw node data from mcp__claude_ai_Figma__figma_get_node
  componentPath: string;
  checkAspects?: Array<'colors' | 'typography' | 'spacing' | 'structure'>;
}
```

**Output:**
```ts
{
  drifts: Array<{
    aspect: string;
    figmaValue: string;
    implementedValue: string;
    severity: 'critical' | 'minor';
    location: string;
  }>;
  driftScore: number;        // 0 = perfect match, 100 = completely different
  summary: string;
}
```

---

## Data Types

```ts
// W3C Design Token (simplified)
interface DesignToken {
  name: string;              // e.g. 'color.primary.500'
  $type: 'color' | 'dimension' | 'fontFamily' | 'fontWeight' | 'number' | 'shadow' | 'border';
  $value: string | number | object;
  $description?: string;
  group?: string;            // e.g. 'color', 'typography'
  mode?: string;             // e.g. 'light', 'dark'
}

// Figma node (as returned by Claude connector's figma_get_node)
type FigmaNode = Record<string, unknown>  // pass-through from connector, don't redefine
```

---

## Dependencies

```json
{
  "dependencies": {
    "@modelcontextprotocol/sdk": "^1.0.0",
    "@vue/compiler-sfc": "^3.5.0",
    "culori": "^4.0.0",
    "glob": "^11.0.0",
    "axe-core": "^4.10.0"
  },
  "devDependencies": {
    "@types/node": "^22.0.0",
    "typescript": "^5.7.0"
  }
}
```

Note: `node-fetch` removed — no direct Figma API calls. `culori` added for oklch color conversion.

---

## CLI Interface

```bash
# Run against a project directory
node dist/index.js --project /path/to/vue-project
```

No `--figma-token` flag needed. Figma data is passed by Claude via tool inputs, not fetched here.

---

## Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `PROJECT_ROOT` | No | Overrides `--project` CLI arg |

No `FIGMA_ACCESS_TOKEN` — authentication is handled by the Claude Figma connector at the platform level.

---

## Architecture Notes

- Follow the same stdio MCP transport pattern as `MCP/Vue/vue-mcp/`
- All tools register via `@modelcontextprotocol/sdk` `McpServer.tool()`
- **No Figma API client** — Figma data arrives as plain JSON input from Claude connector
- Token parsing is separated from transformation — `parsers/` → `transformers/`
- Accessibility audit wraps `axe-core` running against parsed HTML strings
- All logging goes to `stderr`; `stdout` is reserved for MCP JSON-RPC

## Tool Availability Without Figma

| Tool | Needs Figma data? | Works offline/no connector? |
|------|------------------|-----------------------------|
| `generate_tailwind_theme` | Optional input | Yes — accepts scratch tokens |
| `generate_css_variables` | Optional input | Yes — accepts scratch tokens |
| `scaffold_component` | Optional input | Yes — `description` source |
| `generate_style_guide` | No | Yes |
| `analyze_design_consistency` | No | Yes |
| `audit_accessibility` | No | Yes |
| `detect_design_drift` | Yes (node required) | No — requires connector data |
