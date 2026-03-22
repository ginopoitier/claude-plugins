---
name: sdlc-check
description: >
  Validate the current work item against the company SDLC documented in Confluence.
  Reads the relevant SDLC pages and produces a compliance checklist with blockers and warnings.
  Load this skill when: "sdlc check", "sdlc compliance", "process check", "check against sdlc",
  "is this ready", "does this meet process", "process compliance", "ready for sprint",
  "ready for release", "check process", "definition of ready", "definition of done".
user-invocable: true
argument-hint: "[TICKET-123 | pr | story | design | release]"
allowed-tools: Read, Glob, Grep, Bash, mcp__atlassian__jira_get_issue, mcp__atlassian__confluence_search, mcp__atlassian__confluence_get_page
---

# SDLC Check

## Core Principles

1. **SDLC is the source of truth** — always fetch from Confluence. Never apply assumptions about what the SDLC says without reading it.
2. **Different gates for different stages** — the SDLC has different requirements for: story ready for sprint (DoR), story ready to merge (PR process), story ready for release (DoD, release checklist). Match the check to the stage.
3. **Blockers vs warnings** — a blocker means "cannot proceed without resolving this". A warning means "should be addressed but doesn't block". Be explicit about which is which.
4. **Produce actionable output** — every failing check must state what is missing and who should fix it. Not just "missing AC" but "acceptance criteria field is empty on ORD-456 — tech lead to define before sprint start".
5. **Requires SDLC configured** — if `SDLC_CONFLUENCE_SPACE` is not set, tell the user to configure it via `/kit-setup`. Do not silently skip.

## Patterns

### Stage Detection

```
Detect the check type from the argument:
  TICKET-123              → Definition of Ready check (pre-sprint)
  "pr" or PR number       → PR / merge readiness check
  "release"               → Release readiness check (DoD + release checklist)
  "design" or SDR-NNN     → Design review check
  (nothing)               → Ask the user what to check
```

### Full Check Flow

```
/sdlc-check ORD-456

Step 1 — Read config:
  ~/.claude/kit.config.md → SDLC_CONFLUENCE_SPACE, SDLC_PARENT_PAGE

Step 2 — Read the work item:
  mcp__atlassian__jira_get_issue("ORD-456")
  → description, acceptance_criteria, technical_description, story_points, status, assignee

Step 3 — Fetch relevant SDLC pages (parallel searches):
  mcp__atlassian__confluence_search("definition of ready", space: SDLC_CONFLUENCE_SPACE)
  mcp__atlassian__confluence_search("definition of done", space: SDLC_CONFLUENCE_SPACE)
  mcp__atlassian__confluence_search("story format OR user story", space: SDLC_CONFLUENCE_SPACE)
  → Read the matching pages

Step 4 — Run compliance checks against fetched SDLC pages

Step 5 — Output compliance report
```

### Compliance Report Format

```markdown
## SDLC Check — ORD-456: {Story Title}
**Stage:** Definition of Ready (pre-sprint)
**Date:** {date}

### Blockers 🔴 (must resolve before proceeding)
1. **Acceptance criteria empty** — The acceptance_criteria field on ORD-456 is empty.
   SDLC requires: at minimum 3 testable ACs per story.
   → Action: Tech lead to define ACs. Use `/epic` or add manually in Jira.

2. **Story not estimated** — Story points field is blank.
   SDLC requires: all stories entering sprint must be estimated.
   → Action: Run `/tech-refinement ORD-456` to break down and estimate.

### Warnings 🟡 (should fix, doesn't block)
1. **Technical description sparse** — The technical_description field has only 1 line.
   SDLC recommends: API changes, DB impact, and affected components documented.
   → Action: Expand via `/tech-refinement ORD-456`.

2. **No SDR linked** — This story involves a significant technology choice (Redis integration).
   SDLC recommends: SDRs for decisions with multi-service impact.
   → Action: Consider running `/sdr new "Redis session storage for ORD-456"`.

### Passing ✅
- [x] Story description present and follows SDLC format
- [x] Assignee set
- [x] Epic link present (ORD-400)
- [x] Labels applied

### Verdict
❌ NOT READY — 2 blockers must be resolved before this story enters the sprint.
```

### PR / Merge Readiness Check

```markdown
## SDLC Check — PR: {branch or PR number}
**Stage:** PR / merge readiness

Checks sourced from SDLC PR process page:

### Blockers 🔴
1. **No linked Jira ticket** — branch name "feature/order-status" has no ticket reference.
   SDLC requires: branch must reference a Jira key (e.g. feature/ORD-456-order-status).

2. **Missing tests** — diff adds 3 new endpoints, no new test files found.
   SDLC requires: integration tests for all new endpoints.

### Warnings 🟡
1. **No PR description** — PR body is empty.
   SDLC recommends: PR description explaining what and why.

### Verdict
❌ NOT MERGEABLE — 2 blockers.
```

### Release Readiness Check

```markdown
## SDLC Check — Release Readiness
**Stage:** Pre-release / DoD

Checks all stories in current sprint / release milestone.

For each story:
  - DoD checklist complete?
  - Tests passing?
  - Documentation updated?
  - Runbook updated (if infra change)?
  - Stakeholder sign-off?

Summary table:
| Story | DoD | Tests | Docs | Sign-off | Status |
|-------|-----|-------|------|----------|--------|
| ORD-456 | ✅ | ✅ | ⚠️ | ✅ | ⚠️ Warning |
| ORD-457 | ✅ | ✅ | ✅ | ✅ | ✅ Ready |
```

## Anti-patterns

### Checking Without Reading SDLC

```
# BAD — applying assumptions about what DoR/DoD requires
"Your story is missing acceptance criteria and tests."
→ What if the company's SDLC doesn't require unit tests? Or uses a different AC format?

# GOOD — always read SDLC first
mcp__atlassian__confluence_search("definition of ready", space: SDLC_CONFLUENCE_SPACE)
→ "Company DoR requires: 3 ACs, estimated, has wireframes for UI stories"
→ Check exactly against those requirements
```

### Treating Warnings as Blockers

```
# BAD — blocking on optional items
🔴 BLOCKER: "No code comments on private methods"
→ This is a style preference, not a process blocker

# GOOD — severity matches SDLC language
If SDLC says "must" → 🔴 Blocker
If SDLC says "should" or "recommended" → 🟡 Warning
If SDLC says "optional" or "consider" → 🔵 Suggestion
```

### Skipping the Check When Busy

```
# BAD — ship without SDLC check on Friday afternoon
"We're in a rush, skipping the process check"
→ Missing DoD items discovered in production

# GOOD — the check is fast, the consequences aren't
/sdlc-check release → runs in seconds, catches gaps before they ship
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Pre-sprint planning | `/sdlc-check TICKET-123` for each story entering sprint |
| Before raising PR | `/sdlc-check pr` against current branch |
| Before release | `/sdlc-check release` across all sprint tickets |
| SDLC not configured | Tell user to configure via `/kit-setup`, stop |
| SDLC page not found for a check type | Apply sensible defaults, note the gap clearly |
| Blocker found | Do not mark as ready/merged — return actionable fix |
| All checks pass | State verdict clearly: "✅ READY for {stage}" |
