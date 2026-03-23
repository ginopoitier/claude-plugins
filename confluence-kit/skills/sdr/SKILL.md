---
name: sdr
description: >
  Create, list, view, and deprecate Software Decision Records in Confluence.
  SDRs cover any significant software change: architecture, infrastructure, process,
  technology choice, or design decision.
  Load this skill when: "software decision record", "SDR", "decision record",
  "document decision", "document this change", "record this decision", "why did we choose",
  "technology choice", "infrastructure decision".
user-invocable: true
argument-hint: "[new|list|show|deprecate] [title or SDR-number]"
allowed-tools: Read, mcp__atlassian__confluence_search, mcp__atlassian__confluence_get_page, mcp__atlassian__confluence_create_page, mcp__atlassian__confluence_update_page, mcp__atlassian__jira_get_issue
---

# Software Decision Records (SDR)

## Core Principles

1. **Broader than ADRs** — SDRs cover any significant decision: technology choices, process changes, design patterns, infrastructure. If it will be questioned in 6 months, write an SDR.
2. **Context before decision** — always capture *why* the decision was needed, what constraints existed, and what was rejected.
3. **Storage is per-project** — read `SDR_CONFLUENCE_SPACE` and `SDR_PARENT_PAGE` from `.claude/confluence.config.md`.
4. **Link to Jira when relevant** — if the decision was driven by a ticket, link it.

## SDR Template

```
Title: SDR-{NNN}: {Short decision title}

## Status
Proposed | Accepted | Deprecated | Superseded by SDR-{NNN}

## Context
{What situation, requirement, or problem drove this decision?
 What constraints (technical, organisational, time) existed?}

## Decision
{The choice that was made. Be specific.}

## Alternatives Considered
| Option | Pros | Cons | Why Rejected |
|--------|------|------|--------------|
| {alt} | ... | ... | ... |

## Consequences
{Positive: what gets easier, what new capabilities exist.
 Negative: what gets harder, what is now constrained.
 Follow-up: what actions or future decisions this creates.}

## Jira Reference
{Link to ticket if applicable}

## Date
{YYYY-MM-DD}

## Author
{Name}
```

## Patterns

### Read Config

```bash
CONFLUENCE_SPACE=$(grep "^CONFLUENCE_SPACE_KEY=" .claude/confluence.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SDR_SPACE=$(grep "^SDR_CONFLUENCE_SPACE=" .claude/confluence.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SDR_SPACE=${SDR_SPACE:-$CONFLUENCE_SPACE}
SDR_PARENT=$(grep "^SDR_PARENT_PAGE=" .claude/confluence.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SDR_PARENT=${SDR_PARENT:-Software Decision Records}
```

### New SDR

1. If a Jira ticket key is provided, load it for context:
```
mcp__atlassian__jira_get_issue
  issueIdOrKey: "{TICKET-KEY}"
```

2. Find parent page and next SDR number (same pattern as ADR).

3. Draft the SDR from context. Show preview. Confirm. Create.

### List / Show / Deprecate

Same pattern as ADR — search for `SDR-` prefix, get by number, update version for deprecation.

## Execution

Parse `$ARGUMENTS` for subcommand. Default to `new`.
Read config. If project config missing → tell user to run `/confluence-setup --project`.

$ARGUMENTS
