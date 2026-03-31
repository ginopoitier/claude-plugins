# Design System Architecture

## What a Design System Is

A design system is three things working together:

```
Design Tokens  ──→  Components  ──→  Patterns/Templates
(primitives)         (UI units)       (page compositions)
```

1. **Design Tokens** — the atomic values: colors, spacing, type scales
2. **Components** — reusable UI units built from tokens
3. **Patterns/Templates** — page-level compositions built from components

---

## Architecture Decision: Standalone vs Embedded

### Standalone Design System (npm package)
Best for:
- Multiple apps consuming the same design system
- Design system maintained by a dedicated team
- Versioned releases with changelogs

```
@company/design-system/
  package.json
  src/
    tokens/
    components/
    index.ts
  dist/           # compiled output published to npm
```

**Trade-offs:** More setup overhead; versioning lag between design system and app updates.

### Embedded Design System (in-app directory)
Best for:
- Single-app design system
- Small team, no separate design system maintainer
- Fast iteration without npm publish cycle

```
src/
  design-system/
    tokens/
    components/
    index.ts
  app/
    pages/
    features/
```

**Trade-offs:** Can't share across apps; easier to let it drift into app-specific code.

---

## Component Architecture Decisions

### Decision: Headless vs Styled Components

| Approach | Pros | Cons |
|----------|------|------|
| **Fully styled** (own CSS/Tailwind) | Full control over look | More maintenance |
| **Headless** (Radix Vue, Headless UI) | Accessibility built-in | Need to style everything |
| **Hybrid** (styled primitives + headless for complex) | Best of both | Two patterns to learn |

**Recommendation:** Styled Tailwind for primitives (Button, Badge, Avatar). Headless (Radix Vue) for complex interactive components (Dialog, Select, DropdownMenu). Don't build accessibility primitives from scratch.

### Decision: Class Variance Authority vs Plain clsx

| Tool | Use when |
|------|---------|
| `cva` | Component has 2+ variants OR 2+ size options |
| `clsx` | Simple conditional classes (1 variant, 1 state) |
| Raw template literal | 0 conditions, static classes |

### Decision: Global registration vs import-on-use

| Approach | Use when |
|----------|---------|
| Global registration | App uses most components everywhere |
| Import-on-use | Tree-shaking matters; large component library |
| `unplugin-vue-components` | Want auto-import without global registration bloat |

---

## Token Architecture Decisions

### Decision: Flat vs Nested token names

| Style | Example | Use when |
|-------|---------|---------|
| Dot-nested (W3C) | `color.primary.500` | JSON source of truth, tool interop |
| Dash-flat (CSS) | `--color-primary-500` | CSS custom properties, Tailwind |
| Slash-nested (Figma) | `Color/Primary/500` | Figma variable names |

**Decision:** Store tokens in W3C dot-nested format in `tokens.json`. Transform to dash-flat for CSS. This is what Style Dictionary does.

### Decision: oklch vs hex/rgb

**Use oklch** for all new tokens because:
- Perceptually uniform: equal lightness steps look equal to humans
- Full P3 gamut support on modern displays
- Easy to manipulate (darken = reduce L, vibrate = increase C)
- Supported in all modern browsers (Safari 15.4+, Chrome 111+)

**Fall back to hex** only for legacy browser support requirements.

### Decision: When to add a semantic tier

Add semantic tokens when:
- The same color is used in multiple contexts with different intent
- You support multiple themes (light/dark, brand A/brand B)
- Components should not reference raw palette values directly

Skip semantic tier when:
- Single-page project with no theming
- Very small scale (≤5 components)

---

## File Organization

```
design-system/
  tokens/
    tokens.json         # W3C source of truth — the only place to edit tokens
    base.css            # generated: primitive @theme values
    semantic.css        # generated: semantic @theme aliases
    components.css      # optional: component-scoped tokens
  components/
    primitives/
      DsButton/
        DsButton.vue
        DsButton.test.ts
        DsButton.stories.ts   # optional
      DsBadge/
      DsAvatar/
      DsIcon/
      DsSpinner/
    composite/
      DsCard/
      DsModal/
      DsAlert/
      DsTooltip/
    form/
      DsInput/
      DsSelect/
      DsCheckbox/
    layout/
      DsContainer/
      DsStack/
      DsGrid/
  patterns/
    DataTable/
    CommandPalette/
  templates/
    DashboardLayout.vue
    AuthLayout.vue
  StyleGuide.vue
  index.ts
  figma-registry.json   # node-id → component mapping for drift detection
```

---

## Versioning Strategy

For standalone packages, follow semantic versioning:
- **Patch** (1.0.x) — bug fixes, accessibility improvements, no API changes
- **Minor** (1.x.0) — new components, new token additions, backwards-compatible
- **Major** (x.0.0) — breaking API changes (renamed props, removed components, token renames)

**Token renames are always a major version bump** — they break every consumer.

Use a deprecation cycle:
```css
/* v1.5 — add new token, keep old one with deprecation notice */
@theme {
  --color-brand-500: oklch(55% 0.2 255);   /* new name */
  --color-primary: var(--color-brand-500); /* deprecated — remove in v2 */
}
```

---

## Quality Gates Before Release

1. `npm run build` passes — no TypeScript errors
2. All components pass `/accessibility-audit` with score ≥ 90
3. `/design-audit` shows 0 token violations
4. All components have a story or style guide entry
5. `tokens.json` is in sync with latest Figma export
6. `CHANGELOG.md` updated
7. Version bumped in `package.json` and `plugin.json`
