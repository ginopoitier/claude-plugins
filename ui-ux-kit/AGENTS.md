# Agents — UI/UX Kit

Specialist agents available in ui-ux-kit and their routing rules.

## Agent Roster

| Agent | Model | Domain | When to Use |
|-------|-------|--------|-------------|
| `design-system-architect` | Opus | Token architecture, component tiers, system structure, multi-app strategy | Architecture questions, token naming decisions, design system planning |
| `component-builder` | Sonnet | Vue 3 + Tailwind component implementation, slot patterns, cva variants | Building specific components, fixing component issues, adding variants |
| `style-guide-writer` | Sonnet | Style guide generation, token documentation, component showcase | Generating living docs, token visualization, design system reference |
| `figma-analyst` | Sonnet | Figma API, token extraction, design-to-code mapping, drift detection | Figma sync questions, token extraction, design file analysis |

## Routing Table

| User Intent | Agent |
|-------------|-------|
| "How should I structure my design tokens?" | `design-system-architect` |
| "Should I use standalone or embedded design system?" | `design-system-architect` |
| "What semantic tokens do I need?" | `design-system-architect` |
| "Multi-brand theming strategy" | `design-system-architect` |
| "Build a DsInput component" | `component-builder` |
| "Add a loading variant to DsButton" | `component-builder` |
| "DsModal focus trap not working" | `component-builder` |
| "Generate style guide page" | `style-guide-writer` |
| "Document all color tokens" | `style-guide-writer` |
| "Create component showcase" | `style-guide-writer` |
| "Extract tokens from Figma file" | `figma-analyst` |
| "Figma sync showing drift" | `figma-analyst` |
| "Map Figma variables to CSS tokens" | `figma-analyst` |

## Meta Skill Routing

| Skill | When It Activates |
|-------|------------------|
| `context-discipline` | Always — controls token budget and subagent delegation |
| `model-selection` | Always — routes tasks to Haiku/Sonnet/Opus |
| `instinct-system` | Learns project-specific design patterns and conventions |
| `self-correction-loop` | On any user correction about design decisions |
| `autonomous-loops` | When generating multiple components or full design systems |
| `verification-loop` | After every generation task |
| `learning-log` | During sessions — captures design decisions and gotchas |
