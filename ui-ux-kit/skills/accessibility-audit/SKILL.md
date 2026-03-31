---
name: accessibility-audit
description: >
  WCAG 2.1 AA accessibility audit of Vue components — checks for missing labels, contrast
  issues, keyboard traps, missing ARIA, and interactive element problems.
  Load this skill when: "accessibility audit", "a11y audit", "wcag audit", "accessibility check",
  "screen reader", "aria", "contrast check", "keyboard accessibility", "focus management",
  "accessibility issues", "wcag compliance", "a11y check", "audit components".
user-invocable: true
argument-hint: "[<component-path-or-dir>] [--standard WCAG_2_1_AA|WCAG_2_2_AA] [--fix]"
allowed-tools: [Read, Write, Edit, Bash, Glob, Grep]
---

# Accessibility Audit

Audit Vue components against WCAG 2.1 AA (or 2.2 AA).

## Steps

### 1. Resolve target

- If $ARGUMENTS has a path: audit that component or directory
- If `--fix` is not set: report-only mode
- Default target: `DESIGN_SYSTEM_PATH/components/`

### 2. MCP audit

Call `audit_accessibility` MCP tool on each component file.
Group results by severity:
- `error` — violates WCAG criterion; must fix
- `warning` — best practice violation; should fix

### 3. Manual checks (supplement MCP)

For issues the MCP may miss, do a targeted grep scan:

```bash
# Missing alt on images
grep -r '<img' --include="*.vue" | grep -v 'alt='

# Click handlers on non-interactive elements
grep -r '@click' --include="*.vue" | grep -v '<button\|<a\|<input\|<select\|<textarea'

# Outline removal without replacement
grep -r 'focus:outline-none\|outline: none' --include="*.vue" --include="*.css"

# Auto-playing media
grep -r 'autoplay' --include="*.vue"
```

### 4. Report

```
Accessibility Audit — WCAG 2.1 AA
Target: src/design-system/components/ (12 components)

ERRORS (must fix):
  DsButton.vue:42    missing-label        Icon button has no aria-label
  DsModal.vue:15     missing-focus-trap   Dialog does not trap focus
  DsInput.vue:8      label-association    Input is not associated with its label

WARNINGS (should fix):
  DsTooltip.vue:22   hover-only           Tooltip triggers only on hover, not focus
  DsCard.vue:5       heading-hierarchy    Card uses h3 without h2 parent context

PASSED: DsBadge, DsAvatar, DsSpinner, DsContainer, DsStack, DsGrid, DsDivider

Score: 72/100
```

### 5. Fix mode (if `--fix`)

For each `error` item, apply the fix automatically where possible:
- Missing `aria-label` on icon buttons → add it (prompt for label text)
- Missing `role="dialog"` + `aria-modal` → add to modal root
- Missing `for`/`id` association → add

For items requiring user input (e.g. what the aria-label should say), stop and ask.

Re-run audit after fixes to confirm score improvement.

## Execution

$ARGUMENTS

Audit the target, report all issues grouped by severity, and optionally apply fixes.
