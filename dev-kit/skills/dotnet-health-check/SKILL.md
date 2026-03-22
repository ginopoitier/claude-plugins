---
name: dotnet-health-check
description: >
  Audit a .NET Clean Architecture project and produce a graded report out of 100 —
  covering architecture layers, CQRS, Result pattern, EF Core, logging, minimal APIs, and code quality.
  Load this skill when: "health check", "dotnet health check", "audit project", "project audit",
  "grade project", "code quality report", "architecture audit", "project health",
  "review project quality", "assess codebase".
user-invocable: true
argument-hint: "[optional: path to solution root]"
allowed-tools: Read, Glob, Grep, Bash
---

# .NET Health Check

## Core Principles

1. **Explore before scoring** — Glob all `.csproj` files first to map the layer structure. Score based on what you actually find, not assumptions.
2. **Reference exact locations** — every finding cites `File.cs:Line`. No generic advice like "use AsNoTracking" — say "GetOrderHandler.cs:23 — missing AsNoTracking()".
3. **Score reflects real impact** — architecture violations weigh 25 points because they cascade. Code style is 5 points because it's fixable without risk.
4. **Critical issues block releases** — anything in the Critical Issues section is a production risk. Everything else is a recommendation.
5. **Sample, don't read everything** — read 3-5 representative handlers, 2-3 endpoints, and 1-2 configurations. Sampling catches systemic issues without consuming the full context window.

## Patterns

### Scoring Rubric

| Section | Points | Key Checks |
|---------|--------|-----------|
| Architecture | 25 | Layer separation · inward dependencies · domain purity |
| CQRS / MediatR | 20 | All ops via MediatR · separated C/Q · behaviors registered · `internal sealed` |
| Result Pattern | 15 | `Result<T>` in all handlers · no business exceptions · ProblemDetails mapping |
| EF Core | 15 | `AsNoTracking()` · `CancellationToken` · no N+1 · `IEntityTypeConfiguration<T>` |
| Logging | 10 | Serilog configured · Seq sink · structured properties · no interpolation |
| Minimal APIs | 10 | `IEndpointGroup` · auto-discovery · no fat Program.cs · validation filters |
| Code Quality | 5 | File-scoped namespaces · primary constructors · records for DTOs · no nullable suppression |

### Exploration Process

```bash
# Step 1 — Map the solution
glob "**/*.csproj"           # find all projects
glob "**/*.sln"              # find solution file

# Step 2 — Check layer structure
glob "src/**/*.csproj"       # expect Domain, Application, Infrastructure, Api
grep "ProjectReference" src/**/*.csproj  # verify dependency direction

# Step 3 — Sample handlers
glob "Application/**/Commands/**/*Handler.cs"   # sample 3 handlers
glob "Application/**/Queries/**/*Handler.cs"    # sample 2 query handlers

# Step 4 — Sample endpoints
glob "Api/Endpoints/**/*.cs"   # sample 2 endpoint groups

# Step 5 — Check configurations
glob "Infrastructure/**/Configurations/*.cs"   # IEntityTypeConfiguration<T>
glob "Application/**/Behaviors/*.cs"           # pipeline behaviors
```

### Report Format

