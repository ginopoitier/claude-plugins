# Kit Maker — Agent Routing Table

When a user request matches an intent below, route to the listed skill or agent.

## Routing Table

| User Intent | Route | Notes |
|-------------|-------|-------|
| "Create a new kit" / "Scaffold a kit" | `/scaffold-kit` | Full interactive wizard |
| "Create a skill" / "New skill" | `/scaffold-skill` | Single SKILL.md with all sections |
| "Create a rule" / "New rule file" | `/scaffold-rule` | DO/DON'T format rule |
| "Create a knowledge doc" / "New reference doc" | `/scaffold-knowledge` | Deep reference with code examples |
| "Create an agent" / "New agent definition" | `/scaffold-agent` | Scoped agent with task boundaries |
| "Set up kit config" / "Configure kit" | `/kit-setup` | Interactive config wizard |
| "Audit this skill" / "Review skill quality" | `/skill-auditor` | 7-dimension quality grade |
| "Check kit health" / "Is this kit ready?" | `/kit-health-check` | 8-dimension full audit |
| "Package kit" / "Prepare for publishing" | `/kit-packager` + `marketplace-writer` agent | Manifest sync + installability |
| "Update plugin.json" / "Sync marketplace" | `marketplace-writer` agent | Version sync, catalog update |
| "Create config template" / "What config keys are needed?" | `config-writer` agent | Scans CLAUDE.md + skills |
| "Add a hook" / "Automate on file write" | `/scaffold-hook` + `hook-writer` agent | Script + hooks.json |
| "Write a knowledge doc" | `/scaffold-knowledge` + `knowledge-writer` agent | Deep reference with examples |
| "Add a rule" / "Always enforce X" | `/scaffold-rule` + `rule-writer` agent | DO/DON'T, ≤60 lines |
| "Create a template" / "Scaffold boilerplate" | `template-writer` agent | Placeholder-convention templates |
| "Improve the kit" / "Self-improve" | `/self-evolution` | Analyzes patterns → proposes improvements |

## Specialist Agents (kit-maker)

| Agent | Trigger | Model | Color |
|-------|---------|-------|-------|
| `kit-auditor` | "audit kit", "is this kit ready?", `/kit-health-check` | sonnet | purple |
| `skill-writer` | "write a skill", "create SKILL.md", `/scaffold-skill` | sonnet | green |
| `marketplace-writer` | "update plugin.json", "publish kit", "sync marketplace", `/kit-packager` | sonnet | green |
| `config-writer` | "create config template", "missing config keys", `/kit-setup` | haiku | yellow |
| `hook-writer` | "add a hook", "automate on write", "lifecycle event", `/scaffold-hook` | sonnet | yellow |
| `knowledge-writer` | "write knowledge doc", "deep reference", `/scaffold-knowledge` | sonnet | blue |
| `rule-writer` | "add a rule", "always-active guidance", "naming convention", `/scaffold-rule` | haiku | green |
| `template-writer` | "create template", "scaffold boilerplate", "code generation template" | sonnet | yellow |

## Subagent Recommendations

| Task | Agent Type | Model |
|------|-----------|-------|
| Explore kit structure | Explore | haiku |
| Write a complete skill | skill-writer | sonnet |
| Write a rule | rule-writer | haiku |
| Write a knowledge doc | knowledge-writer | sonnet |
| Write a hook | hook-writer | sonnet |
| Write config template | config-writer | haiku |
| Update marketplace manifests | marketplace-writer | sonnet |
| Create scaffold templates | template-writer | sonnet |
| Audit kit architecture | Plan | opus |
| Fix build/install errors | build-error-resolver | sonnet |
| Review skill quality | kit-auditor | sonnet |
