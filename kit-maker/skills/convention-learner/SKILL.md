---
name: convention-learner
description: >
  Detects and enforces project-specific coding conventions by analyzing
  existing codebase patterns using Grep, Glob, and config files.
  Works for any language or framework — learns naming, structure, style, and test
  organization from what already exists. Load when: "conventions", "coding standards",
  "project patterns", "enforce style", "detect patterns", "learn conventions", "code consistency".
user-invocable: false
allowed-tools: Read, Write, Grep, Glob, Bash
---

# Convention Learner

## Core Principles

1. **Observe before enforcing** — Never impose conventions without first analyzing the existing codebase. A project with 200 files following one pattern should get a 201st file that matches. Detect first, then match.
2. **Project conventions override kit defaults** — If the project uses `snake_case` for files, follow that even if another convention is theoretically better. Explicit config files (`eslint`, `.editorconfig`, `pyproject.toml`) always win.
3. **Use Grep and Glob for analysis** — Pattern-match against existing files to find naming conventions, structural patterns, and style decisions. Config files provide explicit overrides.
4. **Document findings** — After detecting conventions, suggest adding them to the project's CLAUDE.md. Undocumented conventions are lost when the original developers leave.
5. **Consistency over perfection** — A project consistently using one convention is better than a project with a "better" convention applied inconsistently.

## Patterns

### Convention Detection Flow

Systematic analysis to understand a project's coding conventions. Run this when joining an existing project or before generating new code.

**Step 1: Project Structure Analysis**

```bash
# Map top-level structure
ls -la

# Identify source layout pattern
find . -type d -maxdepth 3 | grep -v node_modules | grep -v .git | head -40

# Detect architecture pattern:
# - Feature folders: src/features/, src/modules/, src/{domain}/
# - Layer folders: src/controllers/, src/services/, src/repositories/
# - Flat: src/ with all files together
```

**Step 2: File Naming Patterns**

```bash
# Sample 10-20 source files to detect naming convention
find src/ -name "*.{js,ts,py,go,rs,cs,java}" | head -20

# Detect:
# - kebab-case: user-service.ts, order-handler.py
# - PascalCase: UserService.ts, OrderHandler.cs
# - snake_case: user_service.py, order_handler.py
# - camelCase: userService.js, orderHandler.js
```

**Step 3: Naming Suffix/Prefix Conventions**

```bash
# Detect suffix patterns (services, handlers, controllers, etc.)
find src/ -type f | sed 's/.*\///' | sed 's/\..*//' | sort | \
  grep -oE '[A-Z][a-z]+$' | sort | uniq -c | sort -rn | head -20

# Detect test file naming
find . -name "*.test.*" -o -name "*.spec.*" -o -name "*_test.*" -o -name "*Tests.*" | head -10
```

**Step 4: Explicit Convention Files**

```bash
# Check for linting/formatting configs
ls .editorconfig .eslintrc* .prettierrc* .pylintrc pyproject.toml \
   .rubocop.yml stylua.toml .clang-format 2>/dev/null

# Check for build/dependency configs that reveal conventions
ls package.json Cargo.toml go.mod *.csproj pom.xml build.gradle 2>/dev/null

# Read any found config for style rules
cat .editorconfig 2>/dev/null
cat .prettierrc 2>/dev/null
```

**Step 5: Test Organization**

```bash
# Detect test location pattern
find . -name "*.test.*" -o -name "*_test.*" -o -name "*Tests.*" | head -20

# Patterns:
# Co-located: src/user/user.test.ts (next to source)
# Separate: tests/user/test_user.py (separate test dir)
# Mirror: src/user.cs → tests/UserTests.cs (mirrored structure)
```

**Step 6: Build Convention Summary**

Compile findings:

```markdown
## Detected Conventions

### File Naming
- Source files: kebab-case (detected: 18/20 files)
- Test files: *.test.ts co-located with source

### Directory Structure
- Feature-organized: src/{feature}/{component}
- Shared utilities: src/shared/

### Code Style
- Inferred from .prettierrc: 2-space indent, single quotes
- No explicit naming rules beyond file-level

### Explicit Overrides
- .editorconfig: tab_width = 2, end_of_line = lf
```

### Convention Enforcement

**When Generating Code — match detected patterns:**

```
# Example: project uses kebab-case files + feature folders
# Generate: src/orders/create-order.ts  (not: src/orders/CreateOrder.ts)

# Example: project uses PascalCase class names + private underscore fields
# Generate: class OrderService { private _repo: ... }  (not: orderService or _OrderService)

# Example: project's tests are co-located *.test.ts
# Generate: src/orders/create-order.test.ts  (not: tests/create-order.test.ts)
```

**When Reviewing — flag deviations:**

```
⚠️ Convention violation: "UserService.js" but project convention is kebab-case
   (detected in 18/20 existing service files: user-service.js, order-service.js...)
   Suggest renaming to: user-service.js
```

**Suggesting Enforcement Config:**

After detecting patterns, suggest config to enforce them automatically:

```json
// .eslintrc: if project uses camelCase variables
{ "rules": { "camelcase": "error" } }
```

```toml
# pyproject.toml: if project uses black
[tool.black]
line-length = 88
```

```ini
# .editorconfig: if project uses 2-space indent
[*.{js,ts,py}]
indent_size = 2
```

### Recurring Antipattern Tracking

Track patterns that keep appearing in code review across sessions:

```bash
# Search for patterns that were flagged before
grep -rn "TODO\|FIXME\|console\.log\|print(" src/ | wc -l
```

When a pattern recurs, add it explicitly to CLAUDE.md:

```markdown
## Conventions
- **NEVER use console.log in production code** — use the project logger (8 instances found)
```

## Anti-patterns

### Enforcing Without Detecting

```
# BAD — imposing a convention without checking what exists
"All service files should be PascalCase"
# But this project uses snake_case_service.py throughout

# GOOD — detect first, then follow what exists
→ Grep reveals: 15/15 service files use snake_case
"This project uses snake_case for service files. Matching that convention."
```

### Ignoring Explicit Config Files

```
# BAD — ignoring .editorconfig / .eslintrc because kit prefers otherwise
# .eslintrc says: "quotes": ["error", "double"]
# But generating single-quoted strings anyway

# GOOD — explicit config files always win
"Your .eslintrc requires double quotes.
I'll use double quotes to match your project settings."
```

### Detecting From Too Few Samples

```
# BAD — one file as the basis for a convention
→ Found 1 file using PascalCase → imposing PascalCase everywhere

# GOOD — sample broadly, require majority before concluding
→ Found 20/22 files using kebab-case, 2 using PascalCase
→ Convention: kebab-case (90% majority)
→ Note: 2 files deviate from convention (flag in review)
```

## Decision Guide

| Scenario | Action | Tool |
|----------|--------|------|
| Joining existing project | Run full detection flow (Steps 1-6) | Grep, Glob, Read config |
| Generating new code | Check detected conventions first | Previous detection results |
| Convention conflict (kit vs project) | **Project wins** | — |
| No conventions detected | Use kit defaults, document them | architecture skill |
| Explicit config file exists | Trust it completely — don't override | Read config file |
| Recurring antipattern | Add to CLAUDE.md conventions | Grep history |
| Pattern seen in 1 file | Create instinct at 0.3 confidence via instinct-system | instinct-system |
| Pattern confirmed in 80%+ files | Treat as convention, enforce it | instinct-system |
| Conflicting patterns (50/50 split) | Flag to user — don't guess | Report both patterns |
