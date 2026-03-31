---
name: learning-log
description: >
  Captures design decisions, token gotchas, Figma API quirks, and component architecture
  discoveries made during a session. Written to a session log for future reference.
  Load this skill when: "log this", "remember for later", "session notes", "design decision",
  "document this choice", "why did we do this", "learning log", "session discoveries".
user-invocable: false
allowed-tools: [Write, Read]
---

# Learning Log (UI/UX)

Capture discoveries as they happen. Don't rely on memory.

## What to Log

- **Figma API quirks** — "Variables API returns hex, not oklch — need to convert"
- **Token decisions** — "Used oklch for all colors to enable lightness manipulation"
- **Component architecture choices** — "Used cva over clsx because project already uses it"
- **Accessibility gotchas** — "VoiceOver reads hidden text in icon buttons — use aria-label not title"
- **Tailwind v4 differences** — "v4 doesn't support `theme()` function in `@layer components` — use CSS vars"

## Log Format

```markdown
## Session Log — [date]

### Design Decisions
- [decision and why]

### Figma/MCP Discoveries
- [API behavior, quirk, or limitation found]

### Component Gotchas
- [thing that was harder than expected]

### Token Notes
- [naming or transformation choice made]
```

## Execution

During the session, when a non-obvious discovery is made, append it to the session log. At session end, surface the most important items to the user.

$ARGUMENTS
