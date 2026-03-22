---
name: pr-prep
description: >
  Prepare a pull request description from the current diff and linked Jira ticket.
  Reads SDLC PR template from Confluence, verifies acceptance criteria are met,
  and produces a ready-to-paste PR body with risk classification and review guidance.
  Load this skill when: "prepare pr", "pr description", "write pr", "create pr",
  "pr body", "pull request description", "ready to raise pr", "pr template",
  "before i submit", "open pr", "pr review prep", "check acceptance criteria",
  "draft pr", "pr checklist".
user-invocable: true
argument-hint: "[branch or --jira TICKET-123]"
allowed-tools: Read, Bash, Grep, Glob, mcp__atlassian__jira_get_issue, mcp__atlassian__confluence_search, mcp__atlassian__confluence_get_page
---

# PR Prep

## Core Principles

1. **Read the SDLC PR template first** — Your company may have a mandatory PR description format in Confluence. Always look for it before writing anything. Use it exclusively if found.
2. **Derive the Jira ticket from the branch name** — Branch names like `feature/ORD-456-order-status` contain the key. Use it to pull the story's description and acceptance criteria automatically.
3. **Verify ACs against the diff** — Read the acceptance criteria from the Jira ticket and check each one against the actual code changes. Flag any AC not evidently addressed before proceeding.
4. **The description tells reviewers what to care about** — Don't just list files changed. Explain the intent, highlight the risky parts, and guide the reviewer to what matters.
5. **PR is not done until SDLC gate passes** — Flag any AC gaps explicitly in the PR body rather than silently checking them off. Reviewers discovering gaps is worse than proactively documenting them.

## Patterns

### Full PR Prep Flow

```
/pr-prep

Step 1 — Get the diff:
  git diff main...HEAD  (or git diff origin/main...HEAD)
  git log main..HEAD --oneline
  → Understand what changed: files, scope, intent

Step 2 — Extract Jira ticket:
  Current branch: git branch --show-current
  Pattern: feature/{KEY}-{N}-* or fix/{KEY}-{N}-*
  → mcp__atlassian__jira_get_issue("{KEY}-{N}")
  → Pull: summary, description, acceptance_criteria, story_points, epic link

Step 3 — Read SDLC PR template (if configured):
  mcp__atlassian__confluence_search("pull request template OR PR description format",
    space: SDLC_CONFLUENCE_SPACE)
  → Use company template if found; use default structure below if not

Step 4 — Check ACs against diff:
  For each acceptance criterion in the Jira ticket:
  - Read the relevant changed files
  - Determine: ✅ clearly addressed | ⚠️ partially addressed | ❌ not addressed
  → Flag any ❌ or ⚠️ before proceeding — these are gaps that must be documented

Step 5 — Write the PR description
Step 6 — Output the description + AC verification report
```

### PR Description Structure (default)

```markdown
## {Jira ticket title}

**Ticket:** [{KEY}-{N}]({JIRA_BASE_URL}/browse/{KEY}-{N})
**Type:** Feature | Bug fix | Refactor | Chore
**Risk:** Low | Medium | High

---

### What changed
[2-4 sentences: what the feature/fix is, why it was needed.
Not a list of files — the diff shows that. Explain the intent.]

### How it works
[Optional — for non-obvious changes. Explain the approach taken,
especially if there were multiple options considered.]

### Acceptance criteria
- [x] AC1: {copied from Jira — checked if clearly addressed in diff}
- [x] AC2: {with note of which file/line satisfies it}
- [ ] AC3: {unchecked if not addressed — explain why or document as known gap}

### Review guidance
**Focus here:**
- `{File}:{line-range}` — {why this is the most important part}
- `{File}` — {any tricky logic or deliberate trade-off}

**Safe to skim:**
- Migrations, generated files, config changes, test data

### Testing
- [ ] Integration tests added/updated: `{test file}`
- [ ] Tested locally: {what you tested and how}
- [ ] No manual testing needed: {reason}

### Checklist
- [ ] Linked to Jira ticket
- [ ] Tests pass locally
- [ ] No secrets or hardcoded values
- [ ] Breaking change? {yes/no — if yes, document migration path}
```

