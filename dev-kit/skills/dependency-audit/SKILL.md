---
name: dependency-audit
description: >
  Audit NuGet package dependencies for vulnerabilities, outdated versions, and unused
  references. Detects CVEs, suggests safe upgrade paths, and finds transitive conflicts.
  Load this skill when: "dependency-audit", "vulnerable packages", "outdated packages",
  "NuGet audit", "CVE", "package vulnerabilities", "upgrade packages", "unused references",
  "transitive dependencies", "package conflicts", "dotnet list package", "security advisory".
user-invocable: true
argument-hint: "[outdated|vulnerable|unused|all]"
allowed-tools: Read, Bash, Glob, Grep
---

# Dependency Audit

## Core Principles

1. **Vulnerabilities block everything else** — A Critical or High CVE is a release blocker. Identify these first and report them at the top, regardless of which sub-command was run.
2. **Transitive dependencies are your responsibility too** — `--include-transitive` is always used. A vulnerability in a dependency-of-a-dependency is still your vulnerability.
3. **Major version upgrades need a human decision** — Flag them clearly with "breaking changes likely" but do not auto-apply. Minor and patch upgrades are safe to generate commands for.
4. **Unused references increase attack surface** — Every unused package is a dependency that can become vulnerable. Flag for removal.
5. **Generate the fix, not just the report** — For every vulnerability or outdated package, include the exact `dotnet add package` command needed to resolve it.

## Patterns

### Vulnerability Scan

```bash
# Always include transitive — direct packages are not the only risk
dotnet list package --vulnerable --include-transitive
```

Output interpretation:
```
> Project 'MyApp.Api' has the following vulnerable packages
   [net9.0]:
   Top-level Package              Requested   Resolved   Severity   Advisory URL
   > Newtonsoft.Json              12.0.3      12.0.3     High       https://github.com/...

# Severity levels and response:
# Critical → Release blocker. Fix before any other work.
# High     → Release blocker. Fix before any other work.
# Moderate → Track. Fix in current sprint.
# Low      → Backlog. Fix opportunistically.
```

### Outdated Package Scan

```bash
dotnet list package --outdated
```

Group results by upgrade risk:

```
MAJOR version behind (review carefully — breaking changes likely):
  Microsoft.EntityFrameworkCore.SqlServer  7.0.20 → 9.0.4
  MediatR                                  11.1.0 → 12.4.1
  → These require reviewing migration guides before upgrading

MINOR/PATCH behind (safe to upgrade):
  Serilog.AspNetCore    7.0.0 → 8.0.3
  FluentValidation      11.9.0 → 11.11.0
  → Generate dotnet add package commands for these
```

### Unused Reference Detection

For each project, read its `.csproj` and cross-reference with actual namespace usage:

```bash
# Step 1: Get all package references for a project
grep -n "PackageReference" src/MyApp.Api/MyApp.Api.csproj

# Step 2: For each package, search for its namespace in source files
# Example: check if Newtonsoft.Json is actually used
grep -rn "using Newtonsoft\|JsonConvert\|JObject\|JArray" src/MyApp.Api/ --include="*.cs"
```

Common packages that end up unused:
- `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` — added during scaffold, removed after
- `Microsoft.Extensions.Http` — often pulled transitively
- `Swashbuckle.AspNetCore` — if migrated to built-in OpenAPI

### Combined Audit Report

```
Dependency Audit — {date}
==========================

VULNERABLE PACKAGES
Critical: 0
High: 1
  → Newtonsoft.Json 12.0.3 (CVE-2024-21907) — upgrade to 13.0.3
    Fix: dotnet add package Newtonsoft.Json --version 13.0.3
Moderate: 1
  → System.Text.Json 7.0.1 (CVE-2023-XXXXX) — upgrade to 7.0.5
    Fix: dotnet add package System.Text.Json --version 7.0.5

OUTDATED PACKAGES
Major version behind (review carefully):
  → Microsoft.EntityFrameworkCore.SqlServer 7.0.20 → 9.0.4
    ⚠ Breaking changes in 8.0 and 9.0 — review migration guide first
Minor/Patch (safe to upgrade):
  → Serilog.AspNetCore 7.0.0 → 8.0.3
    Fix: dotnet add package Serilog.AspNetCore --version 8.0.3

UNUSED REFERENCES (candidates for removal)
  → MyApp.Api: Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
    (no EF error page usage found in source)

SUMMARY
Verdict: Review required — 1 High vulnerability
Blocking: Upgrade Newtonsoft.Json to 13.0.3 before release
```

