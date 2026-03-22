---
name: scaffold-rule
description: >
  Interactive wizard for creating a new Claude Code rule file with proper DO/DON'T structure,
  code examples, and a Deep Reference link. Rules are always-loaded domain constraints.
  Load this skill when: "create a rule", "new rule file", "scaffold rule", "add a rule",
  "rule template", "write a rule", "domain rule", "coding rule".
user-invocable: true
argument-hint: "[rule domain or topic]"
allowed-tools: Read, Write, Glob
---

# Scaffold Rule

## Core Principles

1. **Rules are constraints, not documentation** — Every rule must have specific, testable DO and DON'T items. "Write clean code" is not a rule. "Use `AsNoTracking()` on all read-only EF Core queries" is a rule.
2. **Code examples are mandatory** — Every rule file must show what compliant code looks like. A rule without code examples will be ignored.
3. **5+ DOs, 4+ DON'Ts** — The quality bar is non-negotiable. Fewer items mean the rule is either too narrow (should be a comment) or not fully thought through.
4. **Deep Reference link for complex topics** — If the rule covers a topic that warrants 20+ lines of patterns, add a `## Deep Reference` section pointing to a knowledge doc. Keep the rule file scannable.
5. **Always-loaded means cost matters** — Rules load every session. Keep them focused and concise. Each rule should be < 60 lines. Move elaborations to `knowledge/`.

## Patterns

### Rule File Structure

```markdown
# Rule: {Title Case Domain}

## DO
- Use **specific pattern**: `code example here`
- Always do X when Y: `code()`
- Prefer A over B: `A.method()` not `B.method()`
- ... (5+ items minimum)

## DON'T
- Don't use X — reason why: ~~`bad.code()`~~
- Don't Y without Z
- ... (4+ items minimum)

## Deep Reference
For full patterns and code examples: @~/.claude/knowledge/{kit}/topic.md
```

### Wizard Flow

**Step 1 — Identify the domain**
Ask: "What domain or technology does this rule cover? Give me a concrete example of a pattern you want to enforce or prevent."

**Step 2 — Extract the constraints**
For each thing they mention, determine:
- Is this something Claude should always do? → DO item
- Is this something Claude should never do? → DON'T item
- Is this a nuanced case? → candidate for Deep Reference

**Step 3 — Write concrete DO items (5+)**
Each DO must:
- Start with an action verb
- Include a code snippet showing compliance
- Explain *why* in a subordinate clause if non-obvious

```markdown
## DO
- Use `IReadOnlyList<T>` for return types, not `List<T>`
- Suffix async methods with `Async`: `GetOrderAsync()` not `GetOrder()`
- Use file-scoped namespaces: `namespace MyApp.Orders;`
- Use `required` modifier for non-optional DTO properties
- Prefer `var` when the type is clear from the right side
```

**Step 4 — Write concrete DON'T items (4+)**
Each DON'T must:
- Start with "Don't"
- Explain the consequence if violated
- Show the bad pattern with ~~strikethrough~~ when useful

```markdown
## DON'T
- Don't use `DateTime.Now` — inject `TimeProvider` via DI instead
- Don't suppress nullable warnings with `!` — fix nullability properly
- Don't use regions — split large files instead
- Don't comment what code does — rename it instead
```

**Step 5 — Decide Deep Reference**
If the rule covers more than 3 distinct sub-topics or needs full code samples > 20 lines, create a knowledge doc:

```markdown
## Deep Reference
For full patterns and code examples: @~/.claude/knowledge/{kit-name}/{topic}.md
```

**Step 6 — Place the file**
```
{kit-name}/rules/{domain-name}.md
```

Naming: `kebab-case` describing the domain. Examples: `data-access.md`, `api-design.md`, `error-handling.md`.

### Example Output

```markdown
# Rule: Data Access

## DO
- Use parameterized queries for all database operations — never build queries by string concatenation
- Project to DTOs — never load full entities when only a subset of fields is needed
- Always pass a cancellation token to async database calls
- Use a connection pool — never open a new connection per request
- Use transactions for operations that must succeed or fail together

## DON'T
- Don't build SQL by string interpolation — always use parameterized queries or an ORM
- Don't call synchronous blocking database methods in async contexts
- Don't load all records to count them — use a `COUNT` query
- Don't put database configuration in the domain layer

## Deep Reference
For full patterns: @~/.claude/knowledge/{kit-name}/data-access.md
```

## Anti-patterns

### Rules as Prose

```markdown
# BAD — prose documentation, not a rule
# Rule: Database Access
Entity Framework Core is used for all database access in this project. You should
follow good patterns when writing queries and make sure to handle errors properly.

# GOOD — specific constraints with code
# Rule: EF Core Conventions
## DO
- Use `AsNoTracking()` on all read-only queries
- Always pass `CancellationToken ct` to every async call
## DON'T
- Don't use `.Include()` in query handlers — project to DTOs instead
```

### Rules That Are Too Broad

```markdown
# BAD — too many unrelated domains in one rule file
# Rule: Everything
## DO
- Follow SOLID principles
- Write tests
- Use async/await
- Handle errors
- Log things

# GOOD — one focused domain per file
# Rule: Async/Await Conventions
## DO
- Suffix all async methods with Async
- Always await rather than .Result or .Wait()
- Pass CancellationToken through the entire call chain
```

### Rules Without Code Examples

```markdown
# BAD — abstract, ignores real code
## DO
- Handle errors properly with the result pattern

# GOOD — concrete, actionable
## DO
- Return `Result<T>` from all handlers — never throw for business failures:
  `return CustomerErrors.NotFound;` not `throw new NotFoundException()`
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Constraint applies every session | Rule file (always-loaded) |
| Constraint is domain-specific, rarely needed | Skill with `## Patterns` section |
| Pattern is complex with many sub-cases | Rule file + Deep Reference knowledge doc |
| Just documenting what exists (not constraining) | Knowledge doc, not a rule |
| Rule would be > 80 lines | Split into two focused rules |
| Rule applies only to one feature | Don't make it a rule — add it to the relevant skill |
| Enforcement should be automated | Rule + hook script to validate |
