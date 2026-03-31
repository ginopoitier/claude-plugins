# Memory Conventions

Rules for how Claude writes and maintains memory files when the memory-kit is active.

## DO

**DO** use the four memory types precisely:
- `user` — facts about the user's role, expertise, workflow, and preferences
- `feedback` — behavioral corrections ("don't", "stop", "always/never") and confirmed approaches
- `project` — current initiatives, deadlines, decisions with absolute dates (never relative like "Thursday")
- `reference` — pointers to external systems: URLs, tool names, system locations

**DO** lead feedback/project bodies with the fact/rule, followed by `**Why:**` and `**How to apply:**` lines.

**DO** keep descriptions under 150 characters — they appear as one-line index entries in MEMORY.md.

**DO** use the `memory_store` MCP tool when writing memories rather than Write/Edit directly — it maintains the MEMORY.md index automatically.

**DO** name memory files as `{type}_{slug}.md` (e.g., `feedback_avoid_db_mocking.md`).

**DO** convert relative dates to absolute ISO dates in project memories (e.g., "Thursday" → "2026-04-03").

**DO** confirm capture to the user: "Captured to memory: [name] ([type])"

## DO NOT

**DO NOT** store code patterns, architecture, file paths, or conventions — these belong in CLAUDE.md or instincts.md.

**DO NOT** store git history, recent commits, or who-changed-what — use `git log`.

**DO NOT** store ephemeral session state (in-progress tasks, current branch, "what we're working on now").

**DO NOT** store debugging solutions or fix recipes — the fix is in the code; the commit message has the context.

**DO NOT** duplicate memories — run `memory_deduplicate` if unsure before storing a new feedback memory.

**DO NOT** write memory content directly into MEMORY.md — it is an index only; let the MCP maintain it.

**DO NOT** capture information already documented in CLAUDE.md files.

## Quality Standards

Every memory file must have all six frontmatter fields: `name`, `description`, `type`, `tags`, `created`, `updated`.

A memory without a `description` is useless in the index — it won't surface in search results.

Prefer updating an existing memory over creating a near-duplicate. When in doubt, use `memory_search` first.