### Upgrade Command Generation

After the report, generate all safe upgrade commands in one block:

```bash
# Safe upgrades (minor/patch only):
dotnet add package Serilog.AspNetCore --version 8.0.3
dotnet add package FluentValidation.DependencyInjectionExtensions --version 11.11.0

# Critical fixes:
dotnet add package Newtonsoft.Json --version 13.0.3

# After running upgrades:
dotnet restore
dotnet build
dotnet test
```

## Anti-patterns

### Auditing Only Direct Dependencies

```bash
# BAD — misses transitive vulnerabilities
dotnet list package --vulnerable
→ Shows 0 vulnerabilities (all via transitive packages)

# GOOD — always include transitive
dotnet list package --vulnerable --include-transitive
→ Shows 2 vulnerabilities in dependencies of dependencies
```

### Auto-Upgrading Major Versions

```
# BAD — blindly upgrading everything
dotnet add package MediatR --version 12.4.1
# → Breaks all handler registrations (MediatR 12 changed DI API)
# → Breaks IPipelineBehavior signatures

# GOOD — flag and explain
MediatR 11 → 12 is a major upgrade with breaking changes:
- Handler registration API changed
- IPipelineBehavior<TRequest, TResponse> parameter order changed
Review the migration guide: https://github.com/jbogard/MediatR/releases/tag/v12.0.0
Do you want me to show what needs to change before upgrading? [y/n]
```

### Reporting Without Fix Commands

```
# BAD — lists vulnerabilities but doesn't tell you what to do
High: Newtonsoft.Json 12.0.3 — CVE-2024-21907

# GOOD — includes the fix in the report
High: Newtonsoft.Json 12.0.3 — CVE-2024-21907
Safe version: 13.0.3
Fix: dotnet add package Newtonsoft.Json --version 13.0.3
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| `vulnerable` sub-command | Run `--vulnerable --include-transitive`, format by severity |
| `outdated` sub-command | Run `--outdated`, group by major vs minor/patch |
| `unused` sub-command | Read `.csproj` files, grep namespaces, flag zero-usage refs |
| `all` (default) | Run all three, produce combined report |
| Critical/High CVE found | Mark as release blocker, generate fix command immediately |
| Major version upgrade needed | Flag with breaking change warning, do not auto-generate command |
| Minor/patch upgrade available | Generate `dotnet add package` commands ready to run |
| No Jira/issue tracker | List TODOs for each finding at end of report |
| Package genuinely unused | Suggest `<PackageReference Remove="..." />` in `.csproj` |
| Transitive conflict found | Show the dependency chain that causes the conflict |

## Execution

You are executing the /dependency-audit command. Audit NuGet package dependencies.

### `/dependency-audit vulnerable`
```bash
dotnet list package --vulnerable --include-transitive
```
- Format results by severity: Critical → High → Moderate → Low
- For each vulnerability: package, version, CVE, fix version
- Immediately flag Critical and High as blocking issues

### `/dependency-audit outdated`
```bash
dotnet list package --outdated
```
- Group by: major version behind (risky upgrade) vs minor/patch (safe)
- Highlight packages with security implications in outdated versions
- Show the recommended upgrade command

### `/dependency-audit unused`
For each project:
1. Read the `.csproj` file for `<PackageReference>` entries
2. Search source files in that project for usage of each package's namespaces
3. Flag packages with no apparent usage

### `/dependency-audit all`
Run all three checks and produce a combined report. Default when no argument is given.

### Upgrade Command Generation
After the report, offer to generate the upgrade commands:
```bash
dotnet add package <name> --version <safe-version>
```
