# Kit Anatomy — Complete Reference

A Claude Code kit is a directory of files that extend Claude's behavior for a specific domain. When installed, its rules load every session, its skills load on demand, and its knowledge provides deep reference.

## Full Kit Structure

```
kits/{kit-name}/
  CLAUDE.md                   # Entry point (required)
  .claude-plugin/
    plugin.json               # Official Claude Code plugin manifest (required for /plugin install)
  config/
    kit.config.template.md    # User fills this → saves to ~/.claude/kit.config.md
  rules/                      # Always loaded — keep lean (3–8 files)
    {rule-name}.md
  skills/                     # Lazy loaded — can have 50+ skills
    {skill-name}/
      SKILL.md
  knowledge/                  # Loaded on demand by skills
    {topic}.md
  templates/                  # Scaffolding starting points
    {template-name}/
      {files...}
  agents/                     # Specialized agent definitions
    {agent-name}.md
  hooks/                      # Shell scripts for automation
    {hook-name}.sh
```

## CLAUDE.md — The Entry Point

CLAUDE.md is the first thing loaded in every session. It must:

1. Load all rules via `@` references
2. Reference optional shared integrations or hooks when used
3. List all available skills with one-line descriptions

```markdown
# Kit Name

> Brief description of what this kit does.

## Always-Active Rules

@~/.claude/rules/kit-name/rule1.md
@~/.claude/rules/kit-name/rule2.md

## Integrations

All config values from ~/.claude/kit.config.md:
`VALUE1` · `VALUE2` · `VALUE3`

## Skills Available

### Category
- `/skill-name` — what it does
```

## Rules — Always-Loaded Domain Guidance

Rules are the always-on behavioral layer. Every rule fires every session.

**Format:**
```markdown
# Rule: Topic Name

## DO
- Specific, actionable positive guidance

## DON'T
- Specific, actionable prohibitions

## Deep Reference
For full patterns: @~/.claude/knowledge/kit-name/topic.md
```

**What belongs in rules:**
- Naming conventions
- Mandatory patterns (always do X)
- Hard prohibitions (never do Y)
- Quick-reference tables for common decisions

**What does NOT belong in rules:**
- Code examples (too long — put in knowledge)
- Detailed explanations (too verbose — put in knowledge)
- Feature descriptions (put in skills)

## Skills — Lazy-Loaded Behaviors

Skills load only when their trigger keywords appear in conversation, or when explicitly invoked via `/skill-name`. This keeps the always-loaded context small.

**Every SKILL.md structure:**
```
frontmatter (name, description with keywords, user-invocable, allowed-tools)
# Title
## Core Principles (3–5 rules)
## Patterns (code examples, named subsections)
## Anti-patterns (BAD/GOOD pairs)
## Decision Guide (scenario → recommendation table)
```

**Trigger keyword strategy:**
- 5+ keywords minimum, 8+ ideal
- Match exact phrases users say ("socket exhaustion" not "resource leak")
- Cover the problem ("can't connect"), the tool ("AddHttpClient"), and the concept ("typed client")

## Commands

Lightweight orchestrators that invoke skills and agents. Commands contain routing logic, not implementation.

```
{kit-name}/
  commands/
    {namespace}/        # optional grouping (e.g., gsd/ for /gsd:command-name)
      {command-name}.md
```

### When to use commands vs skills

| Need | Use |
|------|-----|
| Shortcut to a single skill | Command (thin wrapper) |
| Route to different skills based on input | Command (routing logic) |
| Multi-step workflow | Command (orchestrator) |
| Implementation logic / domain knowledge | Skill |

### Command frontmatter

```yaml
---
description: >
  One-line description shown in the /command picker.
---
```

### Max size

Commands should be ≤ 200 lines. If longer, the logic belongs in a skill.

See `knowledge/command-format.md` for the full command specification.

## Knowledge — On-Demand Deep Reference

Knowledge docs are loaded by skills when full detail is needed. They're too long to be always-loaded but too valuable to leave out.

**Structure:**
```
# Topic — Subtitle

## Overview (2–3 sentences max)
## Pattern 1 (full code example + explanation)
## Pattern 2
## Anti-patterns
## Reference (NuGet packages, links, versions)
```

## Agents — Specialized Subprocesses

Agent definitions tell Claude how to spin up a focused subprocess for a specific task.

```markdown
---
name: skill-writer
description: Specialized in writing high-quality SKILL.md files from a description
model: sonnet
tools: Read, Write, Edit
---

# Skill Writer Agent

## Task Scope
Write complete, quality-gated SKILL.md files. Given: skill name + domain + patterns.
Returns: complete SKILL.md ready to use (no placeholders).

## Output Format
A single SKILL.md file at the specified path.

## Constraints
- Always include 5+ trigger keywords
- Always include code examples (no pseudocode)
- Always run quality self-check before returning
```

## Hooks — Automation Scripts

Hooks run automatically on Claude Code lifecycle events. Use them to enforce quality without requiring manual steps.

**Most useful hook events:**
- `PostToolUse` on `Write` — validate what was just written
- `Stop` — verify kit state after session ends
- `PreToolUse` on `Bash` — block dangerous commands

**Example: Validate SKILL.md frontmatter on write**
```json
{
  "hooks": {
    "PostToolUse": [{
      "matcher": "Write",
      "hooks": [{
        "type": "command",
        "command": "~/.claude/hooks/kit-name/validate-skill-frontmatter.sh"
      }]
    }]
  }
}
```

## .claude-plugin/plugin.json — Official Plugin Manifest

Claude Code's native plugin system reads this file when you run `/plugin install`. It is the primary distribution format — no `install.sh` required.

```json
{
  "name": "kit-name",
  "version": "1.0.0",
  "description": "One sentence: what this kit does and who it's for",
  "author": {
    "name": "Author Name",
    "email": "author@example.com"
  },
  "license": "MIT",
  "keywords": ["domain", "technology", "use-case"],
  "commands": "./skills/"
}
```

`mcpServers` is optional — add it only if the kit requires an MCP server. See `marketplace-spec.md` for the full `plugin.json` reference.

## Install Conventions

| Component | Source | Destination |
|-----------|--------|-------------|
| Rules | `kit-name/rules/` | `~/.claude/rules/kit-name/` |
| Skills | `kit-name/skills/*/` | `~/.claude/skills/*/` (flat) |
| Knowledge | `kit-name/knowledge/` | `~/.claude/knowledge/kit-name/` |
| Agents | `kit-name/agents/` | `~/.claude/agents/` |
| Hooks | `kit-name/hooks/` | `~/.claude/hooks/kit-name/` |

Skills share a flat namespace — two kits cannot have skills with the same directory name. Use domain-prefixed names when there's risk of collision: `data-health-check` not just `health-check`.

## Config System

Users have different API keys, paths, and preferences. The config system handles this:

```markdown
<!-- config/kit.config.template.md -->
# Kit Name — Configuration Template
# Copy to ~/.claude/kit.config.md and fill in your values.

## Your Settings
SETTING_1=value1
SETTING_2=value2
```

Reference in CLAUDE.md:
```markdown
## Configuration
All values from `~/.claude/kit.config.md`:
`SETTING_1` · `SETTING_2`
When missing → run `/kit-setup`
```
