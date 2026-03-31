---
name: design-system
description: >
  Scaffold a complete design system from scratch or from a Figma file: tokens, components,
  style guide, and project integration. Full end-to-end setup.
  Load this skill when: "design system", "build design system", "create design system",
  "full design system", "from scratch design system", "figma design system",
  "design system setup", "component library", "token system".
user-invocable: true
argument-hint: "[figma:<file-key>] [--name <SystemName>] [--prefix <Prefix>] [--output <path>]"
allowed-tools: [Read, Write, Edit, Bash, Glob, Grep]
---

# Design System Scaffolder

Build a complete, production-ready design system end-to-end.

## What Gets Generated

```
DESIGN_SYSTEM_PATH/
  tokens/
    base.css              # @theme {} — raw values
    semantic.css          # @theme {} — semantic aliases
    tokens.json           # W3C format source of truth
  components/
    DsPrimitive/          # Base: Button, Badge, Avatar, Icon, Spinner
    DsComposite/          # Composed: Card, Modal, Alert, Tooltip
    DsForm/               # Form: Input, Select, Checkbox, Radio, Textarea
    DsLayout/             # Layout: Container, Stack, Grid, Divider
  StyleGuide.vue          # Living documentation page
  index.ts                # Barrel export
package.json              # (if standalone package)
```

## Workflow

### Phase 1: Token Extraction / Creation

Read `UI_UX_KIT: FIGMA_CONNECTOR=...` from hook-injected context:
- `FIGMA_CONNECTOR=available` + file key provided → run `/design-tokens figma:<key>`
- `FIGMA_CONNECTOR=unavailable` or no file key → guide through scratch token creation:

1. Pick a primary color (all others derived)
2. Choose typography scale
3. Confirm spacing scale (Tailwind default or custom)

### Phase 2: Core Component Set

Ask which component tiers to generate. Defaults:

**Tier 1 — Primitives (always)**
- `DsButton` — variants: primary, secondary, ghost, destructive; sizes: sm, md, lg
- `DsBadge` — variants: default, success, warning, error, info; sizes: sm, md
- `DsAvatar` — image + fallback initials; sizes: xs, sm, md, lg
- `DsIcon` — wrapper for any icon library; sizes: sm, md, lg
- `DsSpinner` — loading indicator; sizes: sm, md, lg

**Tier 2 — Composites (optional)**
- `DsCard` — surface container with optional header/footer slots
- `DsModal` — accessible dialog with focus trap
- `DsAlert` — status messages with icon + dismiss
- `DsTooltip` — hover/focus tooltip
- `DsDropdown` — accessible menu

**Tier 3 — Form (optional)**
- `DsInput` — text input with label, hint, error states
- `DsSelect` — native + custom select
- `DsCheckbox` — accessible checkbox with label
- `DsRadio` — radio group
- `DsTextarea` — multiline input

**Tier 4 — Layout (optional)**
- `DsContainer` — max-width centered wrapper
- `DsStack` — vertical/horizontal flex stack with gap token
- `DsGrid` — CSS grid wrapper with column configs
- `DsDivider` — horizontal/vertical separator

### Phase 3: Style Guide

Run `/style-guide` to generate `StyleGuide.vue`.

### Phase 4: Integration

Offer to run `/integrate-design-system` to wire into the host project.

### Phase 5: Quality Check

Run `/accessibility-audit` on all generated components.
Run `/design-audit` for consistency.

## Progress Output

At each phase, report:
```
Phase 1/5: Tokens ✓ (42 base, 18 semantic)
Phase 2/5: Components ✓ (12 components generated)
Phase 3/5: Style Guide ✓
Phase 4/5: Integration ✓ (added to main.css + router)
Phase 5/5: Quality ✓ (0 a11y errors, 0 design violations)
```

## Execution

$ARGUMENTS

Run through all phases. After each phase, confirm before proceeding to the next. Surface any blockers immediately.
