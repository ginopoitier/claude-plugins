---
name: kit-health-check
description: >
  Full audit of a Claude Code kit for completeness, quality, and installability.
  Checks structure, skill coverage, rule quality, knowledge gaps, manifest validity,
  and produces a graded report with actionable recommendations.
  Load this skill when: "kit health", "audit kit", "kit quality", "is my kit ready",
  "kit complete", "check kit", "kit gaps", "kit review", "before publishing kit".
user-invocable: true
argument-hint: "[path to kit directory]"
allowed-tools: Read, Glob, Grep, Bash
---

# Kit Health Check

## Core Principles

1. **Structure before content** — A kit with perfect skills but missing `plugin.json` is uninstallable. Check structure first.
2. **Coverage over depth** — A kit missing a critical domain skill is worse than a kit with imperfect skills. Map coverage gaps first.
3. **Meta-skills are non-negotiable** — Any kit missing context-discipline, model-selection, and instinct-system gets an automatic grade cap of C.
4. **Installability is the final gate** — The kit must be installable by someone who has never seen it before. Test the manifest and install paths.

## Patterns

### 8-Dimension Kit Health Assessment

**Dimension 1: Structure Completeness**

Check for required files/dirs:

```bash
# Required
ls kit-name/CLAUDE.md
ls kit-name/.claude-plugin/plugin.json
ls kit-name/rules/
ls kit-name/skills/
ls kit-name/hooks/check-settings.sh

# Recommended
ls kit-name/knowledge/
ls kit-name/templates/
ls kit-name/agents/
ls kit-name/config/kit.config.template.md
```

| Grade | Criteria |
|-------|----------|
| A | All 5 required + 4 recommended present |
| B | All 5 required + 2–3 recommended |
| C | All 5 required only |
| D | Missing 1 required item |
| F | Missing 2+ required items |

**Dimension 2: CLAUDE.md Quality**

| Grade | Criteria |
|-------|----------|
| A | All sections present, all referenced files exist, skills index complete |
| B | All sections, 1–2 referenced files missing |
| C | Missing a section (Meta, Integrations) |
| D | Skills index missing or heavily incomplete |
| F | CLAUDE.md is a stub |

**Dimension 3: Meta-skill Coverage**

Required: context-discipline, model-selection, verification-loop, instinct-system, self-correction-loop

| Grade | Criteria |
|-------|----------|
| A | All 5 meta-skills present |
| B | 4/5 present |
| C | 3/5 present |
| D | Only 1–2 present |
| F | No meta-skills |

**Dimension 4: Domain Skill Coverage**

Assess against the kit's stated domain: are the top 5–8 use cases covered by skills?

| Grade | Criteria |
|-------|----------|
| A | 90%+ of primary use cases have a dedicated skill |
| B | 75–89% coverage |
| C | 50–74% coverage |
| D | 25–49% coverage |
| F | < 25% coverage |

**Dimension 5: Skill Quality (average of all skills)**

Run `/skill-auditor` on each skill, average the GPA.

| Grade | Criteria |
|-------|----------|
| A | Average GPA ≥ 3.5 |
| B | 3.0–3.4 |
| C | 2.5–2.9 |
| D | 2.0–2.4 |
| F | < 2.0 |

**Dimension 6: Knowledge Coverage**

| Grade | Criteria |
|-------|----------|
| A | Every major skill has a corresponding deep-reference knowledge doc |
| B | 75%+ of complex skills have knowledge docs |
| C | Only 1–2 knowledge docs for a large kit |
| D | No knowledge docs |
| F | n/a (too small to need them) |

**Dimension 7: Installability**

| Grade | Criteria |
|-------|----------|
| A | manifest valid JSON, all install paths use `~/.claude/`, all referenced files exist |
| B | Manifest valid, 1 install path issue |
| C | Manifest valid but incomplete (missing some install paths) |
| D | Manifest has syntax errors |
| F | No manifest |

**Dimension 8: Hooks & Automation**

| Grade | Criteria |
|-------|----------|
| A | Relevant hooks for the domain (e.g., validate-on-write), hooks are executable |
| B | 1 hook present |
| C | No hooks but domain would benefit from them |
| D | Hooks referenced in manifest but files missing |
| F | n/a (simple kit, no hooks needed) |

### Quick Audit Checklist (Pre-Publish Gate)

Run this before publishing any kit version:

```bash
# 1. Required files present?
ls kit-name/CLAUDE.md kit-name/.claude-plugin/plugin.json kit-name/rules/ kit-name/skills/ kit-name/hooks/check-settings.sh

# 2. Manifest valid JSON?
python3 -m json.tool kit-name/.claude-plugin/plugin.json > /dev/null && echo "✓ JSON valid"

# 3. All skills have frontmatter?
grep -rL "^name:" kit-name/skills/*/SKILL.md

# 4. All skills have trigger keywords?
grep -rL "Load this skill when" kit-name/skills/*/SKILL.md

# 5. All CLAUDE.md @references resolve?
grep "@~/.claude/" kit-name/CLAUDE.md | while read ref; do
  path="${ref#@}"
  [ -f "$HOME/$path" ] || echo "MISSING: $path"
done

# 6. Meta-skills present?
for skill in context-discipline model-selection verification-loop instinct-system self-correction-loop; do
  [ -d "kit-name/skills/$skill" ] && echo "✓ $skill" || echo "✗ MISSING: $skill"
done
```

### Coverage Gap Detection

Map the kit's domain to expected skills, then diff against what exists:

```
1. State the kit's primary domain in one sentence
   e.g. "This kit helps developers build, audit, and publish Claude Code kits"

2. List the 6-8 most common user tasks in that domain
   e.g. Create a kit / Create a skill / Audit quality / Package for distribution / Configure post-install / Learn patterns

3. For each task, check: is there a dedicated skill?
   - /scaffold-kit  ✓   (Create a kit)
   - /scaffold-skill ✓   (Create a skill)
   - /kit-health-check ✓  (Audit quality)
   - /kit-packager ✓   (Package)
   - /kit-setup ✓     (Configure)
   - /instinct-system ✓  (Learn patterns)
   Coverage: 6/6 = A

4. Grade gaps:
   - Missing 0 → A
   - Missing 1 → B
   - Missing 2 → C
   - Missing 3+ → D/F
```

### Report Card Format

```markdown
## Kit Health Report: {kit-name} v{version}

| Dimension | Grade | Key Finding |
|-----------|-------|-------------|
| Structure | A | All required + recommended dirs present |
| CLAUDE.md | B | 2 referenced rule files missing |
| Meta-skills | A | All 5 meta-skills present |
| Domain Coverage | C | Missing skills for: deployment, testing, debugging |
| Skill Quality | B | Avg GPA 3.1 across 8 skills |
| Knowledge Docs | B | 4/6 skills have knowledge doc backing |
| Installability | A | Manifest valid, all paths correct |
| Automation | B | 1 hook (validate-skill), missing auto-sync |

### Overall GPA: 3.0 (B-)

### Priority Fixes
1. **Domain Coverage (C → B):** Add skills for: deployment workflow, test patterns, debugging guide
2. **CLAUDE.md (B → A):** Create missing rule files: rules/testing.md, rules/security.md
```

## Anti-patterns

### Don't Audit Without Running the Tools

```
# BAD — reading files and guessing
"The kit looks pretty complete to me"

# GOOD — systematic check
→ ls kit-name/ → confirms structure
→ Grep frontmatter in all SKILL.md files → confirms required fields
→ diff expected skills vs actual → reveals gaps
→ Validate manifest JSON → confirms installability
```

### Grading by Gut Feel

```
# BAD — subjective assessment without evidence
"The skills look pretty good and the kit seems well-structured."
→ Grade: B+

# GOOD — evidence-based grading
→ Glob "kit-name/skills/*/SKILL.md" → 12 skills found
→ Grep "## Anti-patterns" in each → 11/12 have Anti-patterns (1 missing)
→ Grep "Load this skill when" → 12/12 have trigger keywords
→ Grep "## Decision Guide" → 10/12 have Decision Guide (2 missing)
→ Average GPA: 3.1 (B)
Evidence: "Skill Quality B — 2/12 missing Decision Guide section"
```

### Ignoring Meta-skill Gaps

```
# BAD — auditing only domain skills, missing the meta layer
Kit has 8 excellent domain skills. Grade: A-

# GOOD — meta-skills are a hard cap
Missing: context-discipline, model-selection
→ Automatic grade cap: C on Meta-skill dimension
→ Overall GPA cannot exceed B regardless of other dimensions
→ Priority Fix #1: Add context-discipline and model-selection before publishing

Why: Without meta-skills, Claude uses this kit inefficiently —
expensive models for simple tasks, fills context with unnecessary reads.
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Pre-release check | Full 8-dimension audit, fix all D/F grades |
| Quick mid-build check | Structure + Meta-skills + Installability only |
| Adding skills to existing kit | Skill Quality + Domain Coverage only |
| After major refactor | Full audit + re-check CLAUDE.md index |
| GPA < 2.5 | Do not publish — return for rework |

## Execution

Read the target kit systematically using the 8-dimension assessment above, produce the graded report card, and list priority fixes ordered by impact.

$ARGUMENTS
