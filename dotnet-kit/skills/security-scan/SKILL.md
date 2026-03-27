---
name: security-scan
description: >
  Comprehensive security audit combining vulnerability scanning, package audit, secrets
  detection, and authorization coverage check. Produces a release-gate report.
  Load this skill when: "security-scan", "security audit", "hardcoded secrets",
  "missing authorization", "SQL injection", "vulnerable packages", "OWASP",
  "authorization coverage", "secrets detection", "security review", "pen test prep",
  "release security check", "CVE scan".
user-invocable: true
argument-hint: "[project]"
allowed-tools: Read, Bash, Glob, Grep
---

# Security Scan

## Core Principles

1. **Critical findings block release** — SQL injection, hardcoded secrets, and missing auth on sensitive endpoints are not warnings. They are release blockers. Report them at the top.
2. **Scan all five dimensions** — Vulnerability patterns, package CVEs, authorization coverage, secrets detection, and security headers. Skipping any dimension gives a false sense of security.
3. **False positives are noise — verify before flagging** — A variable named `password` in a test fixture is not a secret. Read the context before escalating a grep hit to a finding.
4. **Authorization coverage is positive-assertion** — It is not enough to find auth annotations. Verify that ALL endpoints require auth by default and that any `AllowAnonymous` is intentional and documented.
5. **Surface the fix alongside the finding** — Every finding includes what the vulnerable line is AND what the correct version looks like. Reviewers should be able to fix it on the spot.

## Patterns

### Secrets Detection

```bash
# Hardcoded credentials — high false-positive risk, always verify context
grep -rn "password\s*=\s*['\"][^'\"]\+" src/ --include="*.cs" -i
grep -rn "apikey\s*=\s*['\"][^'\"]\+" src/ --include="*.cs" -i
grep -rn "secret\s*=\s*['\"][^'\"]\+" src/ --include="*.cs" -i
grep -rn "token\s*=\s*['\"][^'\"]\+" src/ --include="*.cs" -i

# Connection strings with embedded credentials
grep -rn "Password=" src/ --include="appsettings*.json"
grep -rn "User Id=" src/ --include="appsettings*.json"
```

```csharp
// BAD — hardcoded credential
var apiKey = "sk-live-AbCdEfGhIjKlMnOpQrStUv";

// GOOD — from configuration
var apiKey = configuration["Stripe:ApiKey"];
// Stored in User Secrets (dev) or Azure Key Vault / env variable (prod)
```

### SQL Injection Detection

```bash
# Raw SQL with string interpolation or concatenation
grep -rn "ExecuteSqlRaw\|FromSqlRaw\|ExecuteSqlInterpolated" src/ --include="*.cs"
grep -rn "string\.Format.*sql\|\"SELECT.*\" +" src/ --include="*.cs" -i
```

```csharp
// BAD — SQL injection via string interpolation
var sql = $"SELECT * FROM Orders WHERE CustomerId = '{customerId}'";
db.Orders.FromSqlRaw(sql);

// GOOD — parameterized (EF Core LINQ is safe)
db.Orders.Where(o => o.CustomerId == customerId);

// GOOD — if raw SQL is needed, use parameters
db.Orders.FromSqlRaw("SELECT * FROM Orders WHERE CustomerId = {0}", customerId);
```

### Authorization Coverage

```bash
# Find all endpoint route definitions
grep -rn "MapGet\|MapPost\|MapPut\|MapDelete\|MapPatch" src/ --include="*.cs"

# Find endpoints with explicit authorization
grep -rn "RequireAuthorization\|\[Authorize\]" src/ --include="*.cs"

# Find endpoints explicitly opting out
grep -rn "AllowAnonymous\|\.AllowAnonymous()" src/ --include="*.cs"
```

```csharp
// GOOD — auth required on the group, explicit allow-anonymous for public endpoints
var group = app.MapGroup("/api/orders")
    .RequireAuthorization();  // ← all endpoints in this group require auth

// GOOD — explicitly allowing anonymous where intentional
app.MapGet("/api/health", HealthCheckHandler)
    .AllowAnonymous()
    .WithSummary("Public health check endpoint");

// BAD — sensitive endpoint without any auth annotation
app.MapGet("/api/admin/users", AdminGetUsersHandler);  // ← no RequireAuthorization()
```

### Security Headers Check

```csharp
// Required in Program.cs
app.UseHsts();                    // ← check present
app.UseHttpsRedirection();        // ← check present

// Check for custom security headers middleware
grep -rn "X-Content-Type-Options\|X-Frame-Options\|Content-Security-Policy" src/

// Check CORS is not overly permissive
grep -rn "AllowAnyOrigin\|AllowAnyHeader\|AllowAnyMethod" src/ --include="*.cs"
```

```csharp
// BAD — CORS wildcard with credentials (browsers block this, but it signals misconfiguration)
builder.Services.AddCors(o => o.AddPolicy("Default", p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// GOOD — explicit origin list
builder.Services.AddCors(o => o.AddPolicy("Default", p =>
    p.WithOrigins("https://app.mycompany.com")
     .WithMethods("GET", "POST", "PUT", "DELETE")
     .AllowCredentials()));
```

