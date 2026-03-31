# Tailwind CSS v4 — Patterns and Reference

## What Changed from v3

| Concern | v3 | v4 |
|---------|----|----|
| Config | `tailwind.config.js` | `@theme {}` in CSS |
| Custom colors | `theme.extend.colors` | `--color-*` in `@theme` |
| Custom spacing | `theme.extend.spacing` | `--spacing-*` in `@theme` |
| Plugin system | `plugins: [...]` | `@plugin` directive |
| Dark mode | `darkMode: 'class'` in config | `@variant dark (...)` or media |
| `@apply` | Widely used | Still works, but avoid |
| Content scan | `content: [...]` | Auto-detected |
| `theme()` function | `theme('colors.blue.500')` | `var(--color-blue-500)` |

---

## @theme Block

This is the v4 replacement for `tailwind.config.js` theme extension.

```css
@import "tailwindcss";

@theme {
  /* All custom tokens go here as CSS custom properties */
  --color-brand-500: oklch(55% 0.2 255);
  --font-family-sans: 'Inter', ui-sans-serif, system-ui;
  --font-size-sm: 0.875rem;
  --spacing-18: 4.5rem;
  --radius-card: 0.5rem;
}
```

Tailwind v4 generates utility classes from these:
- `--color-brand-500` → `bg-brand-500`, `text-brand-500`, `border-brand-500`
- `--spacing-18` → `p-18`, `m-18`, `gap-18`
- `--radius-card` → `rounded-card`

**Critical:** Only variables declared under `@theme` become utility-generating tokens.
Variables declared under `:root` are just CSS variables — no utility classes generated.

---

## Semantic Tokens in v4

Put semantic aliases inside `@theme` too — they generate utilities AND can be overridden for dark mode:

```css
@theme {
  /* Base */
  --color-neutral-100: #f5f5f5;
  --color-neutral-900: #171717;
  --color-primary-600: oklch(48% 0.22 255);

  /* Semantic — override these for dark mode */
  --color-surface: var(--color-neutral-100);
  --color-on-surface: var(--color-neutral-900);
  --color-interactive: var(--color-primary-600);
}

/* Dark mode override — class-based */
.dark {
  --color-surface: var(--color-neutral-900);
  --color-on-surface: var(--color-neutral-100);
}
```

This lets components use `bg-surface` and `text-on-surface` utilities that automatically adapt to dark mode.

---

## @layer in v4

```css
@layer base {
  /* Global element styles, font declarations */
  body {
    font-family: var(--font-family-sans);
    -webkit-font-smoothing: antialiased;
  }

  *, *::before, *::after {
    box-sizing: border-box;
  }
}

@layer components {
  /* Multi-property patterns only — NOT used for regular components */
  /* Only when 3+ classes are always used together with no variants */
  .prose { ... }
}

@layer utilities {
  /* Custom single-purpose utilities */
  .scrollbar-hide {
    scrollbar-width: none;
    &::-webkit-scrollbar { display: none; }
  }
}
```

---

## Variant Modifiers

```html
<!-- Responsive -->
<div class="block md:flex lg:grid">

<!-- Dark mode -->
<div class="bg-white dark:bg-neutral-900">

<!-- State -->
<button class="bg-primary hover:bg-primary-700 active:bg-primary-800 disabled:opacity-50">

<!-- Group (parent hover affects child) -->
<div class="group">
  <span class="opacity-0 group-hover:opacity-100">...</span>
</div>

<!-- Peer (sibling state) -->
<input class="peer" id="toggle" type="checkbox">
<div class="hidden peer-checked:block">Visible when checked</div>

<!-- aria-* variants -->
<div class="aria-expanded:rotate-180">
<div class="aria-disabled:opacity-50">

<!-- data-* variants -->
<div class="data-[state=open]:block">
```

---

## Class Variance Authority (cva) Pattern

cva is the recommended approach for design system component variants in v4:

```ts
import { cva, type VariantProps } from 'class-variance-authority'

const buttonVariants = cva(
  // base — always applied
  'inline-flex items-center justify-center gap-2 font-medium transition-colors disabled:pointer-events-none disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2',
  {
    variants: {
      variant: {
        primary:     'bg-interactive text-white hover:bg-primary-700 focus-visible:ring-primary-500',
        secondary:   'border border-border bg-transparent hover:bg-surface',
        ghost:       'bg-transparent hover:bg-neutral-100',
        destructive: 'bg-red-600 text-white hover:bg-red-700',
      },
      size: {
        sm:  'h-8 px-3 text-sm rounded-sm',
        md:  'h-10 px-4 text-base rounded-md',
        lg:  'h-12 px-6 text-lg rounded-lg',
        icon: 'h-10 w-10 rounded-md',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
    },
  }
)

type ButtonVariants = VariantProps<typeof buttonVariants>
```

---

## Anti-patterns in v4

```css
/* BAD: using @apply in component files */
.btn-primary {
  @apply bg-primary-600 text-white px-4 py-2;
}

/* GOOD: compose classes directly in template */
<button class="bg-interactive text-white px-4 py-2">

/* BAD: theme() function (v3 only) */
background: theme('colors.primary.600');

/* GOOD: CSS custom property */
background: var(--color-interactive);

/* BAD: using tailwind.config.js with v4 */
module.exports = { theme: { extend: { colors: { primary: '...' } } } }

/* GOOD: @theme in CSS */
@theme { --color-primary-600: oklch(48% 0.22 255); }
```

---

## File Structure for v4

```
src/
  assets/
    main.css            # @import "tailwindcss" + @theme + @layer base
  design-system/
    tokens/
      base.css          # @theme with primitive tokens
      semantic.css      # @theme with semantic aliases
    index.css           # @import base.css; @import semantic.css;
```

```css
/* main.css */
@import "tailwindcss";
@import "../design-system/index.css";

@layer base {
  body {
    font-family: var(--font-family-sans);
    background-color: var(--color-surface);
    color: var(--color-on-surface);
  }
}
```
