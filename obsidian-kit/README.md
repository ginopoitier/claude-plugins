# obsidian-kit

Obsidian personal knowledge base toolkit for Claude Code — create, search, and manage notes in your local vault. No Obsidian app API needed; works directly with Markdown files.

## What's Included

| Category | Skills |
|----------|--------|
| Notes | `/note` — create, search, or open a note in the vault; supports daily dev journal entries |
| Setup | `/obsidian-setup` — configure vault path and folder structure |

## Install

```bash
bash install.sh
```

Then run `/obsidian-setup` in Claude Code to configure your vault path.

## How it works

Obsidian notes are plain Markdown files in a local directory. obsidian-kit reads and writes files directly — no Obsidian API or plugin required. The vault location is configured once via `/obsidian-setup`.

## Configuration

| Level | File | Contains |
|-------|------|----------|
| User / Device | `~/.claude/obsidian-kit.config.md` | Vault path, dev journal folder, projects folder |
| Project (optional) | `.claude/obsidian.config.md` | Project-specific subfolder in the vault |

## Requirements

- Claude Code 1.0.0+
- An existing Obsidian vault directory (or any Markdown folder)
