---
name: instinct-system
description: >
  Project-specific pattern learning — detects recurring design decisions, token naming choices,
  and component conventions. Stores them as instincts with confidence scores.
  Load this skill when: "remember this pattern", "learn from this", "project conventions",
  "we always do X", "instinct", "pattern learning", "confidence score".
user-invocable: false
allowed-tools: [Read, Write, Grep]
---

# Instinct System (UI/UX)

Learn and apply project-specific design patterns automatically.

## What to Track

- **Token naming style** — does this project use `color.blue.500` or `blue-500` or `blue/500`?
- **Component prefix** — `Ds`, `App`, `Ui`, or none?
- **Variant naming** — `primary/secondary/ghost` or `filled/outlined/text`?
- **Slot naming** — `default/leading/trailing` or `default/prefix/suffix`?
- **Tailwind usage** — does the project use `cva`, `clsx`, or raw string concatenation?
- **Figma file structure** — are tokens in Variables, Styles, or both?

## Confidence Tiers

| Tier | Confidence | Meaning |
|------|-----------|---------|
| Hypothesis | < 50% | Seen once, do not apply automatically |
| Instinct | 50–80% | Seen 2–3 times, apply but mention it |
| Rule | > 80% | Seen consistently, apply silently |

## Storage Format

```markdown
## Instinct: [pattern name]
- **Observed:** [what was seen and when]
- **Confidence:** [%]
- **Rule:** [the pattern to apply]
- **Counter-examples:** [any exceptions seen]
```

## Execution

Observe design decisions as they are made. When a pattern appears ≥ 2 times, record it as an instinct. Apply instincts ≥ 50% confidence when generating new components or tokens, noting when an instinct was used.

$ARGUMENTS
