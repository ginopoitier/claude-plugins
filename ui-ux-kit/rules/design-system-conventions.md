# Rule: Design System Conventions

## DO
- Name design tokens using three-tier hierarchy: `{category}.{scale}.{modifier}` — e.g. `color.primary.500`, `font.size.md`, `space.8`
- Use semantic token aliases that map to base tokens: `color.semantic.error` → `color.red.500`
- Name Vue components with a consistent prefix followed by a noun: `DsButton`, `DsCard`, `DsInput`
- Store all design tokens in a single source of truth: `app.css` (Tailwind v4) or `tokens.json`
- Document every component with a short description, props table, and usage example
- Keep the token namespace flat in CSS (`--color-primary-500`) even if the source is nested
- Version your design system — `package.json` version or a `DESIGN_SYSTEM_VERSION` constant

## DON'T
- Don't define colors, spacing, or typography directly in component files — always reference a token
- Don't use magic numbers (e.g. `mt-[13px]`, `#3a7bd5`) — create a token instead
- Don't mix design system concerns with application logic in the same component
- Don't export implementation details from a component — only expose intentional props and slots
- Don't rename tokens mid-project without a migration path — it breaks every consumer
- Don't create tokens for one-off uses — three usages is the minimum threshold for tokenization

## Naming Conventions

```
# Base tokens (immutable values)
color.blue.100 → color.blue.900
color.neutral.0 → color.neutral.1000
font.size.xs, sm, md, lg, xl, 2xl
space.1, 2, 4, 8, 12, 16, 24, 32, 48, 64
radius.sm, md, lg, full
shadow.sm, md, lg, xl

# Semantic tokens (alias base tokens with intent)
color.semantic.primary       → color.blue.600
color.semantic.error         → color.red.500
color.semantic.warning       → color.amber.500
color.semantic.success       → color.green.600
color.semantic.surface       → color.neutral.0
color.semantic.on-surface    → color.neutral.900
color.semantic.border        → color.neutral.200

# Component tokens (scoped to a component)
button.background.default    → color.semantic.primary
button.background.hover      → color.blue.700
button.text.default          → color.neutral.0
```

## File Structure

```
src/
  design-system/
    tokens/
      base.css           # base token @theme block
      semantic.css       # semantic aliases
      components.css     # component-scoped tokens
    components/
      DsButton/
        DsButton.vue
        DsButton.test.ts
      DsCard/
      DsInput/
    index.ts             # barrel export
  assets/
    main.css             # @import design-system + app styles
```
