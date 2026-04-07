# CLAUDE.md

This file provides guidance to Claude Code (`claude.ai/code`) when working in this repository.

---

## Prime Directive

This repository exists to make Claude **more useful per token**.

Every file, kit, rule, skill, hook, agent, and MCP integration in this repo must justify its existence by improving at least one of these:

- **context efficiency**
- **token economy**
- **task precision**
- **decision speed**
- **working-memory retention**
- **large-repo navigation**
- **repeatability of good output**

If something adds text but does not materially improve Claude’s behavior, it is bloat.

**Bloat is a defect.**

---

## Core Mission

This repository exists to build and maintain a **Claude plugin marketplace** focused on **context efficiency**.

### Primary goal
Every kit in this repo should help Claude:

- use **fewer tokens**
- retain **more useful context**
- reduce **prompt repetition**
- avoid **re-deriving known patterns**
- load **only what is needed**
- produce **higher signal per token**

### What “good” looks like
A successful kit should make Claude better at one or more of these:

- **Context compression** — turning large codebases / docs / patterns into smaller reusable instructions
- **Task routing** — loading only the right rules / skills / agents for the task
- **Decision acceleration** — fewer exploratory tokens before useful output
- **Execution consistency** — less drift, fewer retries, fewer “forgot previous constraints” failures
- **Repository cognition** — better local understanding without re-scanning everything repeatedly

If a contribution adds complexity without improving Claude’s effective working density, it should be simplified or rejected.

---

## What This Repo Is

A **Claude Code plugin marketplace monorepo** containing multiple installable kits.

Users add the marketplace once, then install individual kits:

```bash
/plugin marketplace add ginopoitier/claude-plugins
/plugin install dotnet-kit@ginopoitier-plugins
```

This repo is not just a collection of prompts. It is a **system of composable Claude capability kits** designed to improve Claude’s usefulness inside real developer workflows.

---

## Design Principles

All kits in this repository should follow these principles:

### 1) Token efficiency first
Prefer:
- concise rules
- deterministic structures
- explicit decision trees
- compressed domain patterns
- reusable heuristics

Avoid:
- motivational prose
- duplicated instructions
- long narrative explanations in always-loaded files
- vague “best practices” that cost tokens without changing behavior

### 2) Load late, not early
Only always-load what Claude truly needs every session.

Use:
- `rules/` for minimal, durable behavioral constraints
- `skills/` for task-triggered behavior
- `knowledge/` for deeper reference loaded only when necessary

### 3) Operational over descriptive
Docs should tell Claude **how to act**, not just explain concepts.

Prefer:
- checklists
- decision guides
- anti-patterns
- escalation rules
- tool usage constraints
- output shape expectations

### 4) One problem per component
A kit, skill, rule, agent, or knowledge file should have a **clear bounded purpose**.

Bad:
- “general architecture guidance + SQL tips + debugging style + testing advice”

Good:
- “Entity Framework query diagnosis”
- “Vue SFC state isolation”
- “Roslyn symbol lookup routing”

### 5) Minimize cross-file dependency
A skill should not require Claude to mentally reconstruct five other documents unless absolutely necessary.

Keep each component:
- self-sufficient
- predictable
- composable

### 6) Prefer stable abstractions
Write for reuse across many sessions and repos.

Avoid embedding:
- project-specific assumptions
- local machine paths
- ephemeral workflows
- one-off instructions disguised as reusable kits

---

## Marketplace Philosophy

This repository should function like a **high-signal plugin store**, not a dumping ground.

### Marketplace acceptance bar
A kit should only exist if it does at least one of the following well:

- reduces repeated prompting
- improves task routing
- compresses expert workflow into reusable behavior
- saves context window space over time
- increases Claude’s accuracy in a bounded domain
- integrates a tool or MCP capability in a reusable way

