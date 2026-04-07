---
name: scaffold-knowledge
description: >
  Interactive wizard for creating a new Claude Code knowledge doc — deep reference
  with full code examples loaded on demand. Knowledge docs back complex skills with
  copy-paste-ready patterns. Load this skill when: "create a knowledge doc", "new knowledge",
  "scaffold knowledge", "deep reference", "reference doc", "pattern library", "knowledge base".
user-invocable: true
argument-hint: "[topic name]"
allowed-tools: Read, Write, Glob
---

# Scaffold Knowledge

## Core Principles

1. **Knowledge docs are pattern libraries, not tutorials** — Every section must have runnable, copy-paste-ready code. No pseudocode. No placeholders.
2. **Loaded on demand, never always-loaded** — Knowledge docs are expensive (~3–8k tokens). They must be referenced from skills via `@` links, not from CLAUDE.md directly.
3. **Explain the why, not just the what** — Inline comments must explain *why* a pattern is used, not restate what the code does. `// EF Core requires this for value object mapping` not `// configure value object`.
4. **One anti-pattern section per doc** — Every knowledge doc must show what NOT to do. The anti-pattern section often has more value than the patterns themselves.
5. **Include a copy-paste setup block** — The first pattern must be a complete, runnable registration or setup block. Users need to get started immediately.

## Patterns

### Knowledge Doc Structure

```markdown
# Topic Name — Subtitle

## Setup / Registration

\```csharp
// Copy-paste ready: full DI registration or setup code
services.AddSomething(options => { ... });
\```

## Pattern: {Pattern Name}

\```csharp
// Full working code, not pseudocode
// Comments explain WHY, not WHAT
public sealed class MyClass { ... }
\```

## Pattern: {Another Pattern}

...

## Anti-patterns

\```csharp
// BAD — reason why this is wrong
var bad = new Thing();

// GOOD — reason why this is correct
var good = factory.Create<Thing>();
\```

## NuGet Packages (if applicable)

\```xml
<PackageReference Include="..." Version="*" />
\```
```

### Wizard Flow

**Step 1 — Identify the topic and target skill**
Ask: "What topic does this knowledge doc cover? Which skill will reference it?"

The skill gets a `## Deep Reference` entry:
```markdown
## Deep Reference
@~/.claude/knowledge/{kit-name}/{topic}.md
```

**Step 2 — Gather the patterns**
Ask: "What are the 3–5 most important patterns for this topic? For each, what is the most common mistake?"

**Step 3 — Write the setup block first**
The first code block must be a complete, working setup (install command + minimal registration):

```markdown
## Installation

\```bash
# npm / pip / cargo / dotnet / brew — use whatever the project's package manager is
npm install some-library
\```

## Registration / Configuration

\```js
// Show the minimal wiring needed to get the library working
import { createClient } from 'some-library'

export const client = createClient({
  url: process.env.SERVICE_URL,
  timeout: 5000,
})
\```
```

**Step 4 — Write each pattern with runnable code**

```markdown
## Pattern: Basic Usage

\```js
// Show the most common operation — this is what 80% of users need
const result = await client.query('SELECT * FROM orders WHERE id = ?', [orderId])

// Always show error handling inline
if (!result.ok) {
  throw new Error(`Query failed: ${result.error}`)
}

return result.rows
\```
```

**Step 5 — Write the anti-pattern section**

```markdown
## Anti-patterns

\```csharp
// BAD — business logic in handler
public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
{
    if (cmd.Amount <= 0) throw new ArgumentException("Amount must be positive"); // use validator
    var order = new Order { Id = Guid.NewGuid(), Amount = cmd.Amount };          // use domain factory
    _repo.Add(order);                                                            // use DbContext directly
    await _repo.SaveAsync();                                                     // inject UoW anti-pattern
    return order.Id;
}

// GOOD — thin handler, rich domain
public async Task<Result<Guid>> Handle(CreateOrderCommand cmd, CancellationToken ct)
{
    var order = Order.Create(new CustomerId(cmd.CustomerId), new Money(cmd.Amount));
    db.Orders.Add(order);
    await db.SaveChangesAsync(ct);
    return order.Id.Value;
}
\```
```

**Step 6 — Place the file**
```
{kit-name}/knowledge/{topic-name}.md
```

Naming: `kebab-case` describing the topic. Examples: `ef-core-patterns.md`, `result-pattern.md`, `cqrs-mediatr.md`.

### Example Knowledge Doc Outline

For a topic like "HybridCache Patterns":

1. `## NuGet Packages` — the package reference
2. `## Registration` — `services.AddHybridCache()` setup block
3. `## Pattern: GetOrCreateAsync` — basic cache-aside with full code
4. `## Pattern: Cache Invalidation` — `RemoveByTagAsync` pattern
5. `## Pattern: Output Caching` — endpoint-level caching
6. `## Anti-patterns` — `IMemoryCache` in distributed scenarios, forgetting tags, stampede
7. `## Configuration Reference` — options table

## Anti-patterns

### Pseudocode Instead of Real Code

```markdown
# BAD — pseudocode, unusable
## Pattern: Handler
The handler should get the entity, check if it exists, and return an error if not.
Then it should apply the business logic and save.

# GOOD — copy-paste ready
## Pattern: Handler
\```csharp
internal sealed class CancelOrderHandler(AppDbContext db)
    : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([request.OrderId], ct);
        if (order is null) return OrderErrors.NotFound;
        var result = order.Cancel();
        if (result.IsFailure) return result.Error!;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
\```
```

### Knowledge Doc Always-Loaded in CLAUDE.md

```markdown
# BAD — loads every session, wastes tokens
## Kit Anatomy Reference
@~/.claude/knowledge/kit-maker/kit-anatomy.md      ← 3,000 tokens every session

# GOOD — referenced from the skill that needs it
# In scaffold-kit/SKILL.md:
## Deep Reference
@~/.claude/knowledge/kit-maker/kit-anatomy.md      ← loads only when scaffolding
```

### TODO Placeholders Left in Doc

```markdown
# BAD — incomplete on ship
## Pattern: Saga Orchestration
// TODO: add example

# GOOD — complete or explicitly deferred
## Pattern: Saga Orchestration
[Not yet documented — use Wolverine saga docs: https://wolverine.netlify.app/guide/durability/sagas]
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Skill has > 20 lines of code examples | Extract to knowledge doc, add Deep Reference link |
| Same pattern referenced by 2+ skills | Extract to shared knowledge doc |
| Quick reference table (< 10 lines) | Keep in skill's Decision Guide, not a knowledge doc |
| Full NuGet setup + 3+ patterns | Knowledge doc |
| Topic changes frequently (versions) | Knowledge doc (easier to update than embedded in skill) |
| One-off code sample for a single task | Keep inline in skill Patterns section |
| Knowledge doc > 300 lines | Split into two focused docs |

## Execution

1. Parse `$ARGUMENTS` as the topic name; if blank, ask "What topic does this knowledge doc cover?"
2. Ask which skill will reference this doc (needed for the `## Deep Reference` link)
3. Gather 3–5 key patterns from the user; for each, ask "What is the most common mistake with this pattern?"
4. Write the setup/installation block first (complete, copy-paste-ready)
5. Write each pattern section with runnable code and WHY comments (no pseudocode, no placeholders)
6. Write the `## Anti-patterns` section (BAD → GOOD format)
7. Write file to `{kit-name}/knowledge/{topic-name}.md` in kebab-case
8. Add `## Deep Reference` section to the referencing skill's SKILL.md: `@~/.claude/knowledge/{kit-name}/{topic}.md`
9. Confirm file written and reference added

$ARGUMENTS
