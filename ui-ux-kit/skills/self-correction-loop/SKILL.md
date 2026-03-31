---
name: self-correction-loop
description: >
  Captures user corrections about design decisions, token choices, or component patterns
  and converts them into permanent MEMORY.md rules.
  Load this skill when: "no not that", "wrong approach", "that's not how we do it",
  "correction", "stop doing X", "remember this instead", "don't generate X".
user-invocable: false
allowed-tools: [Read, Write]
---

# Self-Correction Loop (UI/UX)

When the user corrects a design or code decision, capture it permanently.

## Trigger Signals

- Explicit corrections: "no, we use `variant` not `type` for button", "stop using `@apply`"
- Preference statements: "we always use oklch for colors", "don't use cva, we use clsx"
- Rejections: user replaces generated code with a different pattern

## Capture Protocol

1. Identify the rule violated or the pattern corrected
2. Write a feedback memory to `C:\Users\ginop\.claude\projects\G--Claude-Kits\memory\`
3. Also update the instinct-system if this is a project-specific pattern

## Memory Format

```markdown
---
name: correction-[topic]
description: User correction about [topic] in ui-ux-kit
type: feedback
---

Do NOT [wrong behavior]. Instead [correct behavior].

**Why:** [reason the user gave]
**How to apply:** [when this rule kicks in]
```

## Execution

Whenever the user corrects a decision, immediately capture it as a feedback memory before continuing. Do not wait until end of session.

$ARGUMENTS
