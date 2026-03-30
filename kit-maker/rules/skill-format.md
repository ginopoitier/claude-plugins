# Rule: Skill Format

Every skill is a subdirectory with a single `SKILL.md` file. No flat `.md` files in `skills/` root.

## Required Frontmatter

```yaml
---
name: skill-name              # Required. Kebab-case, matches directory name, max 64 chars.
description: >                # Required. MUST include trigger keywords — how Claude auto-loads it.
  What this skill does.
  Load this skill when: "keyword1", "keyword2", ...
user-invocable: true|false    # true = user can call /skill-name (default: true)
argument-hint: "[hint]"       # shown in autocomplete (required if user-invocable)
allowed-tools: Read, Write, Edit, Bash, Grep, Glob  # only tools this skill needs
---
```

## Optional Frontmatter Fields

```yaml
disable-model-invocation: true  # Prevent Claude from auto-loading; user must invoke explicitly
model: claude-sonnet-4-6        # Override session model for this skill's execution
effort: high                    # Override effort level: low|medium|high|max
context: fork                   # Run skill in a forked subagent context
agent: Explore                  # Which subagent type to use with context:fork (Explore|Plan|general-purpose)
paths: "src/**/*.ts"            # Glob patterns — only activate skill when matching files are in context
shell: bash                     # Shell for !`command` blocks: bash or powershell
hooks:                          # Hooks scoped to this skill only (active while skill is running)
  PostToolUse:
    - matcher: "Write|Edit"
      hooks:
        - type: command
          command: "npx prettier --write $FILE"
```

## String Substitutions

| Variable | Description |
|----------|-------------|
| `$ARGUMENTS` | All arguments passed after the skill name |
| `$ARGUMENTS[0]`, `$0` | First argument |
| `$ARGUMENTS[1]`, `$1` | Second argument |
| `${CLAUDE_SESSION_ID}` | Current session ID |
| `${CLAUDE_SKILL_DIR}` | Absolute path to this skill's directory |

## Dynamic Context (Shell Commands)

Use `` !`command` `` to inject live output into the skill before Claude sees it:

```markdown
Current diff: !`git diff --staged`
Open issues: !`gh issue list --state open --limit 10`
```

The command runs immediately; its output replaces the backtick block.

## Required Body Sections (in order)

1. `# Skill Name` — H1 title
2. `## Core Principles` — 3–5 numbered rules that define the non-negotiables
3. `## Patterns` — Code examples with `// GOOD` / `// BAD` labels, named subsections
4. `## Anti-patterns` — The 3 most common mistakes with before/after examples
5. `## Decision Guide` — Markdown table: Scenario → Recommendation
6. `## Execution` — One sentence: what Claude does when this skill is invoked (the entry point)
7. `$ARGUMENTS` — Literal token on its own line at the very end; receives user arguments at runtime

## DO
- Include **5+ trigger keywords** in the description so Claude loads this skill at the right time
- Write `// GOOD` examples first, then `// BAD` examples inside Anti-patterns
- Make the Decision Guide scannable — it's the most-referenced section
- Keep Core Principles to 5 or fewer — more dilutes focus
- Use `allowed-tools` to scope the skill — don't request tools it doesn't need
- End every skill with `$ARGUMENTS` on its own line after `## Execution`

## DON'T
- Don't write skills as prose documentation — every section must have code examples
- Don't skip the Anti-patterns section — it's where the real value is
- Don't use placeholder `[TODO]` content — the skill must be complete before use
- Don't duplicate content from rules files in skills — link via `@~/.claude/rules/...`
- Don't write a skill that covers multiple unrelated concerns — one skill, one domain
