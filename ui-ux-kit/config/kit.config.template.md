# UI/UX Kit — User Config

Copy this file to `~/.claude/kit.config.md` and fill in your values.

> **Figma auth** is handled automatically by the Claude Figma connector.
> No API token required. Enable it once in Claude.ai Settings → Integrations → Figma.

```
## Design system output
DESIGN_SYSTEM_PATH=./src/design-system
COMPONENT_PREFIX=Ds

## Tailwind
TAILWIND_VERSION=4
TAILWIND_CSS_FILE=./src/assets/main.css

## Accessibility
A11Y_TARGET=WCAG_2_1_AA
```

### Field reference

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `DESIGN_SYSTEM_PATH` | Yes | `./src/design-system` | Output root for generated design system files |
| `COMPONENT_PREFIX` | No | `Ds` | Prefix for generated Vue components (e.g. `DsButton`, `DsCard`) |
| `TAILWIND_VERSION` | No | `4` | Tailwind CSS version in use (3 or 4) |
| `TAILWIND_CSS_FILE` | No | `./src/assets/main.css` | Path to the CSS file containing `@theme {}` block |
| `A11Y_TARGET` | No | `WCAG_2_1_AA` | Accessibility standard target for audits |

### Figma connector setup (one-time)

1. Go to [claude.ai](https://claude.ai) → Settings → Integrations
2. Find **Figma** and click Connect
3. Authorize Claude to access your Figma files
4. Done — all skills that use Figma will work automatically
