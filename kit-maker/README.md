# Kit Maker

> Meta-kit for creating, auditing, and publishing marketplace-ready Claude Code kits.

Build any Claude Code kit — dev kits, data science kits, security kits — with proper structure, self-improving meta-skills, hooks, templates, and a one-command installer.

## What This Kit Does

Claude Code kits extend Claude's behavior for a specific domain. Kit Maker gives you the scaffolding, quality standards, and automation to build kits that are structured, complete, cost-efficient, and ready to share.

## Skills

| Command | Description |
|---------|-------------|
| `/scaffold-kit` | Interactive wizard: creates a full kit from scratch |
| `/scaffold-skill` | Create a single skill with proper structure, keywords, and examples |
| `/scaffold-rule` | Create a rule file with DO/DON'T format and deep reference |
| `/scaffold-knowledge` | Create a knowledge doc with full code examples |
| `/scaffold-agent` | Create a specialized agent definition |
| `/kit-setup` | Interactive config wizard: walks through kit.config.md and registers hooks |
| `/skill-auditor` | Quality score a skill across 7 dimensions (A–F grades) |
| `/kit-health-check` | Full kit audit: structure, coverage, quality, installability |
| `/kit-packager` | Package kit as distributable artifact with manifest + install script |
| `/self-evolution` | Analyze session patterns and propose kit template improvements |

## Install

```bash
git clone https://github.com/your-org/kit-maker
cd kit-maker
bash install.sh
```

Then add to your `~/.claude/CLAUDE.md`:
```markdown
@~/.claude/rules/kit-maker/skill-format.md
@~/.claude/rules/kit-maker/kit-structure.md
@~/.claude/rules/kit-maker/quality-standards.md
@~/.claude/rules/kit-maker/naming-conventions.md
```

Restart Claude Code. Run `/scaffold-kit` to create your first kit.

## Configure

Edit `~/.claude/kit.config.md`:
```
KITS_BASE_PATH=~/kits/
KIT_AUTHOR=your-name
KIT_DEFAULT_LICENSE=MIT
```

## Self-Improving

Kit Maker includes:
- `instinct-system` — learns kit-building patterns across sessions
- `self-correction-loop` — captures every correction into permanent memory
- `self-evolution` — after building kits, improves its own templates
- `learning-log` — documents discoveries and decisions per session

## Requirements

- Claude Code 1.0.0+

## License

MIT
