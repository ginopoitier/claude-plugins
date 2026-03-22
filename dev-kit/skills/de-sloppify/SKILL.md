---
name: de-sloppify
description: >
  Find and fix code quality issues in C# projects. Detects antipatterns, dead code,
  magic values, TODO debt, vague naming, and convention violations.
  Load this skill when: "de-sloppify", "clean up code", "code quality", "dead code",
  "TODO debt", "magic strings", "naming issues", "sloppy code", "code review cleanup",
  "fix warnings", "code smells", "technical debt", "inconsistent naming".
user-invocable: true
argument-hint: "[file-or-directory]"
allowed-tools: Read, Write, Edit, Glob, Grep, Bash
---

# De-Sloppify

## Core Principles

1. **Triage by severity** — Group all findings into Critical (break things), Warning (will cause pain), and Style (cosmetic). Fix Critical automatically; ask before touching Style.
2. **Never delete without confirmation** — Commented-out code might be intentional. Always ask before removing a block of commented code.
3. **Fix, don't just report** — For Critical and Warning findings, offer an in-place fix immediately. A report with no action taken is half a job.
4. **Convention violations are fixable** — Mechanical issues (missing `Async` suffix, wrong return types, string interpolation in logs) can all be auto-corrected. Do it.
5. **Naming issues require human judgment** — Flag vague names with suggestions, but do not rename without explicit approval. The domain context matters.

## Patterns

### Antipattern Detection

Grep-based antipattern scan — run on the target directory:

```bash
# async void (outside event handlers)
grep -rn "async void" src/ --include="*.cs" | grep -v "EventHandler"

# Blocking on async — deadlock risk
grep -rn "\.Result\b\|\.Wait()" src/ --include="*.cs"

# HttpClient instantiation — socket exhaustion
grep -rn "new HttpClient()" src/ --include="*.cs"

# DateTime.Now — use TimeProvider instead
grep -rn "DateTime\.Now\b" src/ --include="*.cs"

# Empty catch blocks — swallowing exceptions
grep -rn "catch\s*{" src/ --include="*.cs"
grep -rn "catch\s*(Exception\s*[a-z]\+\s*)\s*{$" src/ --include="*.cs"

# Console output in production code
grep -rn "Console\.Write\|Debug\.Write" src/ --include="*.cs"

# Thread.Sleep — use Task.Delay in async code
grep -rn "Thread\.Sleep" src/ --include="*.cs"
```

### Dead Code Detection

```csharp
// GOOD — referenced everywhere it needs to be
public sealed class OrderService(AppDbContext db) { ... }

// BAD — private method with zero callers (flag for removal)
private void ProcessOldFormat(string data) { ... }  // ← 0 references, CS warning
```

Look for:
- `private` methods/classes with zero `Grep` hits in the project
- Compiler warnings CS0168, CS0169, CS0219 (unused variable/field)
- Commented-out code blocks (3+ consecutive `//` lines of code, not documentation)

### Magic Values

```csharp
// BAD — magic number, magic string, hardcoded URL
if (retryCount > 3) ...
var url = "https://api.payments.internal/v2/charge";
if (status == "pending_review") ...

// GOOD — named constant or configuration
private const int MaxRetryAttempts = 3;
// URL from IOptions<PaymentOptions>
// status from OrderStatus enum
```

### TODO Debt Triage

```bash
# Find all technical debt markers
grep -rn "TODO\|FIXME\|HACK\|XXX\|TEMP" src/ --include="*.cs"
```

Severity grouping:
- `FIXME` / `HACK` → Critical — these signal known broken or fragile code
- `TODO` → Warning — create GitHub issues for each, remove the comment
- `TEMP` → Warning — temporary code that became permanent; remove it

### Convention Violations (C# specific)

```csharp
// BAD — missing Async suffix
public async Task<Order> GetOrder(Guid id) { ... }

// GOOD
public async Task<Order> GetOrderAsync(Guid id) { ... }

// BAD — mutable list return type
public List<Order> GetPendingOrders() { ... }

// GOOD — readonly return type
public IReadOnlyList<Order> GetPendingOrders() { ... }

// BAD — old null check
if (string.IsNullOrEmpty(name)) ...

// GOOD — modern pattern
if (name is null or "") ...

// BAD — regions hide complexity
#region Helpers
...
#endregion

// GOOD — split into smaller focused files
```

## Anti-patterns

### Fixing Naming Without Domain Context

```
// BAD — renaming "ProcessPayment" to "HandlePayment" without understanding
// the domain distinction between processing and handling
Edit: PaymentService.cs — rename ProcessPayment → HandlePayment

// GOOD — flag with suggestions, let the developer decide
Warning: PaymentService.ProcessPayment — name is vague.
Suggestions: ChargeCard, ExecutePaymentCapture, SubmitPaymentRequest
(renaming requires domain context — awaiting your decision)
```