```
## .NET Health Check — {ProjectName}

### Score: XX/100  Grade: {A/B/C/D/F}

---

### Architecture — X/25
✅ Domain has no external NuGet dependencies
✅ Application references only Domain
❌ Infrastructure references Api (src/Infrastructure/Services/UserService.cs:12 — injecting IEndpointGroup)
⚠️  Business logic found in endpoint handler (src/Api/Endpoints/Orders/OrderEndpoints.cs:67)

### CQRS / MediatR — X/20
✅ All operations route through MediatR (ISender)
✅ Handlers are internal sealed
❌ IMediator injected instead of ISender (src/Api/Endpoints/Products/ProductEndpoints.cs:8)
⚠️  CancellationToken missing on 2 handlers

### Result Pattern — X/15
✅ All command handlers return Result<T>
✅ Errors mapped to ProblemDetails in endpoints
❌ Exception thrown for not-found case (src/Application/Orders/Commands/CancelOrder/CancelOrderHandler.cs:34)

### EF Core — X/15
✅ IEntityTypeConfiguration<T> used for all entities
❌ AsNoTracking() missing on 3 query handlers
❌ .Include() used in GetOrdersHandler instead of .Select() projection

### Logging — X/10
✅ Serilog configured with bootstrap logger
✅ Seq sink registered
⚠️  String interpolation in log message (src/Application/Products/CreateProductHandler.cs:45)

### Minimal APIs — X/10
✅ IEndpointGroup pattern with auto-discovery
✅ No business logic in Program.cs
⚠️  Validation not applied to POST endpoint body (CreateOrderEndpoint.cs)

### Code Quality — X/5
✅ File-scoped namespaces throughout
✅ Records used for DTOs and commands
⚠️  2 uses of nullable suppression (!)

---

### Critical Issues 🔴 (fix before next release)
1. src/Infrastructure/Services/UserService.cs:12 — Infrastructure references Api layer.
   This reverses the dependency direction. Move endpoint logic to the Api project.
2. src/Application/Orders/Commands/CancelOrder/CancelOrderHandler.cs:34 — throws NotFoundException
   for an expected not-found case. Use OrderErrors.NotFound (Result pattern).

### Recommendations 🟡 (address soon)
1. GetOrdersHandler.cs:23 — add AsNoTracking() to read query (performance impact at scale).
2. GetProductsHandler.cs:41 — replace .Include(p => p.Category) with .Select() projection.

### Suggestions 🔵 (nice to have)
1. Add ValidationFilter<T> to POST endpoints for request body validation.
2. Consider adding TransactionBehavior to command pipeline for consistency.
```

### Grade Scale

```
90-100 → A  (production-ready, exemplary patterns)
75-89  → B  (good, minor issues to address)
60-74  → C  (working but significant technical debt)
40-59  → D  (architectural problems that compound over time)
0-39   → F  (fundamental issues — consider refactor before new features)
```

## Anti-patterns

### Generic Findings Without File References

```
# BAD — non-actionable
"Some handlers are missing AsNoTracking()."
"There is business logic in wrong places."

# GOOD — exact location and fix
"GetOrderHandler.cs:23 — add .AsNoTracking() after db.Orders.
 GetProductsHandler.cs:41 — replace .Include() with .Select() projection."
```

### Reading Every File Instead of Sampling

```
# BAD — reads 200 handler files, burns context window
Read: every file in Application/

# GOOD — sample 3-5 to detect systemic patterns
Glob: Application/**/Commands/**/*Handler.cs → read 3
Glob: Application/**/Queries/**/*Handler.cs → read 2
→ systemic issues repeat; 5 samples are sufficient to score
```

### Marking All Issues as Critical

```
# BAD — everything is 🔴
🔴 Missing file-scoped namespaces
🔴 No XML doc comments
🔴 AsNoTracking() missing

# GOOD — severity matches business impact
🔴 Architecture violation (cascades across entire codebase)
🟡 AsNoTracking() missing (performance at scale)
🔵 Missing file-scoped namespaces (style)
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Full project audit | Explore all 5 steps, score all 7 sections |
| Quick architecture check only | Glob .csproj files, check references, score Architecture section |
| Post-onboarding baseline | Run full audit, save report, re-run in 30 days |
| Score < 60 (D/F) | Flag Critical Issues first, recommend refactor sprint before new features |
| No test projects found | Score CQRS/Result pattern lower, add "no tests" as Critical Issue |
| Greenfield project (day 1) | Defer audit until first feature is complete |

## Execution

Load before auditing:
@~/.claude/rules/clean-architecture.md
@~/.claude/rules/cqrs.md
@~/.claude/rules/result-pattern.md
@~/.claude/rules/ef-core.md
@~/.claude/rules/logging.md
@~/.claude/rules/api-design.md

### Process

1. Explore project structure with Glob — find all `.csproj` files to map layers
2. Read `*.csproj` files to check package references and project references
3. Sample handlers, endpoints, and configurations to assess quality
4. Score each section and produce the report

Reference exact file paths and line numbers. No generic advice.

$ARGUMENTS
