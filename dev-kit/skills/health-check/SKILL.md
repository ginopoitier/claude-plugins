---
name: health-check
description: >
  Multi-dimensional health assessment for .NET projects with letter grades (A-F)
  using Roslyn MCP tools. Evaluates 8 dimensions: build health, code quality,
  architecture, test coverage, dead code, API surface, security posture, and
  documentation. Produces a structured report card with actionable recommendations.
  Load this skill when: "health check", "how healthy is this", "project health",
  "code quality report", "grade this project", "assess codebase", "quality audit",
  "technical assessment", "codebase review", "report card".
user-invocable: true
argument-hint: "[project path or solution to assess]"
allowed-tools: Read, Write, Bash, Grep, Glob
---

# Health Check

## Core Principles

1. **Data-driven assessment** — Use MCP tools for every dimension. `get_diagnostics` for build health, `detect_antipatterns` for code quality, `detect_circular_dependencies` for architecture. Gut feeling is not a grade.

2. **Letter grades with justification** — Every dimension gets A (90+), B (80+), C (70+), D (60+), or F (<60). Every grade includes the specific data points. "B in Code Quality: 3 anti-patterns in 2,400 lines (1.25 per 1K)" is actionable.

3. **Actionable recommendations** — Every grade below A comes with specific, prioritized fix suggestions. "Add test classes for OrderService, PaymentProcessor, and ShippingCalculator (3 production types without tests)" is actionable.

## Patterns

### 8-Dimension Health Assessment

**Dimension 1: Build Health**

| Grade | Criteria |
|-------|----------|
| A | 0 errors, 0 warnings |
| B | 0 errors, 1-5 warnings |
| C | 0 errors, 6-15 warnings |
| D | 0 errors, 16-30 warnings |
| F | Any errors, or 30+ warnings |

**Dimension 2: Code Quality**

Tool: `detect_antipatterns` | Metric: Anti-pattern count per 1K lines of code

| Grade | Criteria |
|-------|----------|
| A | 0 anti-patterns |
| B | < 0.5 per 1K lines |
| C | 0.5 - 1.5 per 1K lines |
| D | 1.5 - 3.0 per 1K lines |
| F | > 3.0 per 1K lines |

**Dimension 3: Architecture**

Tool: `get_project_graph` + `detect_circular_dependencies`

| Grade | Criteria |
|-------|----------|
| A | Correct dependency direction, 0 circular deps |
| B | Correct direction, 1-2 type-level cycles (no project cycles) |
| C | 1-2 minor direction issues, or 3-5 type-level cycles |
| D | Project-level circular dependency |
| F | Multiple project-level cycles, no discernible architecture |

**Dimension 4: Test Coverage**

Tool: `get_test_coverage_map` | Metric: % of production types with test classes

| Grade | Criteria |
|-------|----------|
| A | 90%+ types have test classes |
| B | 75-89% |
| C | 50-74% |
| D | 25-49% |
| F | < 25% |

**Dimension 5: Dead Code**

Tool: `find_dead_code` | Metric: Count of unused types, methods, properties

| Grade | Criteria |
|-------|----------|
| A | 0-2 dead symbols |
| B | 3-8 dead symbols |
| C | 9-15 dead symbols |
| D | 16-25 dead symbols |
| F | 25+ dead symbols |

**Dimension 6: Security Posture**

Tool: `dotnet list package --vulnerable` + `detect_antipatterns` (security patterns)

| Grade | Criteria |
|-------|----------|
| A | 0 vulnerable packages, no hardcoded secrets, auth on all endpoints |
| B | 0 critical/high vulns, 1-2 low/medium vulns |
| C | 1-2 medium vulns, or minor auth gaps |
| D | High-severity vuln, or missing auth on sensitive endpoints |
| F | Critical vuln, hardcoded secrets, or systemic auth gaps |

### Report Card Format

```markdown
## Project Health Report

**Project:** MyApp | **Date:** 2026-03-21 | **Assessed by:** Claude (MCP-assisted)

### Grades

| Dimension | Grade | Score | Key Finding |
|-----------|-------|-------|-------------|
| Build Health | A | 95 | 0 errors, 2 pre-existing warnings |
| Code Quality | B | 82 | 3 anti-patterns in 4.2K lines |
| Architecture | A | 92 | Clean dependency direction, 0 circular deps |
| Test Coverage | C | 68 | 34/50 production types have test classes |
| Dead Code | B | 85 | 5 unused methods identified |
| API Surface | B | 80 | 2 overexposed service types |
| Security | A | 94 | 0 vulnerable packages, auth coverage complete |
| Documentation | D | 55 | 12/30 public APIs have XML docs |

### Overall GPA: 3.0 (B-)

### Priority Recommendations

1. **Test Coverage (C -> B):** Add test classes for these 16 untested types:
   - `OrderService`, `PaymentProcessor`, `ShippingCalculator` (critical path)

2. **Documentation (D -> C):** Add XML docs to public API types

3. **Code Quality (B -> A):** Fix 3 anti-patterns:
   - `OrderService.cs:47` — Replace `DateTime.Now` with `TimeProvider.GetUtcNow()`
   - `PaymentClient.cs:23` — Replace `new HttpClient()` with `IHttpClientFactory`
```

### GPA Calculation

Convert: A=4.0, B=3.0, C=2.0, D=1.0, F=0.0. GPA = average of all dimension scores.

| GPA Range | Overall Assessment |
|-----------|--------------------|
| 3.5 - 4.0 | Excellent — production-ready |
| 3.0 - 3.4 | Good — solid foundation |
| 2.5 - 2.9 | Fair — accumulating tech debt |
| 2.0 - 2.4 | Needs Work — significant improvements required |
| < 2.0 | Critical — major structural issues |

## Anti-patterns

### Grading Without MCP Tools

```
# BAD — gut-feeling assessment
"The code looks pretty clean, I'd give it a B overall."

# GOOD — MCP-driven assessment with data
MCP: detect_antipatterns → 3 findings
MCP: get_diagnostics → 2 warnings
MCP: get_test_coverage_map → 68% coverage
"Code Quality: B (3 anti-patterns in 4.2K lines)"
```

### Recommendations Without Specifics

```
# BAD — vague
"Improve test coverage."
"Fix code quality issues."

# GOOD — specific, prioritized, estimated
"Add test classes for OrderService, PaymentProcessor, ShippingCalculator.
 These are on the critical path. Estimated effort: 4 hours."
```

## Decision Guide

| Scenario | Assessment Type | Dimensions |
|----------|----------------|------------|
| New project onboarding | Full Health Check | All 8 |
| Mid-sprint checkpoint | Quick (Build + Quality + Architecture + Tests) | 4 |
| Pre-release quality gate | Full Health Check | All 8 |
| Post-dependency update | Targeted | Build + Security |
| Tech debt prioritization | Full Health Check | All 8, focus on lowest grades |

## Execution

Run MCP tools across all 8 health dimensions, assign letter grades with data-backed justification, calculate the GPA, and output the structured report card with prioritized, actionable recommendations.

$ARGUMENTS
