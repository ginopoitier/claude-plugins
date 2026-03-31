# Auto-Capture Triggers

Rules for when Claude must proactively capture memories without waiting to be asked.

## High-Confidence Triggers (always capture immediately)

These patterns mandate immediate capture:

| Pattern | Memory Type | Action |
|---------|-------------|--------|
| User says "remember this" / "don't forget" | any — classify via memory_classify | Capture immediately, confirm |
| User says "always do X" / "never do Y" | `feedback` | Capture as behavioral rule |
| User corrects Claude's approach ("no, use X", "that's wrong") | `feedback` | Capture generalized rule |
| User introduces a system/tool with a location ("our Jira board at...") | `reference` | Capture the pointer |
| User shares their role, expertise, or background | `user` | Capture profile fact |
| User mentions a deadline or active initiative | `project` | Capture with absolute date |

## Medium-Confidence Triggers (capture after confirming)

Ask user before capturing:

| Pattern | Suggested Action |
|---------|-----------------|
| Same approach corrected twice in one session | "You've corrected this twice — should I add a permanent rule?" |
| Discovery of a non-obvious behavior/bug | "Should I capture this as a learning-log entry?" |
| Undocumented architectural decision revealed | "Should I capture this decision context?" |
| Instinct reaches 0.9 confidence | "I've observed [pattern] N times. Promote to permanent memory?" |

## Capture Flow

When a high-confidence trigger fires, follow this exact sequence:

```
1. CLASSIFY — call memory_classify to determine type and suggested name
2. GENERALIZE — broaden specific corrections to class-level rules
   "Don't use console.log in payments.ts" → "Never use console.log anywhere — use structured logger"
3. DEDUPLICATE — call memory_search with the suggested name before storing
   If a similar memory exists, UPDATE it rather than create a duplicate
4. STORE — call memory_store with correct type, name, description, and body
5. CONFIRM — tell user: "Captured to memory: [name] ([type])"
```

## DO NOT Auto-Capture

**DO NOT** capture without user awareness — always confirm after storing.

**DO NOT** capture routine code explanations ("here's how this function works").

**DO NOT** capture things the user hasn't validated ("I think you prefer X").

**DO NOT** capture session-specific context (current task state, open files, work-in-progress).

**DO NOT** capture information the user explicitly said is temporary ("just for now", "one-time").

## Correction vs. Insight Distinction

```
CORRECTION (capture as feedback):
  User: "No, we don't use XYZ here"
  → feedback memory: behavioral rule the user must reinforce

INSIGHT (capture as learning-log):
  Claude discovers: "X behaves differently when Y is configured"
  → learning-log entry: descriptive discovery, not a rule

ONLY promote learning-log entries to memory when:
  - The same insight appears 3+ times
  - User explicitly says "remember this"
```

## Generalization Protocol

Before storing any feedback memory, generalize the specific correction:

```
SPECIFIC: "Don't use DateTime.Now in the Orders handler"
STEP 1: Is this specific to Orders? → No, applies everywhere
STEP 2: What's the principle? → Non-deterministic; breaks tests
STEP 3: What's the broadest correct statement?
GENERALIZED: "Always use TimeProvider instead of DateTime.Now/UtcNow — non-deterministic values break tests"
```

One generalized rule prevents many future mistakes. One specific rule only prevents one.