### Kits should not be added if they are mainly:
- prompt collections without operational structure
- thin wrappers around generic advice
- broad “do everything” bundles
- overly verbose rule packs
- copies of knowledge Claude can already infer cheaply

A kit should feel like a **capability multiplier**, not a folder of text.

---

## Repository Layout

```text
claude-plugins/
  .claude-plugin/marketplace.json   ← Single marketplace catalog for ALL kits
  README.md                         ← Install instructions + version matrix + kit overview
  MCP/
    DotNet/                         ← .NET 9 MCP server (Roslyn + SQL Server analysis)
    Vue/                            ← TypeScript MCP server (Vue SFC analysis)
  {kit-name}/
    CLAUDE.md                       ← Kit entry point loaded every session
    .claude-plugin/plugin.json      ← Per-kit manifest (authoritative kit version)
    rules/                          ← Always-loaded behavioral rules (3–8 files)
    skills/{skill-name}/SKILL.md    ← Lazy-loaded task behaviors
    knowledge/                      ← On-demand deep reference
    agents/                         ← Specialist agent definitions
    hooks/                          ← Install/setup automation shell scripts
    config/kit.config.template.md   ← User fills and saves to ~/.claude/kit.config.md
```

---

## Kit Conventions

## Kit structure rules

Each kit must follow these conventions:

- `CLAUDE.md` is the **entry point**
- `CLAUDE.md` should load rules via `@~/.claude/rules/{kit-name}/...`
- Rules are **always-loaded**
- Skills are **lazy-loaded**
- Knowledge is **on-demand**
- Agents are **specialists**, not generic assistants
- Hooks should help users configure the kit correctly

### Required structure
Each kit should contain:

- `CLAUDE.md`
- `.claude-plugin/plugin.json`
- `rules/`
- `skills/`
- `hooks/check-settings.sh`
- `hooks/hooks.json`
- `config/kit.config.template.md`

Optional but encouraged:
- `knowledge/`
- `agents/`

---

## Authoring Standard: Optimize for Token ROI

When writing any file in this repo, optimize for **Token ROI**:

> **Useful behavior gained ÷ tokens consumed**

A file is good if it changes Claude’s behavior materially using few tokens.

### High ROI content
Prefer content like:

- decision trees
- trigger-to-action mappings
- concise domain constraints
- code smell → likely cause tables
- debugging funnels
- architecture heuristics
- naming conventions
- tool routing rules
- output format contracts

### Low ROI content
Avoid content like:

- “In modern software engineering…”
- long intros
- repeated rationale
- generic textbook explanations
- prose-heavy style guides
- obvious advice Claude already knows

### Compression rule
If a paragraph can be replaced by:
- 3 bullets
- a checklist
- a decision table
- a DO / DON’T rule

…then it should be.

---

## Rules (`rules/`)

Rules are **always-loaded**, so they are the most expensive real estate in a kit.

### Rules must be:
- short
- stable
- behavior-shaping
- high-frequency
- low-ambiguity

### Rules must NOT be:
- tutorial content
- deep reference material
- niche edge-case instructions
- long-form prose
- duplicated across files

### Recommended rule count
- **Minimum:** 3
- **Maximum:** 8

If you need more than 8, the kit is probably under-factored and should move content into:
- `skills/`
- `knowledge/`
- `agents/`

### Rule format
Rules should be written in **DO / DON’T format only**.

Good:
```md
DO identify repository conventions before proposing refactors.
DON’T introduce a new abstraction if an existing pattern already solves the problem.
```

Bad:
```md
When working in repositories, it is often useful to first understand...
```

### Rule authoring heuristic
Every rule should answer:

> “What should Claude reliably do in this repo without needing to think about it from scratch every time?”

---

## Skills (`skills/`)

Skills are the main mechanism for **task-specific intelligence without permanent token cost**.

They should encode workflows Claude can load **only when needed**.

### When to create a skill
Create a skill when a behavior is:

