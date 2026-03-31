---
name: scaffold-component
description: >
  Create a typed Vue 3 + Tailwind component from a Figma node, description, or spec.
  Generates the SFC, optional Vitest test skeleton, and optional Storybook story.
  Load this skill when: "scaffold component", "create component", "new component",
  "build component", "generate component", "vue component", "figma to component",
  "component from figma", "button component", "card component", "input component".
user-invocable: true
argument-hint: "<ComponentName> [figma:<node-id>] [--with-tests] [--with-story]"
allowed-tools: [Read, Write, Edit, Bash, Grep, Glob]
---

# Scaffold Component

Generate a production-ready Vue 3 + Tailwind design system component.

## Steps

### 1. Determine source

Read `UI_UX_KIT: FIGMA_CONNECTOR=...` from hook-injected context (already present, no tool call needed):

- `FIGMA_CONNECTOR=available` + `figma:<node-id>` provided → call `mcp__claude_ai_Figma__figma_get_node`, pass result to `scaffold_component` (ux-ui-mcp)
- `FIGMA_CONNECTOR=unavailable` or no node ID → use description or spec path

> Figma is optional. All paths produce the same typed Vue SFC output.

### 2. Determine component tier

| Tier | When | Examples |
|------|------|---------|
| Primitive | Single element, no children | Button, Badge, Avatar, Spinner |
| Composite | Composes primitives | Card, Modal, FormField |
| Pattern | Has logic + composites | DataTable, CommandPalette |

### 3. Define props and variants

Ask or infer:
- Variants (e.g. `primary | secondary | ghost | destructive`)
- Sizes (e.g. `sm | md | lg`)
- States (e.g. `disabled`, `loading`, `error`)
- Slots (e.g. `default`, `leading-icon`, `trailing-icon`)

### 4. Generate component file

Path: `DESIGN_SYSTEM_PATH/components/{ComponentName}/{ComponentName}.vue`

```vue
<script setup lang="ts">
import { computed } from 'vue'
import { cva, type VariantProps } from 'class-variance-authority'

const variants = cva(
  // base classes
  'inline-flex items-center justify-center font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2',
  {
    variants: {
      variant: {
        primary: 'bg-[var(--color-semantic-primary)] text-white hover:bg-[var(--color-primary-700)]',
        secondary: 'border border-[var(--color-semantic-border)] bg-transparent hover:bg-[var(--color-neutral-50)]',
        ghost: 'bg-transparent hover:bg-[var(--color-neutral-100)]',
      },
      size: {
        sm: 'h-8 px-3 text-sm rounded-[var(--radius-sm)]',
        md: 'h-10 px-4 text-base rounded-[var(--radius-md)]',
        lg: 'h-12 px-6 text-lg rounded-[var(--radius-lg)]',
      },
    },
    defaultVariants: { variant: 'primary', size: 'md' },
  }
)

interface Props extends VariantProps<typeof variants> {
  disabled?: boolean
  loading?: boolean
  type?: 'button' | 'submit' | 'reset'
}

const props = withDefaults(defineProps<Props>(), {
  disabled: false,
  loading: false,
  type: 'button',
})

const emit = defineEmits<{
  click: [event: MouseEvent]
}>()

const classes = computed(() => variants({ variant: props.variant, size: props.size }))
</script>

<template>
  <button
    v-bind="$attrs"
    :type="props.type"
    :disabled="props.disabled || props.loading"
    :aria-busy="props.loading"
    :class="[classes, { 'opacity-50 cursor-not-allowed': props.disabled }]"
    @click="emit('click', $event)"
  >
    <slot name="leading-icon" />
    <slot />
    <slot name="trailing-icon" />
  </button>
</template>
```

### 5. Generate test skeleton (if `--with-tests`)

Path: `DESIGN_SYSTEM_PATH/components/{ComponentName}/{ComponentName}.test.ts`

### 6. Generate story (if `--with-story`)

Path: `DESIGN_SYSTEM_PATH/components/{ComponentName}/{ComponentName}.stories.ts`

### 7. Update barrel export

Add `export { default as {ComponentName} } from './{ComponentName}/{ComponentName}.vue'` to `DESIGN_SYSTEM_PATH/index.ts`

### 8. Run verification

- Check for `any` types
- Check for hardcoded colors
- Check for missing `aria-*` on interactive elements

## Execution

$ARGUMENTS

Determine source, define the component interface, generate all requested files, update the barrel, and run verification.
