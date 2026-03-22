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
| "Package kit" / "Prepare for publishing" | `/kit-packager` | Manifest + install script |
| "Improve the kit" / "Self-improve" | `/self-evolution` | Analyzes patterns → proposes improvements |
| "What did I learn?" / "Session summary" | `learning-log` skill | Organic discoveries log |
| "Remember this" / "Don't do that again" | `self-correction-loop` skill | Permanent memory capture |
| "Which model should I use?" | `model-selection` skill | Cost-optimal model routing |
| "Context is filling up" | `context-discipline` skill | Subagent delegation guidance |

## Subagent Recommendations

| Task | Agent Type | Model |
|------|-----------|-------|
| Explore kit structure | Explore | haiku |
| Write a complete skill | general-purpose | sonnet |
| Audit kit architecture | Plan | opus |
| Fix build/install errors | build-error-resolver | sonnet |
| Review skill quality | code-reviewer | sonnet |
