---
name: figma-sync
description: >
  Sync a Figma file with the codebase — extract latest tokens, detect design drift,
  and reconcile differences between Figma source and implemented components.
  Load this skill when: "figma sync", "sync from figma", "pull from figma", "figma update",
  "design drift", "token sync", "figma to code", "update from design", "reconcile design",
  "figma changed", "design updated", "pull designs".
user-invocable: true
argument-hint: "<figma-file-key-or-url> [--tokens-only] [--components-only] [--dry-run]"
allowed-tools: [Read, Write, Edit, Bash, Glob, Grep]
---

# Figma Sync

Pull the latest state from Figma and reconcile with the codebase.

> **Requires the Claude Figma connector.** If it's not enabled, tell the user:
> "The Figma connector is not available in this session. Enable it in Claude.ai Settings → Integrations → Figma, then restart the session. To work without Figma, use `/design-tokens` to create tokens from scratch."

## Steps

### 0. Verify connector

Read `UI_UX_KIT: FIGMA_CONNECTOR=...` from hook-injected context (no tool call needed).
If `FIGMA_CONNECTOR=unavailable`, stop and show the message above.

### 1. Resolve Figma file key

If user passes a URL: extract the file key.
`https://www.figma.com/design/{KEY}/...` → `{KEY}`

### 2. Fetch tokens (unless `--components-only`)

Call `mcp__claude_ai_Figma__figma_get_local_variables` with the file key.
Also call `mcp__claude_ai_Figma__figma_get_styles` for legacy color/text styles.
Pass the result to `generate_tailwind_theme` (ux-ui-mcp) to transform to CSS.
Compare output to existing `tokens.json` in `DESIGN_SYSTEM_PATH/tokens/`.

Show a diff table:
```
Token Changes:
  + color.primary.550 (new)        oklch(51% 0.21 255)
  ~ color.primary.500 (changed)    oklch(55% 0.2 255) → oklch(53% 0.21 255)
  - color.secondary.100 (removed)
```

### 3. Detect component drift (unless `--tokens-only`)

For each implemented Vue component that has a Figma node ID recorded:
- Call `mcp__claude_ai_Figma__figma_get_node` with the stored node ID
- Pass result + component file to `detect_design_drift` (ux-ui-mcp)
- Report drift score and critical vs minor drifts

```
Component Drift Report:
  DsButton    — drift 0%   ✓ Perfect match
  DsCard      — drift 12%  ⚠ Minor: padding differs (figma: 24px, code: 20px)
  DsModal     — drift 35%  ✗ Critical: background color changed, border-radius changed
```

### 4. Confirm changes

If `--dry-run`: print report only, no writes.

Otherwise, for each change category, ask for confirmation:
- "Apply 3 token changes? (2 updates, 1 addition)" [y/n]
- "Regenerate DsModal to fix critical drift?" [y/n]

### 5. Apply changes

- Write updated token files
- For confirmed component regenerations, run `/scaffold-component <Name> figma:<node-id>`
- Update `tokens.json` with new Figma version timestamp

### 6. Summary

```
Sync complete:
  Tokens: 3 updated, 1 added, 0 removed
  Components: 1 regenerated (DsModal), 1 flagged for manual review (DsCard)
  Figma version: 2026-03-31T14:00:00Z
```

## Figma Node ID Registry

Track component → Figma node ID mappings in `DESIGN_SYSTEM_PATH/figma-registry.json`:
```json
{
  "DsButton": "1234:5678",
  "DsCard": "1234:9012",
  "DsModal": "1234:3456"
}
```

This enables drift detection and re-sync per component.

## Execution

$ARGUMENTS

Resolve the file key, fetch tokens and components, show the diff, confirm changes, apply them, and summarize.