- task-specific
- reusable
- too large for rules
- frequently needed
- improved by structured execution steps

Examples:
- “C# service refactor planner”
- “Vue SFC bug triage”
- “SQL performance diagnosis”
- “Prompt compression pass”
- “Context budget recovery”

### Skill format requirements
Every `SKILL.md` must include frontmatter with:

- `name`
- `description`
- `user-invocable`
- `allowed-tools`

### Description requirements
The `description` must include:
- the skill’s purpose
- at least **5 trigger keywords**

These keywords help Claude auto-load the skill.

### Required section order
Every `SKILL.md` must contain these sections in this exact order:

1. **Core Principles**
2. **Patterns**
3. **Anti-patterns**
4. **Decision Guide**
5. **Execution**

The file must end with:

```text
$ARGUMENTS
```

### Skill writing standard
A good skill should reduce Claude’s need to:
- improvise structure
- rediscover common failure modes
- ask unnecessary clarifying questions
- consume context reconstructing workflow

A skill should feel like a **compressed operating procedure**.

---

## Knowledge (`knowledge/`)

Knowledge files are **deep reference**, not default behavior.

Use `knowledge/` for:
- framework-specific nuance
- architecture reference
- domain-specific edge cases
- extended examples
- dense factual material

Do **not** place content in `knowledge/` if it needs to shape Claude’s behavior every session.

---

## Agents (`agents/`)

Agents should be **specialists**, not alternate personalities.

Use agents when Claude benefits from assuming a bounded expert role such as:

- code archaeologist
- performance diagnostician
- migration planner
- dependency cartographer
- prompt compressor
- architecture reviewer

Do not create agents that are just:
- “senior engineer”
- “helpful coding assistant”
- “software expert”

That is too broad and adds little value.

---

## Hooks (`hooks/`)

Hooks exist to improve install success and reduce misconfiguration.

Every kit should ship:

- `hooks/check-settings.sh`
- `hooks/hooks.json`

### Hook responsibilities
Hooks should help users:
- detect missing config
- identify broken install assumptions
- validate required files
- verify MCP dependencies if applicable

Hooks should be:
- minimal
- deterministic
- non-destructive

---

## Config (`config/`)

Each kit may expose user-configurable settings via:

```text
config/kit.config.template.md
```

Users copy and save this as:

```text
~/.claude/kit.config.md
```

### Config design rules
Config should only include values that materially change behavior, such as:

- preferred stack conventions
- naming patterns
- architecture preferences
- repo-specific locations
- allowed/excluded directories
- MCP connection hints

Do not put low-value settings into config just because customization is possible.

---

## Install Paths (after `/plugin install`)

Expected install locations:

- Rules → `~/.claude/rules/{kit-name}/`
- Skills → `~/.claude/skills/`
- Knowledge → `~/.claude/knowledge/{kit-name}/`
- Agents → `~/.claude/agents/`
- Config → `~/.claude/kit.config.md`

### Important constraint
Skills are installed into a **flat namespace**:

```text
~/.claude/skills/
```

Therefore:

- **No two kits may share a skill directory name**
- Skill names must be globally collision-resistant

Prefer names like:
- `dotnet-query-diagnosis`
- `vue-state-triage`
- `context-budget-recovery`

Avoid generic names like:
- `debug`
- `review`
- `refactor`

---

## MCP Servers

This repo includes MCP-backed capabilities that should enhance Claude’s reasoning **without bloating prompt context**.

### Current MCP servers

| Server | Source | Powers |
|--------|--------|--------|
| `devkit-mcp` | `MCP/DotNet/DotNet.Mcp.sln` | Roslyn, SQL Server, Neo4j, .NET analysis |
| `vue-mcp` | `MCP/Vue/vue-mcp/` | Vue SFC analysis, Pinia, type checking |

### Build commands

Build .NET MCP:

```bash
dotnet build MCP/DotNet/DotNet.Mcp.sln
```

