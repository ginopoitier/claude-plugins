# Rule: Tailwind CSS v4 Patterns

## DO
- Define all design tokens in the `@theme {}` block in CSS — not in `tailwind.config.js`
- Use CSS custom properties (`var(--color-primary-500)`) for token values inside components
- Use `@layer base` for global resets and font-face declarations
- Use `@layer components` only for multi-property patterns that can't be composed inline
- Use `@layer utilities` for custom one-off utilities that extend Tailwind
- Compose Tailwind utilities inline in the template — keep class lists readable with line breaks
- Use `clsx` or `cva` (class-variance-authority) for conditional class composition
- Use the `group` and `peer` modifiers for parent/sibling-driven state styling

## DON'T
- Don't use `@apply` in component stylesheets — compose classes in the template instead
- Don't use arbitrary values (`mt-[13px]`) when a scale token exists — create the token
- Don't duplicate token values — reference `var(--color-primary-500)`, don't repeat `#2563eb`
- Don't use Tailwind v3 `tailwind.config.js` `theme.extend` with Tailwind v4 (use `@theme`)
- Don't use `!important` classes (`!mt-4`) except in utility overrides
- Don't import Tailwind utilities directly in `<style>` blocks — use `@import "tailwindcss"`

## Tailwind v4 Setup

```css
/* src/assets/main.css */
@import "tailwindcss";

@theme {
  /* Colors */
  --color-primary-50: oklch(97% 0.02 255);
  --color-primary-100: oklch(94% 0.04 255);
  --color-primary-500: oklch(55% 0.2 255);
  --color-primary-600: oklch(48% 0.22 255);
  --color-primary-900: oklch(25% 0.12 255);

  /* Neutral */
  --color-neutral-0: #ffffff;
  --color-neutral-50: #fafafa;
  --color-neutral-100: #f5f5f5;
  --color-neutral-900: #171717;
  --color-neutral-1000: #000000;

  /* Semantic aliases */
  --color-semantic-primary: var(--color-primary-600);
  --color-semantic-surface: var(--color-neutral-0);
  --color-semantic-on-surface: var(--color-neutral-900);
  --color-semantic-border: var(--color-neutral-200);
  --color-semantic-error: oklch(55% 0.25 25);

  /* Typography */
  --font-family-sans: 'Inter', ui-sans-serif, system-ui, sans-serif;
  --font-size-xs: 0.75rem;
  --font-size-sm: 0.875rem;
  --font-size-md: 1rem;
  --font-size-lg: 1.125rem;
  --font-size-xl: 1.25rem;
  --font-size-2xl: 1.5rem;

  /* Spacing (extends default scale) */
  --spacing-18: 4.5rem;
  --spacing-22: 5.5rem;

  /* Border radius */
  --radius-sm: 0.25rem;
  --radius-md: 0.375rem;
  --radius-lg: 0.5rem;
  --radius-xl: 0.75rem;
  --radius-full: 9999px;
}

@layer base {
  body {
    font-family: var(--font-family-sans);
    color: var(--color-semantic-on-surface);
    background-color: var(--color-semantic-surface);
  }
}
```

## Dark Mode

```css
@theme {
  /* Base tokens — same for both modes */
  --color-neutral-900: #171717;
}

/* Dark mode semantic overrides */
@media (prefers-color-scheme: dark) {
  :root {
    --color-semantic-surface: var(--color-neutral-900);
    --color-semantic-on-surface: var(--color-neutral-50);
    --color-semantic-border: var(--color-neutral-800);
  }
}
```

Or with class-based dark mode:
```css
.dark {
  --color-semantic-surface: var(--color-neutral-900);
}
```

## Class Composition Pattern

```vue
<!-- GOOD: readable multi-line class with cva variants -->
<script setup lang="ts">
import { cva } from 'class-variance-authority'

const buttonVariants = cva(
  'inline-flex items-center justify-center rounded font-medium transition-colors focus-visible:outline-none focus-visible:ring-2',
  {
    variants: {
      variant: {
        primary: 'bg-[var(--color-semantic-primary)] text-white hover:bg-[var(--color-primary-700)]',
        secondary: 'border border-[var(--color-semantic-border)] hover:bg-[var(--color-neutral-50)]',
      },
      size: {
        sm: 'px-3 py-1.5 text-sm',
        md: 'px-4 py-2 text-base',
        lg: 'px-6 py-3 text-lg',
      },
    },
    defaultVariants: { variant: 'primary', size: 'md' },
  }
)
</script>
```
