# Obsidian Kit

> **Config:** @~/.claude/obsidian-kit.config.md — run `/obsidian-setup` if missing.

## Scope
- **Platform:** Obsidian — personal knowledge base, dev journal, learning notes
- **Covers:** Creating notes · Searching the vault · Dev session notes · Personal documentation
- **Does NOT cover:** Work documentation shared with the team — that is confluence-kit

## How it works

Obsidian notes are plain Markdown files in a local vault directory. This kit reads and writes files directly — no Obsidian app API is needed.

## Always-Active Rules

@~/.claude/rules/obsidian-kit/note-conventions.md

## Two-Level Config System

### User / Device Level — `~/.claude/obsidian-kit.config.md`
Vault location and folder structure for this machine:
- `OBSIDIAN_VAULT_PATH` · `OBSIDIAN_DEV_FOLDER` · `OBSIDIAN_PROJECTS_FOLDER`

### Project Level — `.claude/obsidian.config.md` (in each repo, optional)
Project-specific subfolder within the vault:
- `OBSIDIAN_PROJECT_FOLDER`

Run `/obsidian-setup` to configure.

When config is missing → the `check-settings` hook will prompt automatically.

## Skills Available

### Notes
- `/note` — create, search, or open a note in the vault; supports daily dev journal entries
- `/defuddle` — extract clean markdown from web pages with Defuddle CLI (instead of WebFetch)
- `/json-canvas` — create/edit `.canvas` files with nodes, edges, groups
- `/obsidian-bases` — create/edit `.base` files with views, filters, formulas (database-like note views)
- `/obsidian-cli` — interact with Obsidian vault via CLI (read, create, search, manage notes, plugin dev)
- `/obsidian-markdown` — Obsidian Flavored Markdown — wikilinks, callouts, embeds, properties

### Setup
- `/obsidian-setup` — configure vault path and folder structure
