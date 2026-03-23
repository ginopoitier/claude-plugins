---
name: adr
description: >
  Create, list, view, and deprecate Architecture Decision Records in Confluence.
  Load this skill when: "adr", "architecture decision", "decision record",
  "document decision", "technical decision", "ADR-", "/adr", "architecture choice".
user-invocable: true
argument-hint: "[new|list|show|deprecate] [decision title or ADR-number]"
allowed-tools: Read, Glob, mcp__atlassian__confluence_search, mcp__atlassian__confluence_get_page, mcp__atlassian__confluence_create_page, mcp__atlassian__confluence_update_page
---

# Architecture Decision Records (ADR)

## Core Principles

1. **One decision per record** — captures a single architectural choice with context, alternatives considered, and consequences.
2. **Immutable once Accepted** — never edit an accepted ADR. Create a new one that supersedes it and mark the original Deprecated.
3. **Context first** — the most valuable part is *why*, not *what*. Future readers need to understand the forces and constraints.
4. **Suggest proactively** — if making a non-obvious architectural choice, suggest creating an ADR before or immediately after.
5. **Stored in Confluence** — read `ADR_PARENT_PAGE` from `.claude/confluence.config.md`; fall back to `Architecture Decisions`.

## ADR Template

```
Title: ADR-{NNN}: {Short decision title}

## Status
Proposed | Accepted | Deprecated | Superseded by ADR-{NNN}

## Context
{What situation or problem drove this decision? What constraints existed?}

## Decision
{The choice that was made, stated clearly.}

## Alternatives Considered
{What else was evaluated? Why was it rejected?}

## Consequences
{What are the positive and negative results of this decision?
 Include: new patterns introduced, things made easier, things made harder, follow-up actions.}

## Date
{YYYY-MM-DD}
```

## Patterns

### Read Config

```bash
CONFLUENCE_SPACE=$(grep "^CONFLUENCE_SPACE_KEY=" .claude/confluence.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
ADR_PARENT=$(grep "^ADR_PARENT_PAGE=" .claude/confluence.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
ADR_PARENT=${ADR_PARENT:-Architecture Decisions}
```

### New ADR

1. Find the parent page ID:
```
mcp__atlassian__confluence_search
  query: "{ADR_PARENT}"
  spaceKey: {CONFLUENCE_SPACE}
→ get parentId
```

2. Find the next ADR number:
```
mcp__atlassian__confluence_search
  query: "ADR-"
  spaceKey: {CONFLUENCE_SPACE}
→ find highest ADR-NNN, increment by 1
```

3. Draft the ADR from context or user's description. Show preview. Confirm.

4. Create:
```
mcp__atlassian__confluence_create_page
  spaceKey: {CONFLUENCE_SPACE}
  title: "ADR-{NNN}: {title}"
  parentId: {parentId}
  content: {storage format content}
```

### List ADRs

```
mcp__atlassian__confluence_search
  query: "ADR-"
  spaceKey: {CONFLUENCE_SPACE}
→ Display: number, title, status, date
```

### Show ADR

```
mcp__atlassian__confluence_search
  query: "ADR-{NNN}"
  spaceKey: {CONFLUENCE_SPACE}
→ mcp__atlassian__confluence_get_page → render content
```

### Deprecate ADR

1. Get the page and its current version.
2. Update status field to `Deprecated` or `Superseded by ADR-{NNN}`.
3. Add a note at the top explaining the supersession.
```
mcp__atlassian__confluence_update_page
  pageId: {id}
  version: {current + 1}
  content: {updated content with Deprecated status}
```

## Execution

Parse `$ARGUMENTS` for subcommand (new / list / show / deprecate). Default to `new` if none.
Read config. If project config missing → tell user to run `/confluence-setup --project`.

$ARGUMENTS
