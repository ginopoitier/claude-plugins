---
name: tech-refinement
description: >
  Technically refine a Jira story: reads the ticket, identifies unknowns, dependencies,
  and risks, breaks it into subtasks with context and time estimates, and creates everything
  in Jira. Uses sprint duration from config for capacity guidance.
  Load this skill when: "refine story", "tech refinement", "technical refinement",
  "break down story", "story breakdown", "subtasks", "estimate story", "refine ticket",
  "prepare for sprint", "definition of ready", "DoR check", "sprint planning".
user-invocable: true
argument-hint: "[TICKET-123 or story title]"
allowed-tools: Read, mcp__atlassian__jira_get_issue, mcp__atlassian__jira_create_issue, mcp__atlassian__jira_update_issue, mcp__atlassian__jira_add_comment, mcp__atlassian__confluence_search, mcp__atlassian__confluence_get_page
---

# Technical Refinement

## Core Principles

1. **Read the SDLC Definition of Ready** — check what your SDLC requires for a story to be sprint-ready before producing output. Read from `SDLC_CONFLUENCE_SPACE` + `SDLC_PARENT_PAGE` in `~/.claude/confluence-kit.config.md`.
2. **Unknowns block the sprint** — surface all unknowns explicitly. Flag them, assign owners, or split the story.
3. **Subtasks are the unit of work** — each subtask must have a clear description, an owner, and an hour estimate.
4. **Sprint capacity is the constraint** — use `SPRINT_DURATION_DAYS` from jira-kit config to frame estimates. A 2-week sprint is roughly 80 dev-hours per person.

## Patterns

### Read Config and Ticket

```bash
SPRINT_DAYS=$(grep "^SPRINT_DURATION_DAYS=" ~/.claude/jira-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
SPRINT_DAYS=${SPRINT_DAYS:-14}
```

```
mcp__atlassian__jira_get_issue
  issueIdOrKey: "{TICKET-KEY}"
→ Load: summary, description, acceptance criteria, story points, assignee, labels
```

### Read SDLC DoR

```
mcp__atlassian__confluence_search
  query: "definition of ready OR DoR"
  spaceKey: {SDLC_SPACE}
→ Extract: required fields, required subtask types, sign-off requirements
```

### Produce Refinement Output

For each story, output:

**Unknowns and Risks:**
- List each unknown with: what is unknown, who should resolve it, impact if unresolved

**Subtasks (create in Jira):**
- `[BE]` Backend implementation task
- `[FE]` Frontend task (if applicable)
- `[TEST]` Integration/unit test task
- `[REVIEW]` Code review task
- Each subtask: title, description with context, estimate in hours

**DoR Checklist:**
- ✅ / ❌ for each DoR item from the SDLC

**Story Point Validation:**
- Confirm estimate is consistent with subtask total hours
- Flag if subtask total >> story point estimate

### Create Subtasks

```
mcp__atlassian__jira_create_issue
  projectKey: {KEY}
  issueType: "Sub-task"
  summary: "[BE] Implement CreateOrder command handler"
  description: "Context: ... Acceptance: ..."
  parent: {TICKET-KEY}

mcp__atlassian__jira_update_issue
  issueIdOrKey: "{TICKET-KEY}"
  fields:
    timeoriginalestimate: {total hours in seconds}
```

### Add Refinement Comment

```
mcp__atlassian__jira_add_comment
  issueIdOrKey: "{TICKET-KEY}"
  comment: "Tech refinement complete. Subtasks created. Unknowns: [list]. DoR status: [pass/fail]."
```

## Execution

1. Read config. Load ticket. Read SDLC DoR.
2. Analyse the ticket: identify unknowns, dependencies, technical approach.
3. Draft subtasks + DoR checklist. Show to user for review.
4. On confirmation: create subtasks in Jira, add refinement comment.

$ARGUMENTS
