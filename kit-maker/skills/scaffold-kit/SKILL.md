---
name: scaffold-kit
description: >
  Interactive wizard for creating a complete Claude Code kit from scratch.
  Produces a fully structured kit with CLAUDE.md, rules, skills, knowledge docs,
  templates, agents, hooks, and a marketplace-ready manifest.
  Load this skill when: "create a kit", "new kit", "scaffold kit", "build a kit",
  "kit from scratch", "kit wizard", "make a plugin", "kit manifest".
user-invocable: true
argument-hint: "[kit name and domain]"
allowed-tools: Read, Write, Edit, Bash, Glob
---

# Scaffold Kit

## Core Principles

1. **Domain-first** — Before creating any files, deeply understand the domain. A dev kit looks nothing like a data-science kit or a security kit. Don't reuse structure blindly.
2. **Optional shared integration** — Shared memory, token optimization, and self-improvement behavior should be provided by separate support kits and hooks, not copied directly into every kit.
3. **Install-ready from day one** — The `plugin.json` must be correct from the start, and `hooks/check-settings.sh` + `hooks/hooks.json` must exist. An uninstallable or silently-misconfigured kit is a broken kit.
4. **Quality-gated release** — Run `/kit-health-check` before considering the kit ready for distribution.

## Patterns

### Kit Creation Wizard

**Phase 1: Domain Discovery**
Ask:
1. What is the kit's domain? (e.g., data science, DevOps, mobile development)
2. Who is the target user? (their role, expertise level, workflow)
3. What problems does this kit solve that Claude doesn't solve well by default?
4. What are the 5–8 most common tasks in this domain?
5. What tools/technologies are central? (determines rules + knowledge topics)
6. Does the kit need a config system? (user-specific values like API keys, paths)
7. Does the kit warrant a custom MCP server? (complex tool integration, external APIs)

**Phase 2: Structure Planning**
Based on answers, produce a plan:

```
kit-name/
  CLAUDE.md
  .claude-plugin/
    plugin.json               # required for /plugin install
  config/
    kit.config.template.md    # if user-specific config needed
    project.config.template.md  # project-level config (optional)
  rules/                      # 3–6 always-loaded rules
    {domain}-conventions.md
    {domain}-patterns.md
    quality-standards.md
    {tool-specific}.md
  skills/                     # 8–15 skills
    # Domain-specific
    {domain}-setup/           # always include — runs on first use
    scaffold-{primary-artifact}/
    {domain}-health-check/
    {domain}-auditor/
    [3–6 more domain skills]
  knowledge/                  # 3–6 reference docs
    {domain}-patterns.md
    [specific topic docs]
  templates/                  # starter templates
    [domain-specific templates]
  agents/                     # 2–4 specialized agents
    [role-based agents]
  hooks/                      # Required — always include both
    check-settings.sh         # checks config exists + required fields; exit 0 always
    hooks.json                # registers check-settings.sh as UserPromptSubmit hook
```

> Do NOT create `kit.manifest.json` or `install.sh`. The marketplace catalog lives at the repo root `.claude-plugin/marketplace.json` — add a new entry there when creating a new kit.

**Phase 3: Skeleton Generation**
Generate in this order (each can be parallel):
1. `CLAUDE.md` — entry point, references all rules and lists all skills
2. `.claude-plugin/plugin.json` — name, version, description, keywords, commands pointer
3. `hooks/check-settings.sh` + `hooks/hooks.json` — settings enforcement on every prompt
4. All rule files (3–6 files)
5. All skill SKILL.md files (meta + domain)
6. All knowledge docs
7. Templates
8. Agent definitions
9. Config template

**Phase 4: Optional shared integration**
Shared behavior is provided by separate kits and hook-driven automation. Use dedicated documentation for integrating shared memory, token optimization, and self-improvement features.

```bash
# Optional: integrate with separate support kits via hooks
# See the dedicated integration documentation for installation and hook registration.
```

**Phase 5: Quality Gate**
Run `/kit-health-check` on the generated kit. Fix all blockers before marking done.

### Minimal Kit (fast scaffold)

For simple kits (≤5 skills, single domain), use the minimal template:

```
kit-name/
  CLAUDE.md                     # rules + 5 skills listed
  .claude-plugin/
    plugin.json
  rules/{domain}.md             # single combined rule file
  skills/{5 domain skills}/
  knowledge/{1 reference doc}.md
  hooks/
    check-settings.sh
    hooks.json
```

### plugin.json Format

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

After creating the kit, add an entry for it in the repo root `.claude-plugin/marketplace.json`.

## Anti-patterns

### Don't Start with Files Before Understanding the Domain

```
# BAD — jumping straight to writing
User: "Create a data-science kit"
→ Immediately writes files based on generic assumptions

# GOOD — ask first, then generate
User: "Create a data-science kit"
→ "What language? Python/R/Julia? What tasks? EDA, modeling, deployment?
    Who's the user — junior analyst or senior ML engineer?
    Do you need Jupyter integration?"
→ Generate a kit tailored to actual needs
```

### Don't Skip Optional Shared Support

```
# BAD — kit without optional hook-based improvements
skills/
  scaffold-model/
  data-health-check/
  # No hook automation or shared integration

# GOOD — kit with optional external shared-support
skills/
  scaffold-model/
  data-health-check/
  # Optional separate support handles memory, token optimization, and session discovery
```

### Don't Hardcode Paths in CLAUDE.md

```markdown
<!-- BAD — absolute path breaks on other machines -->
@C:/Users/specific-user/.claude/rules/my-rules.md

<!-- GOOD — always use ~/.claude/ prefix -->
@~/.claude/rules/kit-name/my-rules.md
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Domain has 3+ distinct tools/patterns | Full kit with 10+ skills |
| Simple domain, few patterns | Minimal kit (5 skills, 1 rule file) |
| Kit needs user-specific config | Include config/kit.config.template.md + /kit-setup skill |
| Kit integrates with external API/tool | Include MCP server stub in manifest |
| Kit targets beginners | More knowledge docs, simpler rules, `/getting-started` skill |
| Kit targets experts | Fewer basics, more advanced patterns, code-heavy skills |
| Distributing to a team | Include README.md + add entry to root marketplace.json |

## Execution

Ask the domain-discovery questions, plan the kit structure, then generate all files in the order listed in Phase 3 above.

## Deep Reference
For full kit structure and component anatomy: @~/.claude/knowledge/kit-maker/kit-anatomy.md

$ARGUMENTS