Build Vue MCP:

```bash
cd MCP/Vue/vue-mcp && npm install && npm run build
```

### MCP design expectation
MCP integrations should help Claude:
- fetch precise facts instead of inferring them expensively
- inspect code structure without excessive prompt stuffing
- reduce context duplication across large repositories

If an MCP tool increases complexity but does not reduce token burn or improve precision, it should be reconsidered.

---

## Versioning

Version is declared in:

```text
{kit}/.claude-plugin/plugin.json
```

It must stay in sync with:
- the matching entry in `.claude-plugin/marketplace.json`
- the version table in `README.md`

All three must be updated in the **same commit**.

### Version bump policy

| Change | Bump |
|--------|------|
| Removed or renamed a user-facing skill/config key | MAJOR |
| New skill, rule, knowledge doc, agent, or installable capability | MINOR |
| Bug fix, wording improvement, trigger keyword addition, metadata cleanup | PATCH |

### Before committing
1. Run:
   ```bash
   git diff --stat
   ```
2. Determine bump level
3. Update:
   - `{kit}/.claude-plugin/plugin.json`
   - matching entry in `.claude-plugin/marketplace.json`
   - version table in `README.md`

---

## Quality Bar for New Kits

A new kit should not be merged unless it is clearly better than “just prompting Claude manually”.

### A kit should answer YES to most of these:
- Does it reduce repeated instructions?
- Does it encode reusable expert behavior?
- Does it avoid always-loading unnecessary text?
- Does it improve Claude’s task precision?
- Does it compress domain knowledge into operational guidance?
- Would a real developer notice meaningful workflow improvement?

### A kit should likely be rejected if:
- it mostly repeats common engineering advice
- it lacks bounded scope
- it depends on too much always-loaded prose
- it has weak triggerability
- it does not save context or improve decisions

---

## Kit Tooling (`kit-maker`)

Use the scaffolding commands to create new components:

- `/scaffold-kit`
- `/scaffold-skill`
- `/scaffold-rule`
- `/scaffold-knowledge`
- `/scaffold-agent`

### Release tooling
Use:

```text
/kit-packager
```

before release to validate:
- installability
- version sync
- marketplace consistency
- structural completeness
- health score

Publishing should require passing the health threshold:

- **GPA ≥ 3.0**

### Audit tooling
Use:

```text
/kit-health-check
```

to evaluate:
- structural quality
- coverage gaps
- naming quality
- token efficiency risks
- lazy-load hygiene

---

## Review Heuristics for Contributors

When editing this repository, prefer changes that:

- shrink always-loaded footprint
- improve trigger quality
- remove duplication
- increase determinism
- make kits easier to compose
- improve Claude’s repo-level working memory

When in doubt, ask:

> “Will this help Claude do more useful work in the same context window?”

If the answer is not clearly yes, simplify.

---

## What NOT to Commit

Do not commit:

- `kit.config.md`
- `*.local.md`
- personal machine-specific files
- generated build output
- temporary debug artifacts
- secrets or machine-bound paths

### Specifically disallowed
- `MCP/DotNet/publish/`
- `MCP/Vue/vue-mcp/dist/`
- `MCP/Memory/memory-mcp/dist/` (if present)
- hardcoded absolute paths such as:
  - `C:/Users/...`
  - `/Users/...`
  - `/home/...`

Always use:

```text
~/.claude/
```

for user-local references.

### Marketplace catalog rule
There must be **one and only one** marketplace catalog:

```text
.claude-plugin/marketplace.json
```

Do **not** create per-kit `marketplace.json` files.

---

## Non-Negotiable Repo Standard

This repository is for **high-leverage Claude capability engineering**.

Everything added here should make Claude:

- faster to orient
- cheaper to operate
- harder to derail
- better at staying useful over long sessions

If a file, kit, or feature increases token load more than it increases capability, it is not aligned with the repo’s purpose.
