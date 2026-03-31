---
name: memory-capture
description: >
  Manually capture a memory from conversation content. Auto-classifies type, generates
  name and description, deduplicates, writes the memory file, and updates MEMORY.md index.
  Load this skill when: "capture this", "remember this", "save to memory", "/memory-capture",
  "add to memory", "store this", "memory-capture", "persist this", "save this insight".
user-invocable: true
argument-hint: "[<content to capture> | --type <user|feedback|project|reference>]"
allowed-tools: Read, Write, Edit, Glob, Grep
---

# Memory Capture

## Core Principles

1. **Classify before storing** — Always determine the correct memory type before writing. Wrong type = wrong retrieval.
2. **Generalize feedback rules** — Specific corrections must be broadened to class-level rules before storage.
3. **Deduplicate first** — Search for existing memories before creating a new one. Update is almost always better than create.
4. **Confirm to user** — Always tell the user what was captured, where, and as what type.
5. **Description is mandatory** — A memory without a description is invisible in the index.

## Capture Flow

```
1. EXTRACT
   If $ARGUMENTS is provided → use it as content
   Otherwise → ask user "What would you like to capture?"

2. CLASSIFY
   Call memory_classify with the content
   Show result: "Detected type: {type} (confidence: {score})"
   If confidence < 0.6 → ask user to confirm or override type

3. GENERALIZE (feedback type only)
   Transform specific correction to class-level rule:
   "Don't use X in module Y" → "Never use X anywhere — reason"
   Show generalized version, ask user to confirm

4. DEDUPLICATE
   Call memory_search with the suggested name + key terms, limit 3
   If similar memory found:
     "Found similar memory: '{existing_name}'. Update it instead? [Y/n]"
     Yes → update via memory_store with same file_name
     No  → create new memory

5. NAME
   Use memory_classify suggested_name as default
   Ask: "Name this memory [{suggested_name}]: " (enter to accept)

6. DESCRIPTION
   Auto-generate from content (first sentence, ≤ 120 chars)
   Ask: "Description [{auto_description}]: " (enter to accept)

7. BODY
   For feedback/project: structure as rule + **Why:** + **How to apply:**
   For user/reference: plain description

8. STORE
   Call memory_store with: name, description, type, body, source: "manual"
   Output: "Captured to memory: {name} ({type}) → {file_path}"
```

## Type-Specific Capture Formats

### feedback

```markdown
---
name: avoid-database-mocking-in-tests
description: Integration tests must hit real database; mocked tests mask migration failures
type: feedback
tags: [testing, database, mocking, integration-tests]
created: 2026-03-30
updated: 2026-03-30
confidence: 1.0
source: manual
---

Don't mock the database in integration tests.

**Why:** We got burned last quarter when mocked tests passed but the prod migration failed. Mocks hide schema drift and query behavior differences.

**How to apply:** All integration tests use real database via Testcontainers or a dedicated test DB. Unit tests that mock are only for pure business logic.
```

### user

```markdown
---
name: user-role-senior-dotnet
description: Senior .NET developer, 10 years experience, new to Vue frontend in this project
type: user
tags: [dotnet, csharp, vue, expertise]
created: 2026-03-30
updated: 2026-03-30
confidence: 1.0
source: manual
---

Senior .NET/C# developer with 10 years of experience. Currently working with this project's Vue 3 frontend for the first time — frame frontend patterns in terms of .NET/C# analogues where helpful.
```

### project

```markdown
---
name: merge-freeze-mobile-release
description: Non-critical merges frozen from 2026-04-03 for mobile release branch cut
type: project
tags: [release, mobile, deadline, branch]
created: 2026-03-30
updated: 2026-03-30
confidence: 1.0
source: manual
---

Merge freeze begins 2026-04-03 for mobile team's release branch cut.

**Why:** Mobile team is cutting a release branch; non-critical merges risk conflicts and delayed hotfixes.

**How to apply:** Flag any non-critical PRs after 2026-04-03 as blocked until freeze lifts. Prioritize critical bug fixes only.
```

### reference

```markdown
---
name: pipeline-bugs-linear-ingest
description: Backend pipeline bugs tracked in Linear project INGEST
type: reference
tags: [linear, pipeline, bugs, tracking]
created: 2026-03-30
updated: 2026-03-30
confidence: 1.0
source: manual
---

Pipeline bugs and data ingestion issues are tracked in Linear project "INGEST". Search there first when investigating backend data flow bugs.
```

## Anti-patterns

### Capturing Without Generalizing

```
# BAD — too specific to be useful
name: dont-log-in-payments-handler
body: "Don't use console.log in the payments handler"

# GOOD — generalized to apply everywhere
name: use-structured-logger-not-console
body: "Always use the project's structured logger instead of console.log anywhere..."
```

### Missing Description

```
# BAD — invisible in MEMORY.md index
description: ""

# GOOD — searchable and readable in one line
description: "Integration tests must hit real database; mocked tests mask migration failures"
```

### Capturing Ephemeral State

```
# BAD
"Currently working on the payments refactor, file is at src/payments/"

# GOOD — only persistent, reusable knowledge
"The payments module uses VSA with one handler per operation under Features/Payments/"
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| User says "remember this" | Run full capture flow immediately |
| Content is a correction ("don't", "stop", "wrong") | Auto-classify as feedback, generalize before storing |
| User mentions their role/background | Capture as user memory |
| User mentions a deadline or team initiative | Capture as project memory with absolute date |
| User mentions a tool/system location | Capture as reference memory |
| Similar memory already exists | Update existing, never duplicate |
| User provides raw content via $ARGUMENTS | Skip extraction step, proceed to classify |
| Confidence < 0.6 on auto-classification | Ask user to confirm type before proceeding |
