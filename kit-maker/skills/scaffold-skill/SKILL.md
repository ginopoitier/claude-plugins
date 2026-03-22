---
name: scaffold-skill
description: >
  Interactive wizard for creating a new Claude Code skill with all required sections,
  trigger keywords, code examples, anti-patterns, and decision guide.
  Load this skill when: "create a skill", "new skill", "scaffold skill", "add skill",
  "write a skill", "skill template", "SKILL.md".
user-invocable: true
argument-hint: "[skill name and domain]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# Scaffold Skill

## Core Principles

1. **Interactive before writing** — Ask domain + trigger keywords + 3 key patterns before generating any file. A skill written without context will be generic and useless.
2. **Code examples are mandatory** — Every skill section needs runnable code. Prose without code is documentation, not a skill.
3. **Trigger keywords determine value** — A skill nobody loads is a skill that doesn't exist. Get the trigger keywords right.
4. **Quality-gated** — Run `/skill-auditor` immediately after creation. Fix any gaps before marking done.

## Patterns

### Skill Creation Wizard

**Step 1: Gather context**
Ask:
- Skill name (will become directory name and slash command)
- Domain: what technology/pattern does this cover?
- When should Claude load this automatically? (trigger keywords — aim for 8+)
- Is this user-invocable via `/skill-name`? If yes, what argument does it take?
- Which tools does it need? (be minimal — only what it actually uses)
- 3 most important patterns (describe in plain language — will become code examples)
- 3 most common mistakes (describe — will become anti-patterns)

**Step 2: Generate the skill**

```markdown
---
name: {skill-name}
description: >
  {one-line description}.
  Load this skill when: "{keyword1}", "{keyword2}", "{keyword3}",
  "{keyword4}", "{keyword5}", "{keyword6}", "{keyword7}", "{keyword8}".
user-invocable: {true|false}
argument-hint: "[{hint}]"
allowed-tools: {only-what-is-needed}
---

# {Skill Name}

## Core Principles

1. **{Principle 1}** — {one-sentence rationale}
2. **{Principle 2}** — {one-sentence rationale}
3. **{Principle 3}** — {one-sentence rationale}

## Patterns

### {Pattern Name}

```{language}
// GOOD — {why this is right}
{code example}

// Comparison usage
{usage example}
```

### {Pattern Name 2}

```{language}
{code example with inline comments}
```

## Anti-patterns

### Don't {Common Mistake 1}

```{language}
// BAD — {why this is wrong}
{bad example}

// GOOD — {why this is right}
{good example}
```

### Don't {Common Mistake 2}

```{language}
// BAD
{bad example}

// GOOD
{good example}
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| {scenario 1} | {recommendation} |
| {scenario 2} | {recommendation} |
| {scenario 3} | {recommendation} |
| {scenario 4} | {recommendation} |
```

**Step 3: Audit immediately**
After writing, invoke the `skill-auditor` pattern (see `/skill-auditor`) to score it.

### Adding a Skill to an Existing Kit

After creating the skill:
1. Add it to `CLAUDE.md` under the appropriate category
2. Verify the slash command works: `/skill-name`
3. If the kit is installed: copy `SKILL.md` to `~/.claude/skills/{skill-name}/`

## Anti-patterns

### Don't Write Skills Without Trigger Keywords

```yaml
# BAD — no trigger keywords, Claude never auto-loads this
description: >
  Handles HTTP client configuration.

# GOOD — specific keywords that match real user language
description: >
  IHttpClientFactory patterns for .NET.
  Load this skill when: "HttpClient", "IHttpClientFactory", "AddHttpClient",
  "socket exhaustion", "named client", "typed client", "DelegatingHandler".
```

### Don't Request Too Many Tools

```yaml
# BAD — requesting everything
allowed-tools: Read, Write, Edit, Bash, Grep, Glob, WebSearch, WebFetch

# GOOD — only what the skill's code examples actually use
allowed-tools: Read, Write, Edit
```

### Don't Write Skills as Documentation

```markdown
<!-- BAD — prose without code -->
## Patterns
When working with HttpClient, you should use IHttpClientFactory. This prevents
socket exhaustion and enables proper handler rotation. The factory manages...

<!-- GOOD — code-first -->
## Patterns
### Named Client with Resilience
```csharp
builder.Services.AddHttpClient("github", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
})
.AddStandardResilienceHandler();
```
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Brand new skill | Full wizard: ask → generate → audit |
| Skill for existing well-known pattern | Pre-fill from knowledge docs, skip step 1 |
| Updating existing skill | Read current SKILL.md → edit specific sections → re-audit |
| User-invocable vs auto-active | User-invocable if the user calls it explicitly; auto-active if it should load based on context |
| Choosing trigger keywords | Use the exact words users say: "how do I X", "set up X", "X isn't working" |

## Deep Reference

For full skill anatomy, quality bar, and detailed writing guide:
@~/.claude/knowledge/kit-maker/skill-writing-guide.md
