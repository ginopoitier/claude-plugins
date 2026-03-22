# Rule: Skill Format

Every skill is a subdirectory with a single `SKILL.md` file. No flat `.md` files in `skills/` root.

## Required Frontmatter

```yaml
---
name: skill-name          # kebab-case, matches directory name
description: >            # multi-line OK. MUST include trigger keywords — this is how Claude knows to load it
  What this skill does.
  Load this skill when: "keyword1", "keyword2", ...
user-invocable: true|false  # true = user can call /skill-name
argument-hint: "[hint]"     # shown in autocomplete (required if user-invocable)
allowed-tools: Read, Write, Edit, Bash, Grep, Glob  # only tools this skill needs
---
```

## Required Body Sections (in order)

1. `# Skill Name` — H1 title
2. `## Core Principles` — 3–5 numbered rules that define the non-negotiables
3. `## Patterns` — Code examples with `// GOOD` / `// BAD` labels, named subsections
4. `## Anti-patterns` — The 3 most common mistakes with before/after examples
5. `## Decision Guide` — Markdown table: Scenario → Recommendation

## DO
- Include **5+ trigger keywords** in the description so Claude loads this skill at the right time
- Write `// GOOD` examples first, then `// BAD` examples inside Anti-patterns
- Make the Decision Guide scannable — it's the most-referenced section
- Keep Core Principles to 5 or fewer — more dilutes focus
- Use `allowed-tools` to scope the skill — don't request tools it doesn't need

## DON'T
- Don't write skills as prose documentation — every section must have code examples
- Don't skip the Anti-patterns section — it's where the real value is
- Don't use placeholder `[TODO]` content — the skill must be complete before use
- Don't duplicate content from rules files in skills — link via `@~/.claude/rules/...`
- Don't write a skill that covers multiple unrelated concerns — one skill, one domain
