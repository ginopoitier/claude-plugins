# Rule: Vue Component Patterns for Design Systems

## DO
- Always use `<script setup lang="ts">` — never Options API or class components
- Define props with TypeScript interfaces, never inline objects with `PropType<>`
- Use `defineProps` with `withDefaults` for optional props
- Expose slots for content projection — prefer slots over props for complex content
- Use `defineEmits` with explicit event types — never `$emit` untyped
- Use `v-bind="$attrs"` on the root element so consumers can pass HTML attributes
- Prefix internal composables with `use` and keep them next to the component
- Use `<slot>` names that describe intent: `default`, `leading-icon`, `trailing-icon`, `label`

## DON'T
- Don't reach into parent or sibling component state — use props/emits or a store
- Don't use `any` in component props or emits
- Don't mix layout concerns into a primitive component (button shouldn't control its own margin)
- Don't use `scoped` styles in design system components — Tailwind handles styling
- Don't use Options API `mounted`, `data`, `methods` — use Composition API equivalents
- Don't put business logic in design system components — they are pure UI
- Don't import application stores or API clients from design system components

## Prop Patterns

```vue
<!-- GOOD: typed props with defaults -->
<script setup lang="ts">
interface Props {
  variant?: 'primary' | 'secondary' | 'ghost'
  size?: 'sm' | 'md' | 'lg'
  disabled?: boolean
  loading?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'primary',
  size: 'md',
  disabled: false,
  loading: false,
})

const emit = defineEmits<{
  click: [event: MouseEvent]
}>()
</script>
```

## Slot Pattern

```vue
<!-- GOOD: named slots for flexible composition -->
<template>
  <button v-bind="$attrs" :class="buttonClasses" @click="emit('click', $event)">
    <slot name="leading-icon" />
    <slot />
    <slot name="trailing-icon" />
  </button>
</template>
```

## Tailwind Class Composition

```vue
<!-- GOOD: computed classes using cva or clsx -->
<script setup lang="ts">
import { computed } from 'vue'

const buttonClasses = computed(() => [
  'inline-flex items-center justify-center font-medium transition-colors',
  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2',
  {
    'bg-[var(--color-semantic-primary)] text-white hover:bg-[var(--color-blue-700)]': props.variant === 'primary',
    'border border-[var(--color-semantic-border)] hover:bg-[var(--color-neutral-50)]': props.variant === 'secondary',
    'hover:bg-[var(--color-neutral-100)]': props.variant === 'ghost',
    'px-3 py-1.5 text-sm': props.size === 'sm',
    'px-4 py-2 text-base': props.size === 'md',
    'px-6 py-3 text-lg': props.size === 'lg',
    'opacity-50 cursor-not-allowed': props.disabled,
  }
])
</script>
```

## Component Size Tiers

| Tier | Examples | Rule |
|------|---------|------|
| Primitive | Button, Badge, Icon | No children — fully self-contained |
| Composite | Card, Modal, Form | Composes primitives; accepts slots |
| Pattern | DataTable, CommandPalette | Composes composites; has logic |
| Template | PageLayout, AuthLayout | Full-page compositions |
