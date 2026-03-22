---
name: skill-auditor
description: >
  Quality auditor for Claude Code skills. Scores a SKILL.md on 7 dimensions
  with letter grades (A-F) and produces specific, actionable fix recommendations.
  Load this skill when: "audit skill", "review skill", "skill quality", "check skill",
  "skill score", "is this skill good", "skill review", "validate SKILL.md".
user-invocable: true
argument-hint: "[path to skill or skill name]"
allowed-tools: Read, Grep, Glob
---

# Skill Auditor

## Core Principles

1. **Data-driven scoring** — Every grade is backed by a specific count or observation from the SKILL.md content. "Looks good" is not a grade.
2. **Fix before shipping** — Any dimension graded C or below is a blocker. Skills with gaps become liabilities: Claude loads them and gets bad guidance.
3. **Trigger keywords are the most critical dimension** — A skill with no trigger keywords is invisible. Score this first.
4. **Actionable output** — Every finding comes with the exact text to add or change.

## Patterns

### 7-Dimension Skill Audit

**Dimension 1: Frontmatter Completeness**

| Grade | Criteria |
|-------|----------|
| A | All 5 fields present and correct: name, description, user-invocable, argument-hint, allowed-tools |
| B | 4/5 fields present |
| C | Missing argument-hint or allowed-tools |
| D | Missing user-invocable |
| F | Missing name or description |

**Dimension 2: Trigger Keywords**

| Grade | Criteria |
|-------|----------|
| A | 8+ specific, natural-language keywords in description |
| B | 5–7 keywords |
| C | 3–4 keywords |
| D | 1–2 keywords |
| F | No trigger keywords in description |

**Dimension 3: Core Principles**

| Grade | Criteria |
|-------|----------|
| A | 4–5 principles, each with a one-sentence rationale explaining *why* |
| B | 3–4 principles with rationale |
| C | 3 principles, no rationale |
| D | 1–2 principles |
| F | Missing section |

**Dimension 4: Code Examples (Patterns section)**

| Grade | Criteria |
|-------|----------|
| A | 4+ named subsections, each with runnable code and inline comments |
| B | 3 subsections with code |
| C | 2 subsections, code is pseudocode or incomplete |
| D | 1 subsection |
| F | No code examples |

**Dimension 5: Anti-patterns**

| Grade | Criteria |
|-------|----------|
| A | 3 anti-patterns with `// BAD` + `// GOOD` pairs and explanation |
| B | 2 anti-patterns with pairs |
| C | 2 anti-patterns, no code examples |
| D | 1 anti-pattern |
| F | Missing section |

**Dimension 6: Decision Guide**

| Grade | Criteria |
|-------|----------|
| A | 6+ rows, each actionable, covers the important split decisions |
| B | 4–5 rows |
| C | 3 rows |
| D | 1–2 rows |
| F | Missing section |

**Dimension 7: Scope Hygiene**

| Grade | Criteria |
|-------|----------|
| A | `allowed-tools` lists only tools actually used in examples; single coherent domain |
| B | One unnecessary tool in allowed-tools |
| C | Over-broad allowed-tools (e.g., `Read, Write, Edit, Bash, Grep, Glob, WebSearch`) |
| D | Skill covers 2+ unrelated domains |
| F | allowed-tools: `*` or missing |

### Audit Report Format

```
## Skill Audit: {skill-name}

| Dimension | Grade | Finding |
|-----------|-------|---------|
| Frontmatter | A | All 5 fields present |
| Trigger Keywords | C | Only 3 keywords — need 5+ |
| Core Principles | B | 3 principles with rationale — add 1 more |
| Code Examples | A | 4 subsections with full examples |
| Anti-patterns | B | 2 anti-patterns — add a third |
| Decision Guide | C | 3 rows — expand to 5+ |
| Scope Hygiene | A | Minimal tools, single domain |

### GPA: 3.0 (B-)

### Blockers (fix before shipping)
1. **Trigger Keywords (C):** Add these 2 keywords to description:
   - "AddHttpClient" (users say this when setting up HTTP clients)
   - "socket exhaustion" (users say this when they have the problem)

2. **Decision Guide (C):** Add these rows:
   - `HttpClient in singleton → keyed client with AddAsKeyed(ServiceLifetime.Singleton)`
   - `Need auth injection → DelegatingHandler with AddHttpMessageHandler<AuthHandler>()`
```

### GPA Calculation

Convert: A=4.0, B=3.0, C=2.0, D=1.0, F=0.0

| GPA | Assessment |
|-----|-----------|
| 3.5–4.0 | Ship it |
| 3.0–3.4 | Ship with minor fixes noted |
| 2.5–2.9 | Fix C grades before shipping |
| < 2.5 | Do not ship — major rework needed |

## Anti-patterns

### Don't Grade Without Reading the Full File

```
# BAD — skimming
"Looks like it has examples, I'll give it a B"

# GOOD — systematic count
Trigger keywords: counted 4 → Grade C
Code examples: 3 subsections, 2 have full code → Grade B
Anti-patterns: 1 example, no BAD/GOOD pair → Grade D
```

### Don't Give Vague Fix Recommendations

```
# BAD — vague
"Add more trigger keywords"
"Improve the anti-patterns section"

# GOOD — specific, copy-paste ready
'Add to description: "AddHttpClient", "socket exhaustion", "IHttpClientFactory"'
'Add anti-pattern: Don't create HttpClient per request — show using var client = new HttpClient() vs factory.CreateClient("name")'
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| New skill just written | Audit immediately, fix blockers before merging |
| GPA < 2.5 | Do not ship — return for rework |
| Trigger keywords C or below | This is the #1 fix — skill is invisible without them |
| Code examples C or below | Add runnable examples — prose is not enough |
| Scope Hygiene D or below | Split into two skills or remove unused tools |
| Auditing for a team | Share the report with specific file:line fix locations |
