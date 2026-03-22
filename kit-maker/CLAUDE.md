# Kit Maker

> **Meta-kit for building Claude Code kits.** Produces installable, marketplace-ready kits with skills, rules, knowledge, agents, hooks, templates, and optional MCP servers.

## Always-Active Rules

@~/.claude/rules/kit-maker/skill-format.md
@~/.claude/rules/kit-maker/kit-structure.md
@~/.claude/rules/kit-maker/quality-standards.md
@~/.claude/rules/kit-maker/naming-conventions.md

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

## Skills Available

### Kit Creation
- `/scaffold-kit` — interactive wizard: creates a full kit from scratch (CLAUDE.md + rules + skills + knowledge + templates + agents + hooks + manifest)
- `/scaffold-skill` — create a new skill with proper structure, trigger keywords, examples, anti-patterns, decision guide
- `/scaffold-rule` — create a new rule file with DO/DON'T format and deep reference
- `/scaffold-knowledge` — create a new knowledge doc (patterns reference with code examples)
- `/scaffold-agent` — create a new specialized agent definition
- `/kit-setup` — interactive configuration wizard: walks through kit.config.md and registers hooks in settings.json

### Quality & Auditing
- `/skill-auditor` — review a skill for quality: completeness, trigger keywords, examples, anti-patterns, decision guide
- `/kit-health-check` — full kit audit: structure, coverage gaps, quality scores, installability

### Evolution & Maintenance
- `/self-evolution` — analyze kit-building patterns across sessions and propose template/rule improvements
- `/kit-packager` — package kit as installable artifact with `.claude-plugin/plugin.json` manifest and marketplace metadata (install.sh generated optionally for manual/offline installs)

### Meta (auto-active)
- `instinct-system` — learns kit-building patterns from each session (confidence-scored hypotheses)
- `self-correction-loop` — captures every correction into permanent MEMORY.md rules
- `autonomous-loops` — bounded scaffold/fix/improve iteration loops
- `learning-log` — captures discoveries, gotchas, and decisions each session
- `convention-learner` — detects and enforces this kit's own conventions
