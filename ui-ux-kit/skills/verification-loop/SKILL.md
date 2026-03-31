---
name: verification-loop
description: >
  Structured check after generating components or design system files — verify the output
  compiles, matches the spec, and passes accessibility rules.
  Load this skill when: "verify", "check output", "validate component", "did it work",
  "test generated code", "verify design tokens", "confirmation loop", "post-generation check".
user-invocable: false
allowed-tools: [Bash, Grep, Read]
---

# Verification Loop (UI/UX)

After generating any design system artifact, always verify before handing off.

## Verification Checklist

### After generating design tokens
- [ ] `@theme {}` block has no duplicate variable names
- [ ] All semantic tokens reference base tokens (no raw hex values in semantic layer)
- [ ] CSS custom property names are valid identifiers (no dots, starts with `--`)
- [ ] Dark mode overrides exist for all semantic surface/text tokens

### After scaffolding a Vue component
- [ ] `<script setup lang="ts">` is present
- [ ] All props have TypeScript types
- [ ] All emits are typed
- [ ] No `any` types in the component
- [ ] `v-bind="$attrs"` present on root element
- [ ] At least one accessibility attribute (`aria-label`, `role`, or semantic tag) present
- [ ] No hardcoded color/spacing values — only token references

### After generating a style guide page
- [ ] All token categories are represented (colors, typography, spacing)
- [ ] Color swatches show both token name and CSS variable
- [ ] Page renders without errors

### After running accessibility audit
- [ ] All `error` severity issues have a fix applied
- [ ] Re-run audit to confirm score improved

## Loop Protocol

```
Generate → Verify checklist → Fix failures → Re-verify → Hand off
```

Max 3 iterations. If issues persist after 3 loops, surface them to the user instead of continuing silently.

## Execution

After completing any generation task, run through the relevant verification checklist. Fix issues found, then confirm completion.

$ARGUMENTS
