---
name: session-management
description: >
  Start, resume, and end development sessions — captures goals, tracks task progress,
  and creates handoff summaries for future sessions.
  Load this skill when: "session", "start session", "end session", "resume session",
  "/session-management", "session status", "what was I working on", "session handoff".
user-invocable: true
argument-hint: "[start|end|status|resume] [goal description]"
allowed-tools: Read, Write, Edit, Glob, Grep, Bash, TodoWrite
---

# Session Management — Development Session Lifecycle

## Core Principles

1. **One goal per session** — A session should have a single, clear objective that can be achieved in 2–4 hours. If the scope grows mid-session, either narrow it back or plan a follow-up session.
2. **Sessions produce committable states** — Each session should end with the codebase in a working, committable state. Partial work that breaks the build is a session that hasn't ended properly.
3. **Handoff notes are written for future-you** — The Handoff section of `session.md` should contain everything a fresh context needs to pick up where you left off: what was done, what's next, and any non-obvious context.
4. **Save decisions to memory, not just session files** — Session files are ephemeral. Architecture decisions, team conventions discovered, or important blockers should be saved to project memory (MEMORY.md) so they persist across sessions.
5. **Stale sessions start fresh** — A session more than 2 days old without an end entry should be archived and a new one started. The world has changed; stale plans mislead.

## Patterns

### Session File Format

```markdown
# Session — 2026-03-22

**Goal:** Implement order cancellation endpoint with domain validation
**Started:** 09:15
**Status:** In Progress

## Plan
- [x] Create CancelOrderCommand + handler
- [x] Add cancel endpoint to OrderEndpoints
- [ ] Write integration tests (happy path + already-cancelled 409)
- [ ] Run /verify to confirm build and tests pass

## Notes
- `Order.Cancel()` already exists in domain — just needed the handler and endpoint
- AppFactory uses Testcontainers SQL Server — first run is slow (~30s)

## Handoff
**Done:** Command, handler, and endpoint are complete. Tests file created but
only has the happy path test so far.
**Next:** Write the 409 (AlreadyCancelled) and 422 (validation) test cases,
then run /verify.
**Context:** The CancelOrderCommand sends `Guid orderId`. The handler loads the
Order, calls `order.Cancel()`, and maps the Result. Tests are in
`tests/MyApp.Tests/Orders/CancelOrderTests.cs`.
```

### Session Task List Pattern

```markdown
// GOOD — task list matches the session plan, granular enough to track
## Plan
- [x] Read existing Order aggregate to understand Cancel() behavior
- [x] Scaffold CancelOrderCommand + CancelOrderHandler
- [x] Add DELETE /api/orders/{id}/cancel route to OrderEndpoints
- [ ] Add integration test: Cancel_PendingOrder_Returns204
- [ ] Add integration test: Cancel_AlreadyCancelled_Returns409
- [ ] Run /verify

// BAD — too coarse to track, can't tell what's done
## Plan
- [ ] Implement order cancellation
- [ ] Test it
```

### Session File Location

```
~/.claude/projects/{sanitized-cwd}/session.md

Where {sanitized-cwd} is the current working directory with slashes replaced by dashes:
  /home/user/dev/myapp  →  home-user-dev-myapp
  G:\Projects\MyApp     →  G--Projects-MyApp
```

## Anti-patterns

### Ending a Session with Broken State

```markdown
// BAD — ending a session when the build is broken
/session-management end
> Session ended. Handoff: "Added the handler, didn't finish wiring."
→ Next session picks up a broken build with no context

// GOOD — always sanity check before ending
/session-management end
→ Run git status — uncommitted changes found
→ Run dotnet build — 2 errors found
→ "Before ending: you have uncommitted changes and 2 build errors.
   Fix the errors or commit a WIP commit before ending this session."
```

### Vague Session Goals

```markdown
// BAD — goal is too broad to know when it's done
**Goal:** Work on orders feature

// GOOD — specific, achievable, and complete when done
**Goal:** Implement order cancellation — CancelOrderCommand, handler, endpoint, and integration tests
```

### Keeping Important Context Only in Session Files

```markdown
// BAD — team convention discovered during session, only in session.md
## Notes
- Team uses "CQRS-style" — every operation has its own command/query file

// GOOD — also save to project memory so it persists
→ Save to MEMORY.md: "Convention: every operation has its own command/query file.
  No shared 'service' classes. One handler per file."
→ Also note in session.md
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Starting a new work period | `/session-management start <goal>` |
| Need to stop and will resume later | `/session-management end` |
| Returning after a break | `/session-management resume` |
| Want to know current progress | `/session-management status` |
| Session is > 2 days old | Start fresh — archive the old session file |
| Goal grew too large mid-session | Narrow scope, move extras to a follow-up |
| Discovered an important convention | Save to MEMORY.md, not just session notes |
| End of session — build broken | Fix build before ending, or note it explicitly in handoff |

## Execution

### `/session-management start [goal]`
1. Read `~/.claude/projects/{sanitized-cwd}/session.md` if it exists (previous session state)
2. Read MEMORY.md to recall any relevant project context
3. Ask what the user wants to accomplish if no goal given
4. Write a session file to `~/.claude/projects/{sanitized-cwd}/session.md` using the format above
5. Create a TodoWrite task list matching the plan
6. **Obsidian (optional):** If `~/.claude/obsidian-kit.config.md` exists, append a session-start entry to today's dev journal:
   ```bash
   VAULT=$(grep "^OBSIDIAN_VAULT_PATH=" ~/.claude/obsidian-kit.config.md | cut -d= -f2- | tr -d '[:space:]')
   DEV_FOLDER=$(grep "^OBSIDIAN_DEV_FOLDER=" ~/.claude/obsidian-kit.config.md | cut -d= -f2- | tr -d '[:space:]')
   JOURNAL="${VAULT}/${DEV_FOLDER:-Dev}/$(date +%Y-%m-%d).md"
   # Append: ## Session Started HH:MM — {goal}
   ```
   If the journal file doesn't exist yet, create it with a minimal frontmatter header.
7. Confirm the session started and state the first task

### `/session-management end` (or `/session-management stop`)
1. Read the current `session.md`
2. Read the TodoWrite task list for completion state
3. Run a sanity check: `git status` — warn about uncommitted changes
4. Update `session.md` with:
   - Completed items marked
   - Any blockers or in-progress items noted
   - A **Handoff** section: what was done, what's next, any important context
5. Save project memory for next session with key facts
6. **Obsidian (optional):** If `~/.claude/obsidian-kit.config.md` exists, append a session-end summary to today's dev journal (same file as session start). Include: goal, completed items, what's pending, key decisions.
7. Output a clean session summary for the user

### `/session-management status`
1. Read `session.md` and current TodoWrite state
2. Report: goal, completed vs pending, time elapsed
3. Suggest the next action

### `/session-management resume`
1. Read `session.md` — find the last incomplete session
2. Reconstruct the TodoWrite task list from incomplete items
3. Show what was done and what remains
4. Ask the user to confirm resuming or starting fresh

$ARGUMENTS
