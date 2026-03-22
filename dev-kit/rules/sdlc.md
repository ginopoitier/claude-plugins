# Rule: SDLC Compliance

Your company's SDLC is documented in Confluence. Location comes from `~/.claude/kit.config.md`:
- `SDLC_CONFLUENCE_SPACE` — the Confluence space key
- `SDLC_PARENT_PAGE` — the parent page title

**This rule is dormant when `PM_PROVIDER=none` or `PM_PROVIDER=github-issues`** — SDLC compliance only applies on work machines using Jira. Skip silently on personal/home projects.

When `PM_PROVIDER=jira` and these keys are set, SDLC requirements apply to all work. When the keys are blank, skip SDLC checks silently and note to the user that they can configure via `/kit-setup`.

## DO
- Read the relevant SDLC page **before** creating Jira items, writing SDRs, or reviewing PRs when `SDLC_CONFLUENCE_SPACE` is configured
- Use `mcp__atlassian__confluence_search` to find the right SDLC sub-page for the task type (story format, definition of ready, definition of done, PR process, etc.)
- Apply SDLC requirements as **non-negotiables** in `/epic`, `/tech-refinement`, `/sdlc-check`, and `/review`
- Flag SDLC violations as blockers, not suggestions
- Use story format exactly as described in the SDLC — do not invent your own template
- Check Definition of Ready before marking a story as ready for sprint
- Check Definition of Done during `/review` and `/tech-refinement`

## DON'T
- Don't invent story, epic, or acceptance criteria formats — read them from the SDLC
- Don't skip SDLC page lookups to save tool calls — SDLC compliance is mandatory at work machines
- Don't silently ignore missing SDLC config — if `SDLC_CONFLUENCE_SPACE` is blank and the user asks for `/sdlc-check`, tell them to configure it via `/kit-setup`
- Don't apply SDLC rules to personal/home projects — only when `PM_PROVIDER=jira` and SDLC is configured
- Don't cache SDLC content across sessions — always fetch fresh from Confluence

## SDLC Page Lookup Strategy

```
1. Read SDLC_CONFLUENCE_SPACE + SDLC_PARENT_PAGE from ~/.claude/kit.config.md
2. Use mcp__atlassian__confluence_search to find child pages:
   - "story format" or "user story template" → story writing format
   - "definition of ready" or "DoR"           → pre-sprint checklist
   - "definition of done" or "DoD"            → completion checklist
   - "pull request" or "PR process"           → review requirements
   - "software decision record" or "SDR"      → SDR template/process
3. Read the matching page(s) before proceeding
4. If no matching page found, apply sensible defaults and note the gap
```

## Deep Reference
SDLC location is configured via `/kit-setup` → stored in `~/.claude/kit.config.md`.