### AC Verification Report

Show this before the final PR description output:

```markdown
## AC Verification Against Diff

| # | Acceptance Criterion | Status | Evidence |
|---|---------------------|--------|----------|
| 1 | Given a placed order, when I view order detail, then status is shown | ✅ | OrderDetail.vue:45 — status badge rendered from `order.status` |
| 2 | Given a shipped order, then estimated delivery date is shown | ✅ | OrderStatusResponse.cs:12 — EstimatedDelivery field added |
| 3 | Status updates within 5 seconds via real-time connection | ⚠️ | SignalR listener added but no load test for 5s SLA |

**Gap:** AC3 SLA not verifiable from code alone — flagged in PR description.
```

### Risk Classification

```
Low    — internal refactor, no API/schema change, no new dependencies
Medium — new endpoint, new DB column, new dependency, any config change
High   — schema migration, auth change, middleware change, breaking API change,
         removal of existing behavior, changes to payment/auth flows

High-risk PRs:
- Add "⚠ High Risk" to the PR title prefix
- Expand Review guidance with explicit call-outs for each risky area
- Include rollback plan if the change can't be easily reverted
```

## Anti-patterns

### Description That Just Repeats the Diff

```markdown
# BAD — tells reviewers nothing they can't see themselves
## What changed
- Modified OrderEndpoints.cs
- Modified OrderDetail.vue
- Added OrderStatusResponse.cs
- Added tests

# GOOD — explains intent and guides the reviewer
## What changed
Adds real-time order status tracking to the order detail page.
The status is now pushed via the existing SignalR orders hub instead of
being fetched on page load, reducing stale-data support tickets.

## Review guidance
Focus: OrderHub.cs:87 — the event emission logic. This fires on every
state transition and could spam the hub if transitions happen in rapid succession.
I've added a debounce at the application layer (OrderStateMachine.cs:34).
```

### Skipping the AC Check

```markdown
# BAD — ACs copied without verification, just assumed to be done
- [x] Status updates in real time  ← checked without verifying SignalR was actually added

# GOOD — each AC verified against the diff with evidence
- [x] Status updates in real time ← SignalR OnReceived handler in useOrderUpdates.ts:23
- [ ] Status loads within 2s on mobile ← not addressed; requires performance test outside this PR
```

### Raising a PR With Unresolved Gaps (Silently)

```
# BAD — PR raised with AC gaps, reviewers discover it
Reviewer: "Looks like AC3 isn't implemented?"
Author: "Oh... I thought that was out of scope"

# GOOD — gaps surfaced proactively in the PR body
"## Known gaps
AC3 (mobile load time SLA) is not verified in this PR.
Performance test is tracked in ORD-489 and will be validated before release.
Product owner aware and approved this scope reduction."
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Branch has Jira key in name | Auto-fetch ticket, verify all ACs against diff |
| Branch has no Jira key | Ask user for ticket, or proceed with no AC check |
| SDLC PR template found in Confluence | Use it exclusively — don't mix with default |
| AC not addressed in diff | Flag as gap in output — do NOT silently check it off |
| AC partially addressed | Mark as ⚠️ with explanation in PR body |
| High-risk change | Expand Review guidance, add risk rating, consider rollback plan |
| Breaking API change | Add migration path to PR body |
| No SDLC configured | Use default structure |
| PR already exists | Output description ready to paste/update |
| Large diff (50+ files) | Focus Review guidance on the 20% of files with 80% of the risk |

## Execution

You are executing the /pr-prep command. Prepare a PR description from the diff and Jira ticket.

1. Run `git diff main...HEAD` and `git log main..HEAD --oneline` to understand the changes
2. Extract the Jira ticket key from `git branch --show-current`
3. Fetch the Jira issue (if configured) for summary and acceptance criteria
4. Search Confluence for the SDLC PR template (if `SDLC_CONFLUENCE_SPACE` is set)
5. Verify each AC against the diff: ✅ done | ⚠️ partial | ❌ missing
6. Write the PR description using the template above (or the SDLC template if found)
7. Output the AC verification report followed by the ready-to-paste PR description
