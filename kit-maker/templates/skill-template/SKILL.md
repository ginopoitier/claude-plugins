---
name: skill-name            # kebab-case, matches directory name
description: >
  One-line description of what this skill does.
  Load this skill when: "keyword1", "keyword2", "keyword3",
  "keyword4", "keyword5", "keyword6", "keyword7", "keyword8".
user-invocable: true        # false if auto-active only
argument-hint: "[argument]" # shown in autocomplete; remove if user-invocable: false
allowed-tools: Read, Write, Edit  # only tools this skill's code examples use
---

# Skill Name

## Core Principles

1. **Principle One** — One sentence explaining why this rule exists and what breaks if you violate it.
2. **Principle Two** — One sentence explaining why this rule exists.
3. **Principle Three** — One sentence explaining why this rule exists.
4. **Principle Four** — Optional fourth principle.

## Patterns

### Primary Pattern Name

```language
// GOOD — brief explanation of why this is the right approach
code example here
```

### Secondary Pattern Name

```language
// GOOD — brief explanation
code example here

// Usage example
usage code here
```

### Third Pattern (if needed)

```language
// Complex example with inline comments explaining WHY
code here  // why this choice
more code  // why this choice
```

## Anti-patterns

### Don't Do The Common Mistake

```language
// BAD — one sentence: why this is wrong and what it causes
bad code example

// GOOD — one sentence: why this is right
good code example
```

### Don't Do The Second Common Mistake

```language
// BAD — one sentence reason
bad code example

// GOOD — one sentence reason
good code example
```

### Don't Do The Third Common Mistake

```language
// BAD
bad code example

// GOOD
good code example
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Scenario 1 | Specific recommendation |
| Scenario 2 | Specific recommendation |
| Scenario 3 | Specific recommendation |
| Scenario 4 | Specific recommendation |
| Scenario 5 | Specific recommendation |
| Scenario 6 | Specific recommendation |

## Execution

One sentence describing exactly what Claude should do when this skill is invoked — the entry point action. Reference the patterns and decision guide above to shape behavior.

$ARGUMENTS
