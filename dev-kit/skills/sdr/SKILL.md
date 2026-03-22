---
name: sdr
description: >
  Create, list, or view Software Decision Records — documents for any significant software
  change: architecture, infrastructure, process, technology choice, or design decision.
  Load this skill when: "software decision record", "SDR", "decision record",
  "document decision", "document this change", "record this decision", "why did we choose",
  "architecture decision", "technology choice".
user-invocable: true
argument-hint: "[new|list|show|deprecate] [title or SDR-number]"
allowed-tools: Read, Write, Glob, Bash, mcp__atlassian__confluence_search, mcp__atlassian__confluence_get_page, mcp__atlassian__confluence_create_page, mcp__atlassian__confluence_update_page, mcp__atlassian__jira_get_issue
---

# Software Decision Record (SDR)

## Core Principles

1. **SDRs are broader than ADRs** — document any significant software change: technology choices, process changes, design decisions, not just architecture. If it will be questioned in 6 months, write an SDR.
2. **Context before decision** — a decision without context is useless. Always capture why the decision was needed, what constraints existed, and what was rejected.
3. **Storage is configured per project** — read `SDR_CONFLUENCE_SPACE` and `SDR_PARENT_PAGE` from `.claude/project.config.md`. Fall back to `CONFLUENCE_SPACE_KEY` + "Software Decision Records" if not set.
4. **Link to Jira** — every SDR created from a Jira ticket should back-link to the ticket and vice versa.
5. **Check SDLC for SDR template** — if `SDLC_CONFLUENCE_SPACE` is set, search Confluence for the company's SDR template before writing. Only use the default template if none is found.

## Patterns

### SDR Default Template

```markdown
# SDR-{NNN}: {Title}

| Field       | Value |
|-------------|-------|
| Status      | Proposed \| Accepted \| Deprecated \| Superseded |
| Date        | {YYYY-MM-DD} |
| Author      | {author} |
| Jira        | [{KEY}-{N}]({JIRA_BASE_URL}/browse/{KEY}-{N}) |
| Supersedes  | — |
| Superseded by | — |

## Context

[What situation, problem, or need triggered this decision?
What constraints, requirements, or forces are at play?
What would happen if no decision were made?]

## Options Considered

### Option A: {Name}
**Pros:** ...
**Cons:** ...

### Option B: {Name}
**Pros:** ...
**Cons:** ...

### Option C: {Name} *(chosen)*
**Pros:** ...
**Cons:** ...

## Decision

[The decision made, in one clear sentence. Then explain the reasoning — why this option
over the others, and why now.]

## Consequences

**Positive:**
- ...

**Negative / Trade-offs:**
- ...

**Risks:**
- ...

## Follow-up Actions

- [ ] {action} — {owner}
```

### `/sdr new <title>` — Create a new SDR

```
1. Read config:
   - ~/.claude/kit.config.md → SDLC_CONFLUENCE_SPACE, SDLC_PARENT_PAGE, JIRA_BASE_URL
   - .claude/project.config.md → SDR_CONFLUENCE_SPACE, SDR_PARENT_PAGE, JIRA_PROJECT_KEY

2. If SDLC configured: search for SDR template page in Confluence
   mcp__atlassian__confluence_search("SDR template OR software decision record template", space: SDLC_CONFLUENCE_SPACE)
   → Use company template if found, otherwise use default above

3. If Jira ticket given (--jira TICKET-123):
   mcp__atlassian__jira_get_issue(TICKET-123)
   → Pre-fill Context from ticket description + acceptance criteria

4. Determine next SDR number:
   mcp__atlassian__confluence_search("SDR-", space: SDR_CONFLUENCE_SPACE, parent: SDR_PARENT_PAGE)
   → Count existing SDRs, increment

5. Interactively gather:
   - Context (if not pre-filled from Jira)
   - Options considered (minimum 2)
   - Decision and reasoning
   - Consequences

6. Create Confluence page:
   mcp__atlassian__confluence_create_page(
     space: SDR_CONFLUENCE_SPACE,
     parent: SDR_PARENT_PAGE,
     title: "SDR-{NNN}: {title}",
     body: filled template
   )

7. If Jira ticket linked: add comment to Jira ticket with SDR link
```

### `/sdr list` — List all SDRs

```
mcp__atlassian__confluence_search("SDR-", space: SDR_CONFLUENCE_SPACE, parent: SDR_PARENT_PAGE)
→ Display: Number | Title | Status | Date | Author
→ Group by status: Accepted → Proposed → Deprecated
```

### `/sdr show <number>` — Display an SDR

```
mcp__atlassian__confluence_search("SDR-{NNN}", space: SDR_CONFLUENCE_SPACE)
→ mcp__atlassian__confluence_get_page(page_id)
→ Format and display
```

### `/sdr deprecate <number> [superseded-by]`

```
1. Fetch the SDR page
2. Update Status field: Proposed/Accepted → Deprecated
3. If superseded-by given: add "Superseded by SDR-{N}" with link
4. mcp__atlassian__confluence_update_page(...)
```

## Anti-patterns

### Writing SDRs Without Context

```markdown
# BAD — decision without context
## Decision
We chose PostgreSQL.

# GOOD — captures the why
## Context
The current SQLite database is hitting write contention at 500 concurrent users.
We need a database that supports concurrent writes and horizontal read scaling
within our existing AWS infrastructure.

## Decision
We chose PostgreSQL over MySQL because our team has deeper expertise in it,
the JSONB column type suits our semi-structured event data, and AWS RDS
pricing is equivalent.
```

### Skipping the SDLC Template Check

```
# BAD — jump straight to writing using the default template
/sdr new "Switch to Redis for session storage"
→ Creates with default template

# GOOD — check for company template first
→ mcp__atlassian__confluence_search("SDR template", space: SDLC_CONFLUENCE_SPACE)
→ Found: "ENG: SDR Process and Template" — uses that format instead
```

### Not Linking Back to Jira

```
# BAD — SDR exists in Confluence, ticket has no trace of it
Jira ticket ORD-456: "Evaluate caching strategy" — no mention of SDR-007

# GOOD — bidirectional link
SDR-007 links to ORD-456 in the table
ORD-456 has a comment: "Decision recorded in SDR-007: [link]"
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| New SDR from scratch | `/sdr new <title>` → interactive prompts |
| SDR from a Jira ticket | `/sdr new <title> --jira TICKET-123` → pre-fills context |
| Check company format first | Always — search SDLC space for SDR template |
| Small/obvious decision | Skip SDR — use a Jira comment instead |
| Reversing a past decision | `/sdr new` + reference old SDR + `/sdr deprecate <old>` |
| Can't find SDR_CONFLUENCE_SPACE | Tell user to run `/project-setup` or set config |
| SDLC template not found | Use default template, note the gap to user |
