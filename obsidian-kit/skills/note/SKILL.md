---
name: note
description: >
  Create, search, or append to notes in your Obsidian vault. Supports daily dev journal
  entries, project notes, and quick captures. Load this skill when:
  "note", "take a note", "write a note", "obsidian note", "journal entry", "dev journal",
  "capture this", "document this", "add to notes", "open note", "find note", "search vault".
user-invocable: true
argument-hint: "[new|search|append] [note title or search query]"
allowed-tools: Read, Write, Edit, Glob, Bash
---

# Obsidian Note

## Core Principles

1. **Vault path is the root** — all paths are relative to `OBSIDIAN_VAULT_PATH`. Never write outside it.
2. **Plain Markdown** — notes are `.md` files with optional YAML frontmatter. No proprietary syntax except `[[wikilinks]]`.
3. **Organised, not deep** — use the configured folders; don't invent new folder hierarchies.
4. **Dev journal is date-stamped** — daily journal entries go to `{DEV_FOLDER}/{YYYY-MM-DD}.md`, appended if it exists.

## Patterns

### Read Config

```bash
VAULT=$(grep "^OBSIDIAN_VAULT_PATH=" ~/.claude/obsidian-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEV_FOLDER=$(grep "^OBSIDIAN_DEV_FOLDER=" ~/.claude/obsidian-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEV_FOLDER=${DEV_FOLDER:-Dev}
PROJECTS_FOLDER=$(grep "^OBSIDIAN_PROJECTS_FOLDER=" ~/.claude/obsidian-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
PROJECTS_FOLDER=${PROJECTS_FOLDER:-Projects}

# Project-level (optional)
PROJECT_FOLDER=$(grep "^OBSIDIAN_PROJECT_FOLDER=" .claude/obsidian.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
NOTE_FOLDER="${PROJECT_FOLDER:-${PROJECTS_FOLDER}}"
```

### New Note

Determine the target folder from context:
- If "journal" or "today" → `{VAULT}/{DEV_FOLDER}/{YYYY-MM-DD}.md`
- If project context is active → `{VAULT}/{NOTE_FOLDER}/{kebab-title}.md`
- Otherwise → ask user which folder

Frontmatter template:
```yaml
---
date: {YYYY-MM-DD}
tags: [{inferred tags}]
project: {project-name if known}
---

# {Title}

{content}
```

### Append to Dev Journal

If today's journal file exists → append a new `##` section with timestamp.
If not → create it with frontmatter and initial content.

```bash
JOURNAL="${VAULT}/${DEV_FOLDER}/$(date +%Y-%m-%d).md"
```

### Search Vault

```bash
grep -r "{query}" "${VAULT}" --include="*.md" -l
# Show matching filenames + first matching line
```

### Open/Show Note

Read the file and display its content.

## Decision Guide

| Input | Action |
|-------|--------|
| `new` or "write a note" | Prompt for title/content, determine folder, create |
| `new journal` | Append to today's dev journal |
| `search {query}` | Grep vault for matching notes |
| `append {title}` | Find existing note and append content |
| No argument | Ask: new note or journal entry? |

## Execution

Parse `$ARGUMENTS`. Read config — if `OBSIDIAN_VAULT_PATH` is missing → tell user to run `/obsidian-setup`.
Verify vault directory exists before writing. Write the note.

$ARGUMENTS