### Report Format

```
Security Scan Report — {date}
================================
CRITICAL — Release Blockers:
  [SQL-INJ] OrderRepository.cs:45
    db.FromSqlRaw($"SELECT * FROM Orders WHERE Id = '{id}'")
    Fix: Use LINQ: db.Orders.Where(o => o.Id == id)

  [SECRETS] appsettings.Production.json:12
    "Password": "Prod@ssw0rd123!"
    Fix: Move to Azure Key Vault / environment variable

HIGH:
  [AUTH] POST /api/admin/users — no RequireAuthorization()
    Fix: Add .RequireAuthorization("AdminPolicy") to the route

WARNINGS:
  [VULN-PKG] Newtonsoft.Json 12.0.3 — CVE-2024-21907 (High)
    Fix: dotnet add package Newtonsoft.Json --version 13.0.3
  [CORS] AllowAnyOrigin used — verify this is intentional for public API

PASSED:
  ✅ No path traversal patterns found
  ✅ HTTPS redirect configured (app.UseHttpsRedirection())
  ✅ HSTS configured (app.UseHsts())
  ✅ No Thread.Sleep / synchronous blocking in async paths
  ✅ No dangerous deserialization patterns

Summary: 2 critical, 1 high, 2 warnings
Verdict: NOT READY FOR RELEASE — fix critical issues first
```

## Anti-patterns

### Treating All Grep Hits as Findings

```bash
# BAD — flagging test fixtures as security vulnerabilities
grep -rn "password" src/ --include="*.cs"
→ tests/Builders/UserBuilder.cs:12 — password = "test-password"  ← FALSE POSITIVE
# This is test data, not a production secret

# GOOD — verify context before flagging
# Check: is this in a test project? Is it a variable name vs a value?
# Is the value a placeholder like "TestPassword123!" vs a real credential?
```

### Checking Direct Dependencies Only for CVEs

```bash
# BAD — misses transitive vulnerabilities
dotnet list package --vulnerable
→ 0 vulnerabilities  ← FALSE NEGATIVE

# GOOD — always include transitive
dotnet list package --vulnerable --include-transitive
→ 2 vulnerabilities in indirect dependencies
```

### Reporting Without the Fix

```
// BAD — tells you what's wrong but not how to fix it
[AUTH] POST /api/admin/users — missing authorization

// GOOD — shows the problem AND the fix
[AUTH] POST /api/admin/users — missing authorization
  Current: app.MapPost("/api/admin/users", CreateUserHandler);
  Fix:     app.MapPost("/api/admin/users", CreateUserHandler).RequireAuthorization("AdminPolicy");
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| SQL injection pattern found | Critical — release blocker, show fix inline |
| Hardcoded credential found | Critical — release blocker, show config alternative |
| Missing auth on data endpoint | High — release blocker |
| Missing auth on health/metrics endpoint | Check if intentional; warn if not documented |
| Vulnerable package (Critical/High) | High — include fix command |
| Vulnerable package (Moderate/Low) | Warning — include fix command |
| AllowAnyOrigin in CORS | Warning — verify it's intentional for public APIs |
| Missing HTTPS redirect | Warning — add `app.UseHttpsRedirection()` |
| Missing HSTS | Warning — add `app.UseHsts()` |
| No findings | Still output "Passed" checklist — proves the scan ran |
| Test fixture has "password" variable | Skip — verify context before flagging |

## Execution

You are executing the /security-scan command. Run a comprehensive security audit of the codebase.

### What to Run (in order)

**1. MCP Vulnerability Scanner**
Call `scan_security_vulnerabilities` on the devkit-mcp if available:
- SQL injection patterns
- Hardcoded secrets/connection strings
- Path traversal vulnerabilities
- Dangerous deserialization
- Reflection misuse

**2. NuGet Package Audit**
```bash
dotnet list package --vulnerable --include-transitive
```
- Flag any vulnerable packages (severity: critical > high > moderate)

**3. Authorization Coverage**
Call `find_missing_authorization` on the devkit-mcp if available, otherwise grep:
- Identify endpoints without `RequireAuthorization()` or `[Authorize]`
- Flag endpoints that deal with user data but lack auth

**4. Secrets Detection (Grep)**
```bash
grep -r "password\s*=" src/ --include="*.cs" -i
grep -r "ApiKey\s*=" src/ --include="*.cs" -i
grep -r "ConnectionString" src/ --include="appsettings*.json"
```
Always read the context of any hit before escalating to a finding.

**5. HTTPS / Security Headers Check**
- Check for `app.UseHttpsRedirection()` in Program.cs
- Check for `app.UseHsts()`
- Look for CORS configuration — flag `AllowAnyOrigin` with credentials
- Check rate limiting is applied to auth endpoints

$ARGUMENTS
