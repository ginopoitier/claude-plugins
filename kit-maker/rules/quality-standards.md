# Rule: Quality Standards

Every kit component must meet these quality bars before being considered complete.

## Skill Quality Bar

| Dimension | Minimum | Target |
|-----------|---------|--------|
| Trigger keywords | 5 | 8+ |
| Core principles | 3 | 4–5 |
| Pattern examples | 2 named subsections | 4+ subsections |
| Anti-patterns | 2 | 3 |
| Decision guide rows | 4 | 6+ |
| Code examples | 3 | 5+ |

A skill without code examples is documentation, not a skill.

## Rule Quality Bar

Every rule file must have:
- `## DO` section with 5+ specific, actionable items
- `## DON'T` section with 4+ specific prohibitions
- `## Deep Reference` link if the topic warrants a full knowledge doc

Rules that say "write good code" are not rules. Rules that say "use `AsNoTracking()` on all read-only EF Core queries" are rules.

## Knowledge Doc Quality Bar

Every knowledge doc must have:
- Full code examples (not pseudocode) for every pattern
- Inline comments explaining *why*, not just *what*
- At least one anti-pattern section showing what NOT to do
- A copy-paste-ready registration/setup code block

## Agent Quality Bar

Every agent definition must specify:
- Exact task scope (what it does AND what it doesn't do)
- Which tools it has access to
- Output format (what it returns to the calling context)
- When to use it vs. staying in main context

## Template Quality Bar

Every template must be:
- Immediately usable (no `[PLACEHOLDER]` values left unfilled)
- Annotated with `# Comment` explaining each non-obvious section
- Consistent with the kit's naming conventions

## Examples

```yaml
# BAD — skill with no code examples (documentation, not a skill)
## Patterns
Use AsNoTracking() for read queries. Always project to DTOs.

# GOOD — skill with named subsections and executable code examples
## Patterns
### Read-Only Queries
```csharp
// GOOD
var items = await db.Orders
    .AsNoTracking()
    .Select(o => new OrderDto(o.Id.Value, o.Status.ToString()))
    .ToListAsync(ct);

// BAD — loads full entity, enables tracking unnecessarily
var items = await db.Orders.ToListAsync(ct);
```
```

## DO
- Run `/skill-auditor` on every new skill before marking it complete
- Run `/kit-health-check` before publishing any kit version
- Treat quality bar violations as blockers, not warnings

## DON'T
- Don't ship skills with empty Anti-patterns sections
- Don't ship skills whose Patterns section contains only prose — every pattern needs a code block
- Don't ship knowledge docs with `// TODO: add example` placeholders
