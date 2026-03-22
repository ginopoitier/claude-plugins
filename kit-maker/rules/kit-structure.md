# Rule: Kit Structure

Every kit must be a self-contained directory installable via Claude Code's native plugin system.

## Required Layout

```
kits/{kit-name}/
  CLAUDE.md               # Entry point — loads all rules, lists all skills
  .claude-plugin/
    plugin.json           # Official Claude Code plugin manifest (required for /plugin install)
    marketplace.json      # Optional — marketplace registration metadata
  config/
    kit.config.template.md  # User-fillable config values (one per kit)
  rules/                  # Always-loaded domain rules (3–8 files)
    {rule-name}.md
  skills/                 # Lazy-loaded behaviors (subdirs with SKILL.md)
    {skill-name}/
      SKILL.md
  knowledge/              # Reference docs loaded on demand
    {topic}.md
  templates/              # Scaffolding templates for the kit's domain
    {template-name}/
  agents/                 # Specialized agent definitions
    {agent-name}.md
  hooks/                  # Shell scripts for automated enforcement
    {hook-name}.sh
```

## CLAUDE.md Structure (required sections, in order)

1. `# Kit Name` — one-line description of what the kit does
2. `## Always-Active Rules` — `@~/.claude/rules/{kit-name}/...` references
3. `## Meta — Always Apply` — context-discipline, model-selection, verification-loop
4. `## Self-Improvement — Auto-Active` — instinct-system, self-correction-loop (if included)
5. `## Skills Available` — grouped by category, one line per skill with description

## Install Convention

Claude Code's native `/plugin install` command reads `.claude-plugin/plugin.json` and handles installation automatically. This is the primary installation mechanism — no `install.sh` required.

- Rules install to: `~/.claude/rules/{kit-name}/`
- Skills install to: `~/.claude/skills/` (flat — skills share the namespace)
- Knowledge installs to: `~/.claude/knowledge/{kit-name}/`
- Agents install to: `~/.claude/agents/`
- Hooks register via: `~/.claude/settings.json`

An `install.sh` script is **optional** — useful for manual/offline installation or CI environments where the Claude Code CLI is not available. It is not required for plugin distribution via the marketplace or `/plugin install`.

## DO
- Always include at least one **meta skill**: context-discipline + model-selection
- Always include **self-improvement skills**: instinct-system + self-correction-loop
- Keep rules **always-loaded** (≤8, high value) — everything else is lazy
- Include a `config/kit.config.template.md` if the kit needs user-specific values
- Every kit ships with a `/kit-health-check` equivalent for its domain

## DON'T
- Don't put knowledge docs in `rules/` — rules are loaded every session, knowledge is loaded on demand
- Don't create rules that are just prose — rules must have DO/DON'T structure
- Don't create skills without trigger keywords — they'll never auto-load
- Don't reference hardcoded paths in CLAUDE.md — use `~/.claude/` prefix always
