# Rule: Naming Conventions

## Kit Naming
- Kit directory: `kebab-case` (e.g., `dev-kit`, `kit-maker`, `data-kit`)
- Kit display name: `Title Case` (e.g., "Dev Kit", "Kit Maker")
- Kit ID in manifest: same as directory name

## Skill Naming
- Directory: `kebab-case` verb-noun (e.g., `scaffold-skill`, `kit-health-check`)
- `name` in frontmatter: same as directory
- Slash command: `/` + directory name (e.g., `/scaffold-skill`)
- User-invocable skills use imperative verbs: `scaffold`, `audit`, `check`, `package`

## Rule Naming
- File: `kebab-case` describing the domain (e.g., `skill-format.md`, `kit-structure.md`)
- H1 heading: `# Rule: Title Case Domain`

## Knowledge Naming
- File: `kebab-case` describing the topic (e.g., `kit-anatomy.md`, `cost-optimization.md`)
- H1 heading: `# Topic Name — Subtitle`

## Agent Naming
- File: `kebab-case` role (e.g., `skill-writer.md`, `kit-auditor.md`)
- Role names are nouns describing what the agent IS, not what it does

## Template Naming
- Directory: matches what it creates (e.g., `skill-template/`, `minimal-kit/`)
- Files inside: same names as the files they template

## Hook Naming
- File: `kebab-case` describing what it does (e.g., `validate-skill-frontmatter.sh`)
- Must be executable shell scripts

## Examples

```
# BAD naming
skills/scaffold_skill.md        # flat file, underscores
skills/ScaffoldSkill/SKILL.md   # PascalCase directory
rules/EFCore.md                 # PascalCase
rules/ef_core.md                # underscores

# GOOD naming
skills/scaffold-skill/SKILL.md  # kebab-case subdir with SKILL.md
rules/ef-core.md                # kebab-case
agents/skill-writer.md          # kebab-case noun role
```

## DO
- Prefer shorter names that read naturally as slash commands
- Use the same name consistently across: directory, frontmatter `name`, slash command
- Verb-first for user-invocable skills (`scaffold-`, `audit-`, `check-`)
- Noun-first for auto-active support skills (`instinct-`, `learning-`, `self-`)

## DON'T
- Don't use underscores — always hyphens
- Don't use abbreviations unless universally known (`mcp`, `di`, `api`)
- Don't name skills after their implementation (`run-mcp-query`) — name after the outcome (`check-dependencies`)
