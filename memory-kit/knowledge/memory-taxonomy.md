# Memory Taxonomy

Complete reference for memory types, classification rules, anti-patterns, and decision guide.

## The Four Memory Types

### `user` — Who the user is

Stores facts about the user's role, expertise, tools, and workflow preferences.

**What belongs here:**
- Job role and responsibilities ("senior backend engineer", "data scientist", "tech lead")
- Domain expertise ("10 years .NET, new to Vue")
- Tool/technology preferences ("uses Neovim", "prefers functional style")
- Workflow preferences ("reviews PRs on Fridays", "works in 2-week sprints")

**What does NOT belong here:**
- Preferences about Claude's behavior (that's `feedback`)
- Project-specific context (that's `project`)
- Anything that changes session-by-session

**Lifespan:** Permanent unless user explicitly updates.

**Example:**
```markdown
name: user-profile-gino
description: Senior .NET developer, 10yr experience, new to Vue frontend in this project
type: user
body: Senior .NET/C# developer with 10 years of experience.
      Proficient in Clean Architecture, CQRS, EF Core, Serilog.
      Currently learning Vue 3 / TypeScript for this project's frontend.
      Frame frontend patterns in terms of .NET/C# analogues.
```

---

### `feedback` — How Claude should behave

Stores behavioral corrections ("don't", "stop", "always/never") and confirmed approaches.

**What belongs here:**
- Explicit corrections ("don't mock the database")
- Approach validations ("yes, that was the right call")
- Style preferences ("always use file-scoped namespaces")
- Anti-patterns specific to this project ("never use static helpers in handlers")

**What does NOT belong here:**
- One-time instructions for a specific task
- Things already documented in CLAUDE.md
- Obvious conventions any developer would follow

**Lifespan:** Permanent unless corrected or superseded.

**Structure (required):**
```
Rule statement
**Why:** reason the user gave or incident that caused the feedback
**How to apply:** when/where this guidance kicks in
```

**Example:**
```markdown
name: integration-tests-use-real-db
description: Integration tests must use real database; mocked tests mask migration failures
type: feedback
body: Don't mock the database in integration tests — use real database via Testcontainers.
      **Why:** We got burned last quarter when mocked tests passed but prod migration failed.
               Mocks hide schema drift and transaction behavior differences.
      **How to apply:** All test classes named *IntegrationTest or *Tests in the integration
                        project must use the real database. Unit tests may mock.
```

---

### `project` — Current context and initiatives

Stores time-bound project context: active initiatives, deadlines, decisions, and team state.

**What belongs here:**
- Active sprints or initiatives ("we're building the auth module")
- Deadlines (always as absolute dates)
- Team decisions ("we decided to use microservices over monolith because...")
- Architectural decisions specific to current work
- Compliance or legal constraints

**What does NOT belong here:**
- Permanent architectural rules (those belong in `feedback`)
- User profile facts (those belong in `user`)
- External system locations (those belong in `reference`)

**Lifespan:** ~30 days. Project memories should be pruned when initiatives complete.

**Structure (required):**
```
Fact or decision
**Why:** motivation, constraint, or stakeholder ask
**How to apply:** how this should shape Claude's suggestions
```

**Example:**
```markdown
name: auth-rewrite-compliance-driven
description: Auth middleware rewrite driven by legal compliance on session token storage
type: project
body: Auth middleware is being rewritten to remove session tokens from localStorage.
      **Why:** Legal flagged the current implementation as non-compliant with new data
               protection requirements around session token storage.
      **How to apply:** Scope decisions for auth work should favor compliance requirements
                        over ergonomics or backwards compatibility.
```

---

### `reference` — Where things live

Stores pointers to external systems, tools, and resources.

**What belongs here:**
- Issue tracker locations ("bugs in Linear project INGEST")
- Dashboard URLs
- Runbook/wiki locations
- API documentation references
- System names + purposes ("grafana.internal/d/api-latency is the oncall board")

**What does NOT belong here:**
- File paths inside the repository (use `git` or `Glob`)
- General documentation (link to it, don't summarize it here)

**Lifespan:** ~90 days. Verify URLs haven't changed.

**Example:**
```markdown
name: pipeline-bugs-linear-ingest
description: Backend pipeline bugs tracked in Linear project INGEST
type: reference
body: Pipeline bugs and data ingestion issues are tracked in Linear project "INGEST".
      Check there first before filing new issues — cross-reference by component.
```

---

## Classification Heuristics

| Signal in content | Likely type |
|------------------|-------------|
| "I'm a", "I've been", "my background", role/title mention | `user` |
| "don't", "stop", "never", "always", "we got burned", "that's wrong" | `feedback` |
| Approach confirmed ("yes exactly", "that was right", "keep doing that") | `feedback` |
| Deadline, team name, "we're doing", "because legal", initiative name | `project` |
| URL, "tracked in", "can be found at", "located at", system name + location | `reference` |

When multiple signals appear, the strongest signal wins. If tied, default to `feedback`.

---

## Decision Guide

| You observe this | Store as |
|-----------------|----------|
| User corrects Claude's approach | `feedback` |
| User confirms a non-obvious approach worked | `feedback` |
| User describes their expertise level | `user` |
| User mentions their current sprint focus | `project` |
| User shares a tool/system location | `reference` |
| Instinct promoted from instincts.md | Match instinct category to memory type |
| Non-obvious bug root cause discovered | `feedback` (or learning-log if not a behavioral rule) |
| Undocumented architectural decision revealed | `project` (if current) or `feedback` (if permanent rule) |

---

## Anti-patterns

### Storing Code Patterns as Memory

```
# BAD — this belongs in CLAUDE.md, not memory
feedback: "Always use CQRS with MediatR in this project"

# GOOD — architectural conventions belong in CLAUDE.md rules
# Memory stores corrections Claude needs to apply DESPITE its defaults,
# not every convention of the project
```

### Storing Session State as Project Memory

```
# BAD — ephemeral state masquerading as project memory
project: "Currently working on the cart feature, see src/Cart/"

# GOOD — project memories are decisions and context, not tasks
project: "Cart uses eventual consistency pattern — no synchronous inventory checks"
```

### Reference Without Location

```
# BAD — useless reference
reference: "Check the monitoring dashboard when touching APIs"

# GOOD — reference includes the actual pointer
reference: "API latency monitoring: grafana.internal/d/api-latency — oncall watches this"
```
