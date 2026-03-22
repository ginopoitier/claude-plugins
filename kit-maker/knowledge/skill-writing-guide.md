# Skill Writing Guide — From Blank to Ship-Ready

## The Two Failure Modes

**Failure Mode 1: The Invisible Skill**
Perfect content, wrong keywords. Claude never loads it because no trigger phrase matches the conversation. Result: 0 value delivered despite hours of work.

**Fix:** Write trigger keywords first, before any other content. Sit with the domain and ask: "What exact phrases do users say when they hit this problem?"

**Failure Mode 2: The Prose Doc**
Well-written explanation of concepts, zero code examples. Claude reads it and gives generic advice instead of specific, copy-paste patterns.

**Fix:** Every section must have a code block. If you can't show it in code, it's not ready for a skill — it belongs in a knowledge doc.

## Writing Trigger Keywords That Work

Trigger keywords in the `description` field are how Claude decides to load a skill.

**Three keyword types to cover:**

1. **Tool/technology name**: "IHttpClientFactory", "Polly", "EF Core"
2. **The problem**: "socket exhaustion", "retry logic", "circuit breaker"
3. **The action**: "AddHttpClient", "AddStandardResilienceHandler", "retry policy"

```yaml
# WEAK — too generic
description: >
  HTTP client patterns. Load for HTTP clients.

# STRONG — covers all three types
description: >
  IHttpClientFactory and typed HTTP clients for .NET.
  Load this skill when: "HttpClient", "IHttpClientFactory", "AddHttpClient",
  "typed client", "named client", "DelegatingHandler", "socket exhaustion",
  "resilience", "retry", "circuit breaker", "Polly".
```

**Testing keywords:** After writing, ask: "If a user said [keyword], would I want this skill loaded?" If yes, include it. If no, remove it.

## Writing Core Principles That Stick

Principles are the 3–5 rules that define non-negotiable behavior. They're read every time the skill loads.

**Bad principle (too vague):**
> Use best practices when writing HTTP clients.

**Good principle (specific, with rationale):**
> **Never `new HttpClient()` per request** — Direct `HttpClient` creation causes socket exhaustion under load and ignores DNS changes. Use `IHttpClientFactory` to manage handler lifetimes.

Structure: `**Bold rule** — one-sentence rationale explaining the consequence of violating it.`

## Writing Pattern Sections With Code

Each subsection in `## Patterns` is a named, complete scenario.

```csharp
### Named Client with Resilience

// GOOD — factory-managed, resilient
builder.Services.AddHttpClient("github", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp/1.0");
})
.AddStandardResilienceHandler();

// Usage
public class GitHubService(IHttpClientFactory factory)
{
    public async Task<Repo?> GetAsync(string owner, string name, CancellationToken ct)
    {
        var client = factory.CreateClient("github");
        return await client.GetFromJsonAsync<Repo>($"repos/{owner}/{name}", ct);
    }
}
```

Rules for pattern code:
- Always compiles (not pseudocode)
- Shows both registration AND usage when applicable
- Inline comments explain WHY, not what
- `// GOOD —` label on the positive example

## Writing Anti-patterns That Teach

Anti-patterns are where the real learning happens. Show the mistake, then the fix.

```csharp
### Don't Create HttpClient Per Request

// BAD — socket exhaustion under load
public async Task<string> GetDataAsync()
{
    using var client = new HttpClient();    // new socket on every call
    return await client.GetStringAsync("https://api.example.com/data");
}

// GOOD — factory-managed handler rotation
public async Task<string> GetDataAsync(CancellationToken ct)
{
    var client = _factory.CreateClient("api");
    return await client.GetStringAsync("https://api.example.com/data", ct);
}
```

Rules for anti-patterns:
- `// BAD —` label with one-line reason
- `// GOOD —` label with one-line reason
- Both examples must be runnable

## Writing a Useful Decision Guide

The Decision Guide is the most-referenced section. It's what users scan first.

**Bad (too generic):**
| Scenario | Recommendation |
|----------|---------------|
| Need HTTP client | Use IHttpClientFactory |

**Good (specific, covers the real split decisions):**
| Scenario | Recommendation |
|----------|---------------|
| New .NET 10 project | Keyed clients with `AddAsKeyed()` |
| External API calls | `AddStandardResilienceHandler()` on every client |
| Auth token injection | `DelegatingHandler` with `AddHttpMessageHandler` |
| Non-idempotent methods | `DisableForUnsafeHttpMethods()` on retry options |
| API client generation | Refit with `AddRefitClient<T>()` |

Rules:
- Scenario column: what the user is trying to decide
- Recommendation column: the specific answer (method name, class name, pattern name)
- 6+ rows covering the most common decision points in the domain

## Scope: One Skill, One Domain

A skill that covers HTTP clients AND authentication AND caching is three skills crammed into one. It:
- Loads when only one domain is needed (wastes context)
- Has too many trigger keywords (false positives)
- Has too many examples (user can't find the one they need)

Rule of thumb: if the skill would need 3+ `## Patterns` subsections covering unrelated technologies, split it.

## Quality Gate Before Shipping

Before marking any skill done, run `/skill-auditor` mentally:
- [ ] 5+ trigger keywords?
- [ ] 4+ code examples?
- [ ] 3 anti-patterns with BAD/GOOD pairs?
- [ ] Decision guide with 6+ rows?
- [ ] allowed-tools is minimal?

If any answer is no, the skill is not done.
