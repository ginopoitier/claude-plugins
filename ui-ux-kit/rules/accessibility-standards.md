# Rule: Accessibility Standards (WCAG 2.1 AA)

## DO
- Provide `alt` text on every `<img>` — decorative images use `alt=""`
- Use semantic HTML first: `<button>`, `<nav>`, `<main>`, `<section>`, `<article>` over `<div>`
- Every form input must have an associated `<label>` (via `for`/`id` or wrapping)
- Ensure color contrast ratio ≥ 4.5:1 for normal text, ≥ 3:1 for large text (18pt or 14pt bold)
- All interactive elements must be keyboard reachable and activatable with Enter/Space
- Use `aria-label` or `aria-labelledby` when visible text label is absent
- Use `role="dialog"` + `aria-modal="true"` + focus trap for modals
- Announce dynamic content changes with `aria-live="polite"` (non-critical) or `aria-live="assertive"` (errors)
- Respect `prefers-reduced-motion` — skip animations when enabled
- Use `focus-visible` Tailwind utility (not `focus`) to show focus ring only on keyboard navigation

## DON'T
- Don't use `outline: none` or `focus:outline-none` without a visible focus alternative
- Don't use color alone to convey information — pair with icon, label, or pattern
- Don't rely on placeholder text as a substitute for a label
- Don't use `tabindex` values greater than 0 — it breaks natural tab order
- Don't make hover-only interactions — touch and keyboard users cannot hover
- Don't auto-play audio or video — provide controls
- Don't use `aria-hidden="true"` on focusable elements
- Don't use `<div>` or `<span>` with click handlers without `role` and keyboard support

## Required ARIA Patterns

```vue
<!-- Modal / Dialog -->
<div role="dialog" aria-modal="true" aria-labelledby="modal-title" aria-describedby="modal-desc">
  <h2 id="modal-title">Confirm Delete</h2>
  <p id="modal-desc">This action cannot be undone.</p>
</div>

<!-- Icon-only button -->
<button aria-label="Close dialog">
  <XIcon aria-hidden="true" />
</button>

<!-- Status message -->
<div aria-live="polite" aria-atomic="true">
  <!-- Inject success/error messages here dynamically -->
</div>

<!-- Form field with error -->
<label for="email">Email</label>
<input id="email" type="email" aria-describedby="email-error" aria-invalid="true" />
<span id="email-error" role="alert">Please enter a valid email address.</span>
```

## Color Contrast Requirements

| Text Type | Minimum Ratio | Tailwind Guidance |
|-----------|--------------|-------------------|
| Normal body text | 4.5:1 | `text-neutral-900` on `bg-white` |
| Large text (≥18pt) | 3:1 | |
| UI components / graphics | 3:1 | Borders, icons, focus rings |
| Decorative | None | |

Always verify token combinations with a contrast checker when defining `color.semantic.*` pairs.

## Reduced Motion

```css
/* In design system CSS */
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

Or in Tailwind: `motion-safe:transition-all motion-safe:duration-200`
