# Kit Maker

> **Meta-kit for building Claude Code kits.** Produces installable, marketplace-ready kits with skills, rules, knowledge, agents, hooks, templates, and optional MCP servers.

## Always-Active Rules

@~/.claude/rules/kit-maker/skill-format.md
@~/.claude/rules/kit-maker/kit-structure.md
@~/.claude/rules/kit-maker/quality-standards.md
@~/.claude/rules/kit-maker/naming-conventions.md
@~/.claude/rules/kit-maker/versioning.md

## Meta — Always Apply

@~/.claude/skills/context-discipline/SKILL.md
@~/.claude/skills/model-selection/SKILL.md
@~/.claude/skills/verification-loop/SKILL.md

## Integrations

All paths come from kit config (`~/.claude/kit.config.md`) — never hardcode:
`KITS_BASE_PATH` · `KIT_AUTHOR` · `KIT_DEFAULT_LICENSE` · `MARKETPLACE_USERNAME` · `MARKETPLACE_URL` · `CLAUDE_CONFIG_DIR`

When a skill needs config values and `~/.claude/kit.config.md` is missing → tell user to run `/kit-setup`.

## Self-Improvement — Auto-Active

@~/.claude/skills/instinct-system/SKILL.md
@~/.claude/skills/self-correction-loop/SKILL.md
@~/.claude/skills/autonomous-loops/SKILL.md
@~/.claude/skills/learning-log/SKILL.md
@~/.claude/skills/convention-learner/SKILL.md
@~/.claude/skills/mem-search/SKILL.md
@~/.claude/skills/smart-explore/SKILL.md
@~/.claude/skills/timeline-report/SKILL.md

## Skills Available

### Kit Creation
- `/scaffold-kit` — interactive wizard: creates a full kit from scratch (CLAUDE.md + rules + skills + knowledge + templates + agents + hooks + manifest)
- `/scaffold-skill` — create a new skill with proper structure, trigger keywords, examples, anti-patterns, decision guide
- `/scaffold-rule` — create a new rule file with DO/DON'T format and deep reference
- `/scaffold-knowledge` — create a new knowledge doc (patterns reference with code examples)
- `/scaffold-agent` — create a new specialized agent definition
- `/scaffold-hook` — interactive hook wizard: event type, matcher, exit code strategy, hooks.json registration
- `/scaffold-command` — interactive command wizard: orchestrator structure, routing logic, max-200-lines discipline
- `/kit-setup` — interactive configuration wizard: walks through kit.config.md and registers hooks in settings.json

### Quality & Auditing
- `/skill-auditor` — review a skill for quality: completeness, trigger keywords, examples, anti-patterns, decision guide
- `/kit-health-check` — full kit audit: structure, coverage gaps, quality scores, installability

### Knowledge Docs (on demand)
- `kit-anatomy.md` — full kit structure reference: rules, skills, commands, knowledge, agents, hooks, plugin.json, config system
- `agent-frontmatter-schema.md` — all agent frontmatter fields including skills, memory, effort, isolation, initialPrompt
- `hook-exit-codes.md` — hook exit codes, all event types, hook types (command/prompt/agent/http), structured JSON output

### Specialist Agents (invoked automatically or by name)
- `marketplace-writer` — writes/syncs `plugin.json` + `marketplace.json`, validates version parity
- `config-writer` — scans CLAUDE.md + skills, generates `kit.config.template.md`
- `hook-writer` — writes bash hook scripts + `hooks.json` registration with correct exit-code strategy
- `knowledge-writer` — writes deep-reference knowledge docs with real code examples
- `rule-writer` — writes DO/DON'T rule files ≤60 lines
- `template-writer` — writes scaffolding templates with consistent placeholder conventions

### Evolution & Maintenance
- `/self-evolution` — analyze kit-building patterns across sessions and propose template/rule improvements
- `/kit-packager` — version-bump, validate, and publish a kit: syncs `plugin.json` + `marketplace.json` + `README.md`, checks installability, and runs pre-release health gate

### Meta (auto-active)
- `instinct-system` — learns kit-building patterns from each session (confidence-scored hypotheses)
- `self-correction-loop` — captures every correction into permanent MEMORY.md rules
- `autonomous-loops` — bounded scaffold/fix/improve iteration loops
- `learning-log` — captures discoveries, gotchas, and decisions each session
- `convention-learner` — detects and enforces this kit's own conventions
