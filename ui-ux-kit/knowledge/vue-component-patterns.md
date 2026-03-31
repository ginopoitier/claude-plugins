# Vue 3 Component Patterns for Design Systems

## The Non-Negotiables

Every design system component must have:
1. `<script setup lang="ts">` — no exceptions
2. Typed props via `defineProps<Interface>()`
3. Typed emits via `defineEmits<{ event: [arg] }>()`
4. `v-bind="$attrs"` on the root element (for class, style, aria passthrough)
5. Semantic HTML element as the root (not a bare `<div>` for interactive components)

---

## Props Pattern

```ts
// GOOD: typed interface + withDefaults
interface Props {
  variant?: 'primary' | 'secondary' | 'ghost' | 'destructive'
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

// BAD: PropType
const props = defineProps({
  variant: { type: String as PropType<'primary' | 'secondary'>, default: 'primary' }
})
```

---

## Emits Pattern

```ts
// GOOD: typed emits
const emit = defineEmits<{
  click: [event: MouseEvent]
  change: [value: string]
  'update:modelValue': [value: string]  // for v-model
}>()

// BAD: untyped
const emit = defineEmits(['click', 'change'])
```

---

## v-model Support

```ts
// Single v-model
const props = defineProps<{ modelValue: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

// Template: :value="props.modelValue" @input="emit('update:modelValue', $event.target.value)"

// Multiple v-model (Vue 3.4+)
const props = defineProps<{ title: string; content: string }>()
const emit = defineEmits<{
  'update:title': [value: string]
  'update:content': [value: string]
}>()
// Usage: <DsField v-model:title="title" v-model:content="content" />
```

---

## Slots Pattern

```vue
<template>
  <!-- Root element gets $attrs for class/aria/data-* passthrough -->
  <div v-bind="$attrs" :class="cardClasses">
    <!-- Optional named slots with fallback content -->
    <header v-if="$slots.header" class="card-header">
      <slot name="header" />
    </header>

    <div class="card-body">
      <slot />  <!-- default slot -->
    </div>

    <footer v-if="$slots.footer" class="card-footer">
      <slot name="footer" />
    </footer>
  </div>
</template>
```

### Slot Props (render props pattern)
```vue
<!-- DsList.vue — expose item data to consumer -->
<template>
  <ul>
    <li v-for="item in items" :key="item.id">
      <slot :item="item" :index="index" />
    </li>
  </ul>
</template>

<!-- Consumer -->
<DsList :items="users">
  <template #default="{ item }">
    <DsAvatar :name="item.name" />
    <span>{{ item.name }}</span>
  </template>
</DsList>
```

---

## Composables in Design Systems

Design system composables handle shared UI logic:

```ts
// useDisclosure.ts — open/close state for modals, dropdowns, tooltips
export function useDisclosure(initialState = false) {
  const isOpen = ref(initialState)
  const open = () => { isOpen.value = true }
  const close = () => { isOpen.value = false }
  const toggle = () => { isOpen.value = !isOpen.value }
  return { isOpen: readonly(isOpen), open, close, toggle }
}

// useFocusTrap.ts — trap focus inside modals
export function useFocusTrap(containerRef: Ref<HTMLElement | null>) {
  // ...implementation
}

// useId.ts — generate unique IDs for label/input association
let counter = 0
export function useId(prefix = 'ds') {
  return `${prefix}-${++counter}`
}
```

---

## Accessibility Composables

```ts
// useFloating.ts — for tooltips/dropdowns using @floating-ui/vue
import { useFloating, offset, flip, shift } from '@floating-ui/vue'

export function usePopover(referenceRef: Ref, floatingRef: Ref) {
  return useFloating(referenceRef, floatingRef, {
    placement: 'bottom-start',
    middleware: [offset(8), flip(), shift({ padding: 8 })],
  })
}
```

---

## Provide/Inject for Compound Components

Use for components that share state internally (e.g. RadioGroup → Radio):

```ts
// DsRadioGroup.vue
const RadioGroupKey = Symbol('RadioGroup')

interface RadioGroupContext {
  name: string
  modelValue: Ref<string>
  onChange: (value: string) => void
}

provide(RadioGroupKey, { name: props.name, modelValue, onChange })

// DsRadio.vue
const group = inject(RadioGroupKey)  // typed via injection key
```

---

## Component Testing Patterns

```ts
// DsButton.test.ts
import { mount } from '@vue/test-utils'
import DsButton from './DsButton.vue'

describe('DsButton', () => {
  it('renders with default variant', () => {
    const wrapper = mount(DsButton, { slots: { default: 'Click me' } })
    expect(wrapper.text()).toBe('Click me')
    expect(wrapper.classes()).toContain('bg-interactive')
  })

  it('emits click event', async () => {
    const wrapper = mount(DsButton)
    await wrapper.trigger('click')
    expect(wrapper.emitted('click')).toBeTruthy()
  })

  it('is disabled when disabled prop is true', () => {
    const wrapper = mount(DsButton, { props: { disabled: true } })
    expect(wrapper.attributes('disabled')).toBeDefined()
  })

  it('passes through aria attributes via $attrs', () => {
    const wrapper = mount(DsButton, { attrs: { 'aria-label': 'Submit form' } })
    expect(wrapper.attributes('aria-label')).toBe('Submit form')
  })
})
```

---

## Component Tiers Reference

| Tier | Rule | Examples |
|------|------|---------|
| Primitive | Single HTML element, no slots with complex content | DsButton, DsBadge, DsAvatar, DsIcon |
| Composite | Slots + multiple primitives + shared state | DsCard, DsModal, DsAlert, DsDropdown |
| Form | `v-model`, validation, label+input coupling | DsInput, DsSelect, DsCheckbox |
| Layout | Spacing/grid wrappers — no visual chrome | DsContainer, DsStack, DsGrid |
| Template | Full-page composition | DashboardLayout, AuthLayout |
