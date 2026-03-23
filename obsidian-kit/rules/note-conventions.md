# Rule: Obsidian Note Conventions

## DO
- Always read `OBSIDIAN_VAULT_PATH` from `~/.claude/obsidian-kit.config.md` before writing any file — never hardcode paths
- Use plain Markdown (`.md`) files — Obsidian renders standard CommonMark
- Organise notes under the configured folders: `OBSIDIAN_DEV_FOLDER` for dev notes, `OBSIDIAN_PROJECTS_FOLDER` for project docs
- Add YAML frontmatter with `date`, `tags`, and `project` where relevant
- Use `[[wikilink]]` syntax for internal links between notes
- Name files with kebab-case: `order-service-setup-notes.md`

## DON'T
- Don't write outside `OBSIDIAN_VAULT_PATH` — always stay within the vault
- Don't store secrets or credentials in Obsidian notes
- Don't create deeply nested folder structures — flat-ish with good naming is better than deep hierarchy

## Reading Config

```bash
VAULT=$(grep "^OBSIDIAN_VAULT_PATH=" ~/.claude/obsidian-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEV_FOLDER=$(grep "^OBSIDIAN_DEV_FOLDER=" ~/.claude/obsidian-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
DEV_FOLDER=${DEV_FOLDER:-Dev}
PROJECTS_FOLDER=$(grep "^OBSIDIAN_PROJECTS_FOLDER=" ~/.claude/obsidian-kit.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
PROJECTS_FOLDER=${PROJECTS_FOLDER:-Projects}

# Project config (optional)
PROJECT_FOLDER=$(grep "^OBSIDIAN_PROJECT_FOLDER=" .claude/obsidian.config.md 2>/dev/null | cut -d= -f2- | tr -d '[:space:]')
```

## Note Frontmatter Template

```yaml
---
date: {YYYY-MM-DD}
tags: [dev, {project-name}]
project: {project-name}
---
```
