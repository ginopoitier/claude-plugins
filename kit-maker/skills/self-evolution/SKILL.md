---
name: self-evolution
description: >
  Meta-skill that improves the kit itself based on observed usage patterns.
  Analyzes kit-building sessions to find repeated patterns, template gaps, and
  rule weaknesses, then proposes concrete kit improvements.
  Load this skill when: "improve the kit", "update templates", "kit is outdated",
  "evolve kit", "kit patterns", "what did we learn", "kit retrospective",
  "update kit from session", "kit self-improvement".
user-invocable: true
argument-hint: "[session summary or area to improve]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# Self-Evolution

## Core Principles

1. **Observe patterns, don't react to one-offs** — A single correction changes MEMORY.md. A pattern repeated 3+ times across sessions changes the kit itself (templates, rules, knowledge docs).
2. **Separate signal from noise** — Not every session insight is worth baking in. Filter: does this generalize to future kit-building sessions? If yes, it belongs in the kit.
3. **Evolve templates before rules** — Templates are used as starting points. Outdated templates generate bad output on every use. Update them first.
4. **Version before evolving** — Before changing a template or rule, note the previous version and why it changed. This is the kit's institutional memory.

## Patterns

### End-of-Session Kit Retrospective

Run this after completing a kit-building or skill-writing session.

**Step 1: Extract session patterns**
Ask:
- What did I generate that I had to rewrite or fix immediately?
- What questions did the user ask that I couldn't answer from existing knowledge?
- What patterns did I repeat across multiple files (could become a template)?
- What corrections did the user make that revealed a gap in the rules?

**Step 2: Classify each finding**

```
Type A — One-time correction → goes to MEMORY.md via self-correction-loop
Type B — Repeated pattern (3+ uses) → update template
Type C — Knowledge gap → create or update knowledge doc
Type D — Rule violation → strengthen the relevant rule
Type E — Missing skill → create new skill via scaffold-skill
```

**Step 3: Apply changes in priority order**

```
Priority 1: Template updates (highest ROI — affects all future output)
Priority 2: Rule updates (prevents recurring mistakes)
Priority 3: New knowledge docs (fills gaps that caused friction)
Priority 4: New skills (adds missing capabilities)
Priority 5: MEMORY.md entries (session-specific corrections)
```

**Step 4: Log the evolution**

Append to `.claude/learning-log.md`:
```markdown
## Kit Evolution — {date}
- Updated `templates/skill-template/SKILL.md`: added Decision Guide boilerplate after 4 skills were missing it
- Added `knowledge/trigger-keyword-strategy.md`: created after 3 sessions where keywords were the main audit failure
- Strengthened `rules/skill-format.md`: added explicit "5+ trigger keywords" requirement
```

### Pattern Detection Query

When analyzing whether something is a template-worthy pattern:

```
Signal: appears in 3+ SKILL.md files I wrote this session
Signal: user corrected the same thing in 2+ files
Signal: I had to look up the same structure twice
Signal: a section I added was immediately praised or reused

Noise: user preference that doesn't generalize
Noise: domain-specific code that only appears once
Noise: correction based on external context I didn't have
```

### Template Update Protocol

```
1. Read the current template
2. Identify what boilerplate was added manually in 3+ files
3. Add that boilerplate to the template
4. Add a comment: # Added {date} — reason for addition
5. Update the template's own SKILL.md with the new example
```

### Rule Strengthening Protocol

When a rule was violated repeatedly:

```
# Before (weak)
## DO
- Write trigger keywords in the description

# After (specific, with example)
## DO
- Include 5+ trigger keywords as quoted strings in the `description` field:
  `Load this skill when: "keyword1", "keyword2", "keyword3", "keyword4", "keyword5"`
  These must match the exact phrases users say. "HttpClient" not "HTTP client pattern".
```

### Cross-Kit Learning

When you've built 3+ kits and notice patterns across all of them:

```
Pattern across kits: Every kit needed a /health-check skill
→ Add health-check scaffold to templates/full-kit/
→ Add "include a domain health-check skill" to rules/kit-structure.md

Pattern across kits: Users always ask about install process
→ Update scaffold-kit to generate README.md with install instructions
→ Add install.sh generation to kit-packager skill
```

## Anti-patterns

### Don't Evolve on Single-Use Patterns

```
# BAD — updating template for one-time need
User asked for a specific Neo4j pattern → add it to every skill template

# GOOD — only generalize what recurs
Same trigger keyword failure in 4 skills this session → update skill template
to include "Load this skill when:" boilerplate pre-filled
```

### Don't Lose Version History

```
# BAD — silently overwriting
Edit templates/skill-template/SKILL.md → overwrite without noting change

# GOOD — log the evolution
"Updated skill-template Decision Guide: added 2 default rows after
finding 5/7 skills this session were missing them (2026-03-21)"
→ append to learning-log.md
→ then update the template
```

### Don't Let Evolution Drift from Domain

```
# BAD — adding everything that seemed useful
Kit is for data science → add Python async patterns because they came up once

# GOOD — stay in domain
Kit is for data science → add data pipeline patterns because they came up in 4/5 sessions
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Same mistake in 3+ files this session | Update template |
| User corrected same thing twice | Strengthen rule |
| Question I couldn't answer from kit | Create knowledge doc |
| Missing capability (user asked, I couldn't do it) | Scaffold new skill |
| One-time correction | MEMORY.md via self-correction-loop |
| Pattern appearing across 3 different kits | Update scaffold-kit templates |
| After every kit-building session | Log to learning-log.md |
| Quarterly review | Full kit retrospective across all sessions |