### Reporting Without Fixing

```
// BAD — produces a 40-item report and stops
De-Sloppify Report:
  - 8 missing Async suffixes
  - 5 .Result calls
  - 3 new HttpClient() usages
[Report complete]

// GOOD — fix the mechanical issues, report the judgment calls
Fixed automatically:
  - 8 Async suffix additions (async methods)
  - 3 new HttpClient() → IHttpClientFactory injection (see changes)
Pending your decision:
  - 5 .Result calls in non-async contexts — show each? [y/n]
```

### Deleting Commented Code Without Asking

```csharp
// BAD — silently removes commented code
// var legacyProcessor = new LegacyPaymentProcessor();
// result = legacyProcessor.Charge(amount);
// await legacyProcessor.DisposeAsync();
[Deleted without asking]

// GOOD — flag and ask
Found commented-out code block in PaymentService.cs:45-47 (legacy processor).
Remove it? It appears unused since the PaymentV2 migration. [y/n]
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| `async void` found | Fix to `async Task` immediately (Critical) |
| `.Result` / `.Wait()` on async | Fix to `await` (Critical) |
| `new HttpClient()` | Replace with `IHttpClientFactory` injection (Critical) |
| `DateTime.Now` | Replace with `TimeProvider.GetUtcNow()` (Warning) |
| Empty catch block | Flag for developer decision — may be intentional |
| Commented-out code block | Ask before removing |
| Missing `Async` suffix | Fix automatically |
| Vague method name (`DoWork`, `Process`) | Suggest alternatives, do not rename |
| TODO comment | Create GitHub issue, remove comment |
| FIXME / HACK comment | Escalate to Critical — fix or document why it's there |
| Magic number in business logic | Flag with suggested constant name |
| Hardcoded URL/connection string | Move to `IOptions<T>` — flag the location |
| No target specified | Scan `src/` in the current directory |

## Execution

You are executing the /de-sloppify command. Find and fix code quality issues.

### What to Check

**1. Antipatterns (via MCP or Grep)**
Use `detect_antipatterns` if MCP is available, otherwise grep for:
- `async void` (non-event-handler)
- `.Result` or `.Wait()` on tasks
- `new HttpClient()`
- `DateTime.Now`
- Empty catch blocks: `catch { }` or `catch (Exception) { }`
- `Console.WriteLine` or `Debug.Write`
- `Thread.Sleep`

**2. Dead Code**
Look for:
- Methods, classes with `private` access and zero references
- Commented-out code blocks (3+ lines of `//` code)
- Variables declared but never used (look for CS warnings)
- Empty methods with no implementation

**3. Magic Values**
Find hardcoded values that should be constants or config:
- Magic numbers in business logic
- Hardcoded URLs, connection strings, timeouts
- String literals used multiple times

**4. TODO Debt**
```bash
grep -rn "TODO\|FIXME\|HACK\|XXX\|TEMP" src/ --include="*.cs"
```
List all TODO comments, group by severity:
- `FIXME`/`HACK` → immediate attention
- `TODO` → backlog items (create as GitHub issues)
- `TEMP` → should be removed

**5. Naming Issues**
Check for:
- Methods named `DoStuff`, `Process`, `Handle` without context
- Variables named `data`, `result`, `temp`, `obj`
- `Manager`, `Helper`, `Utils`, `Service` in class names (often a DIP violation)

**6. Convention Violations**
Check against `~/.claude/rules/csharp.md`:
- Missing `Async` suffix on async methods
- Using `IList<T>` instead of `IReadOnlyList<T>` for return types
- Using `string.IsNullOrEmpty` instead of `is null or ""`
- Missing file-scoped namespaces
- Regions used

### Approach
1. Scan the target (current dir if not specified)
2. Group findings by severity: Critical → Warning → Style
3. For each Critical/Warning, offer to fix it in-place
4. For Style issues, show a summary and ask "fix all?" before editing
5. Never remove commented-out code without confirming — it might be intentional

### Output Format
```
De-Sloppify Report — {path}
============================
Critical (fix now):
  AntiPattern: src/OrderHandler.cs:42 — .Result on async method
  AntiPattern: src/EmailService.cs:18 — new HttpClient()

Warnings:
  TODO debt: 8 TODOs found (3 FIXME)
  Dead code: OrderHelper.ProcessOld() — private, 0 references

Style:
  Naming: 3 vague method names (DoWork, HandleIt, ProcessData)
  Conventions: 5 methods missing Async suffix

Fixed: 0 | Pending: 12
```
