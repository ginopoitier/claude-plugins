# Design Tokens — Deep Reference

## W3C Design Token Format

The [W3C Design Token Community Group](https://design-tokens.github.io/community-group/format/) defines the standard format for design tokens as JSON.

```json
{
  "color": {
    "primary": {
      "500": {
        "$type": "color",
        "$value": "oklch(55% 0.2 255)",
        "$description": "Brand primary — mid-weight"
      }
    },
    "semantic": {
      "primary": {
        "$type": "color",
        "$value": "{color.primary.500}",
        "$description": "Semantic alias — use for interactive elements"
      }
    }
  },
  "font": {
    "size": {
      "md": {
        "$type": "dimension",
        "$value": "1rem"
      }
    }
  }
}
```

### Token Types

| `$type` | Description | Example `$value` |
|---------|-------------|-----------------|
| `color` | Any color value | `oklch(55% 0.2 255)`, `#2563eb` |
| `dimension` | Size with unit | `1rem`, `4px`, `100%` |
| `fontFamily` | Font family list | `['Inter', 'ui-sans-serif']` |
| `fontWeight` | Numeric or keyword weight | `600`, `"semibold"` |
| `number` | Unitless number | `1.5` (line-height) |
| `shadow` | Box shadow | `{ offsetX: 0, offsetY: 1, blur: 3, ... }` |
| `border` | Border shorthand | `{ color: ..., width: ..., style: ... }` |

### References

Use `{token.path}` syntax to reference other tokens:
```json
"color.semantic.primary": { "$value": "{color.primary.500}" }
```

---

## Three-Tier Token Architecture

```
┌─────────────────────────────────────────┐
│  Tier 1: Primitive / Base Tokens        │
│  Raw values. Never change meaning.      │
│  color.blue.500 = oklch(55% 0.2 255)    │
│  font.size.md = 1rem                    │
└────────────────┬────────────────────────┘
                 │ alias
┌────────────────▼────────────────────────┐
│  Tier 2: Semantic Tokens                │
│  Intent-driven aliases.                 │
│  color.semantic.primary = color.blue.500│
│  color.semantic.error = color.red.500   │
└────────────────┬────────────────────────┘
                 │ alias
┌────────────────▼────────────────────────┐
│  Tier 3: Component Tokens (optional)    │
│  Component-scoped values.               │
│  button.bg.default = semantic.primary   │
│  input.border.error = semantic.error    │
└─────────────────────────────────────────┘
```

**Rule:** Components consume Tier 2 (semantic) tokens. Never reference Tier 1 directly in components.

---

## Color Spaces

### oklch (recommended for design systems)
- Human-perceptual: equal lightness steps look equal
- Easy to derive a full scale from one hue
- `oklch(L% C H)` — Lightness (0–100%), Chroma (0–0.4), Hue (0–360°)

```css
/* Derive a primary scale from one hue (255 = blue) */
--color-primary-50:  oklch(97% 0.02 255);
--color-primary-100: oklch(94% 0.04 255);
--color-primary-200: oklch(88% 0.08 255);
--color-primary-300: oklch(79% 0.12 255);
--color-primary-400: oklch(68% 0.17 255);
--color-primary-500: oklch(55% 0.20 255);
--color-primary-600: oklch(48% 0.22 255);
--color-primary-700: oklch(40% 0.22 255);
--color-primary-800: oklch(30% 0.18 255);
--color-primary-900: oklch(22% 0.12 255);
--color-primary-950: oklch(14% 0.08 255);
```

### Contrast calculation with oklch
- White text on primary-500 (L=55%): contrast ratio ≈ 4.7:1 ✓ (AA passes)
- Black text on primary-500: contrast ratio ≈ 3.2:1 ✗ (AA fails for normal text)
- Switch to primary-700 for white text backgrounds to always pass AA

---

## Figma Variables → Tokens

### Figma Variables API
Figma exposes design tokens via the Variables API (V1 — requires Enterprise or Pro plan for publish, but can read unpublished variables).

Key endpoints:
- `GET /v1/files/{fileKey}/variables/local` — all local variables and collections
- `GET /v1/files/{fileKey}/variables/published` — published library variables

### Variable Collections and Modes
Figma organizes variables into:
- **Collections** — groups of related variables (e.g., "Brand Colors", "Spacing")
- **Modes** — variants within a collection (e.g., "Light", "Dark")

When extracting:
```
Collection: "Brand Colors"
  Mode: "Light" → maps to :root
  Mode: "Dark"  → maps to .dark or @media prefers-color-scheme: dark
```

### Mapping Figma Types to W3C Types
| Figma Type | W3C `$type` |
|-----------|-------------|
| `COLOR`   | `color` |
| `FLOAT`   | `dimension` (if unit) or `number` |
| `STRING`  | `fontFamily` (if family) or custom |
| `BOOLEAN` | — (not standard, handle manually) |

---

## Style Dictionary

[Style Dictionary](https://amzn.github.io/style-dictionary/) is the most widely-used tool for transforming W3C tokens into platform outputs.

### Quick Setup
```bash
npm install -D style-dictionary
```

```js
// style-dictionary.config.js
export default {
  source: ['tokens/tokens.json'],
  platforms: {
    css: {
      transformGroup: 'css',
      buildPath: 'tokens/',
      files: [{ destination: 'base.css', format: 'css/variables', options: { selector: ':root' } }]
    },
    tailwind: {
      transformGroup: 'js',
      buildPath: 'tokens/',
      files: [{ destination: 'tailwind-tokens.js', format: 'javascript/es6' }]
    }
  }
}
```

---

## Token Naming Cheat Sheet

```
color.{palette}.{step}              color.blue.500
color.semantic.{role}               color.semantic.primary
color.semantic.on-{surface}         color.semantic.on-surface
font.size.{t-shirt}                 font.size.md
font.weight.{keyword}               font.weight.semibold
font.family.{stack-name}            font.family.sans
space.{multiplier}                  space.4 (= 1rem if base=4px)
radius.{t-shirt}                    radius.md
shadow.{t-shirt}                    shadow.md
z.{keyword}                         z.modal, z.tooltip, z.dropdown
```
