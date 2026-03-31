---
name: design-audit
description: >
  Design consistency audit — scans Vue components for hardcoded values, token violations,
  design drift, and spacing/color inconsistencies.
  Load this skill when: "design audit", "consistency check", "token violations", "hardcoded colors",
  "design consistency", "check tokens", "arbitrary values", "magic numbers",
  "audit design system", "design drift", "spacing inconsistency", "color violations".
user-invocable: true
argument-hint: "[<path>] [--tokens-path <path>] [--fix]"
allowed-tools: [Read, Write, Edit, Bash, Glob, Grep]
---

# Design Audit

Scan the codebase for design consistency violations.

## Checks Performed

### 1. Token violation scan (MCP)

Call `analyze_design_consistency` MCP tool on the project or design system directory.

Reports:
- Hardcoded colors (`#fff`, `rgb(...)`, named colors outside @theme)
- Arbitrary Tailwind values (`mt-[13px]`, `text-[#3a7bd5]`)
- Missing token references (values that should reference `var(--...)` but don't)

### 2. Figma drift scan (if registry exists)

If `figma-registry.json` exists, call `detect_design_drift` for each registered component.

### 3. Structural audit (grep-based)

```
Checks:
  - <style scoped> in design system components (should be 0)
  - @apply usage (should be 0 in component files)
  - Any `style="..."` inline styles (should be 0)
  - Options API usage (should be 0)
  - Components missing v-bind="$attrs" on root element
```

### 4. Report

```
Design Audit Report
Target: src/design-system/ (12 components, 3 token files)

TOKEN VIOLATIONS:
  DsCard.vue:23        hardcoded-color      background: #f5f5f5 → use var(--color-neutral-100)
  DsAlert.vue:45       arbitrary-value      py-[14px] → use py-3.5 or add --spacing-3.5 token
  DsInput.vue:67       hardcoded-color      border-color: #e2e8f0 → use var(--color-semantic-border)

STRUCTURAL VIOLATIONS:
  DsTooltip.vue        has <style scoped> — remove and use Tailwind classes
  DsDropdown.vue       missing v-bind="$attrs" on root element

FIGMA DRIFT:
  DsModal.vue          drift 35% — background color + border radius differ from Figma

PASSED: DsButton, DsBadge, DsAvatar, DsSpinner, DsContainer, DsStack

Score: 68/100
Recommendation: Fix 3 token violations + 2 structural violations before release.
```

### 5. Fix mode (if `--fix`)

For mechanical fixes only (e.g. replacing a known hardcoded hex with its token equivalent):
- Map hardcoded value to nearest token
- Apply the substitution
- Re-run the check

For judgment calls (e.g. which token to use for an ambiguous value), ask the user.

## Execution

$ARGUMENTS

Run all audit checks, generate the report, and optionally apply mechanical fixes.
