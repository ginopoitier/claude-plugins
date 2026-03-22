# ADR 001 — Universal Design: Kit Maker is Domain-Agnostic

**Status:** Accepted
**Date:** 2026-03-21

## Context

Kit Maker started as a tool built alongside a .NET dev kit. Early versions had .NET-specific patterns baked into the meta-skills: `dotnet build` in verification-loop, `AsNoTracking()` in scaffold-rule examples, `MediatR`/`AppDbContext` in scaffold-knowledge wizards, and Roslyn MCP tool calls in convention-learner.

The question arose: should kit-maker be a .NET tool, or should it work for any kit domain?

## Decision

**Kit Maker is domain-agnostic.** It must work equally well for building a data science kit, a DevOps kit, a mobile development kit, or a .NET kit.

## Consequences

**Positive:**
- A single kit-maker can bootstrap kits for any language or domain
- Meta-skills (verification-loop, convention-learner, autonomous-loops) apply universally
- Lower barrier to adoption — DevOps engineers or data scientists don't encounter .NET-specific examples
- Marketplace reach is wider

**Negative:**
- Examples become slightly more abstract (generic patterns vs concrete .NET code)
- Command resolution hierarchy adds complexity (read from CLAUDE.md → instincts → infer → ask)

## Implementation

Domain contamination was removed from:
- `verification-loop`: replaced `dotnet build` / `dotnet test` with `{BUILD_CMD}` / `{TEST_CMD}` resolved from project config
- `autonomous-loops`: same command resolution pattern
- `convention-learner`: removed Roslyn MCP calls, replaced with Grep/Glob analysis that works for any language
- `scaffold-rule`: example output uses generic data-access patterns, not EF Core
- `scaffold-knowledge`: step 3/4 use generic package install + query patterns

## Rule Going Forward

Any new skill added to kit-maker must:
1. Use `{BUILD_CMD}`, `{TEST_CMD}`, `{FORMAT_CMD}` placeholders resolved from project config
2. Show examples in multiple languages OR use language-agnostic pseudocode
3. Never reference a specific framework (MediatR, Express, Django) as the only example
4. Read commands from: project CLAUDE.md → instincts.md → infer from project files → ask user
