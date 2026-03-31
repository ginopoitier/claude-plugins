---
name: style-guide
description: >
  Generate a living style guide — a Vue page or HTML document that documents all design
  tokens, component variants, and usage patterns in the current design system.
  Load this skill when: "style guide", "design documentation", "component showcase",
  "storybook alternative", "token documentation", "design reference", "visual guide",
  "color palette", "typography specimens", "spacing guide", "living docs".
user-invocable: true
argument-hint: "[--format vue|html|markdown] [--sections colors,typography,spacing,components] [--output <path>]"
allowed-tools: [Read, Write, Edit, Bash, Glob, Grep]
---

# Style Guide Generator

Generate a living style guide that documents the entire design system.

## Steps

### 1. Discover existing tokens

Read `DESIGN_SYSTEM_PATH/tokens/` to find current token files.
If no tokens exist, prompt user to run `/design-tokens` first.

Extract:
- All color tokens (base + semantic)
- Typography tokens (sizes, weights, families)
- Spacing tokens
- Shadow tokens
- Border radius tokens

### 2. Discover existing components

Glob `DESIGN_SYSTEM_PATH/components/**/*.vue` to find all components.
For each component, read props interface from `<script setup>`.

### 3. Generate style guide

Default format: Vue SFC page (drop into any Vue project).

#### Section: Colors
```vue
<!-- Color palette grid -->
<div class="grid grid-cols-10 gap-2">
  <div
    v-for="swatch in colorSwatches"
    :key="swatch.variable"
    class="aspect-square rounded-md"
    :style="{ backgroundColor: `var(${swatch.variable})` }"
  >
    <span class="text-xs">{{ swatch.name }}</span>
  </div>
</div>
```

#### Section: Typography
Show each font size with a specimen: "The quick brown fox jumps over the lazy dog"

#### Section: Spacing
Visual ruler showing each spacing value as a colored bar with label

#### Section: Components
Render each component with all variant combinations, with props table below

### 4. Write output

Default path: `DESIGN_SYSTEM_PATH/StyleGuide.vue`
Or per `--output` flag.

Also generate a router entry suggestion if Vue Router is detected.

## Output Structure

```
StyleGuide.vue
  ├── <StyleGuideColors />     — color palette grid
  ├── <StyleGuideTypography /> — font specimens
  ├── <StyleGuideSpacing />    — spacing scale
  ├── <StyleGuideShadows />    — shadow examples
  └── <StyleGuideComponents /> — component showcase
```

## Execution

$ARGUMENTS

Discover tokens and components, then generate the style guide. Confirm the output path and ask if they want all sections or specific ones.
