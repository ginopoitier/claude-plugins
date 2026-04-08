# Rule: Obsidian Agent Routing

## DO
- Spawn the **`vault-curator`** agent for any vault-wide audit, health check, or curation task
- Give the agent the vault path from config so it doesn't have to re-derive it

## DON'T
- Don't do vault-wide orphan/link/tag analysis in the main context — it produces verbose output that burns context
- Don't spawn the agent for single-note operations — read/write directly

## Available Agents

- `vault-curator` — vault audit: orphaned notes, broken wikilinks, tag consistency, folder health report

## Trigger Phrases → vault-curator

| User says | Action |
|-----------|--------|
| "audit the vault" | Spawn vault-curator |
| "vault health" / "vault health check" | Spawn vault-curator |
| "find orphaned notes" | Spawn vault-curator |
| "fix broken links" | Spawn vault-curator |
| "organize the vault" | Spawn vault-curator |
| "curate the vault" | Spawn vault-curator |
| "clean up the vault" | Spawn vault-curator |
