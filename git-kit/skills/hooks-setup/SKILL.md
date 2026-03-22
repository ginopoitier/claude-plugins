---
name: hooks-setup
description: >
  Install and configure git hooks — pre-commit, commit-msg, pre-push, and
  post-merge. Covers local hooks, shared hooks via the repo, and common
  hook patterns. Load this skill when: "git hooks", "pre-commit hook",
  "commit-msg hook", "pre-push hook", "lint on commit", "validate commit message",
  "husky", "lefthook", "hook setup", "automate git", "enforce conventions in git".
user-invocable: true
argument-hint: "[pre-commit | commit-msg | pre-push | --list | --remove <hook>]"
allowed-tools: Bash, Read, Write
---

# Hooks Setup

## Core Principles

1. **Hooks run locally** — git hooks are not enforced by the remote. They only run on the machine where they're installed. Shared hooks need a setup step per developer.
2. **Fast hooks ship** — a hook that takes 30+ seconds will be bypassed with `--no-verify`. Keep hooks under 5 seconds.
3. **Warn, don't always block** — pre-commit hooks that block every commit for trivial issues get `--no-verify`'d into uselessness. Reserve hard blocks for security/correctness issues.
4. **Document the hooks** — add a `## Git Hooks` section to the project README explaining what hooks are installed and how to install them.
5. **`core.hooksPath` for shared hooks** — commit hooks to a `hooks/` directory in the repo, configure `core.hooksPath` in the setup script.

## Patterns

### Local hook installation (no tool)

```bash
# Hooks live in .git/hooks/ — create an executable script
cat > .git/hooks/pre-commit << 'EOF'
#!/bin/bash
# Run linter before every commit
npm run lint --silent
EOF
chmod +x .git/hooks/pre-commit
```

### Shared hooks via core.hooksPath

```bash
# Commit hooks to the repo
mkdir -p hooks/
cat > hooks/pre-commit << 'EOF'
#!/bin/bash
npm run lint --silent
EOF
chmod +x hooks/pre-commit

# Each developer runs once after cloning:
git config core.hooksPath hooks/

# Or in a setup script:
echo 'git config core.hooksPath hooks/' >> scripts/setup.sh
```

### Common hook patterns

**pre-commit — lint and format:**
```bash
#!/bin/bash
set -e
echo "Running pre-commit checks..."

# Lint only staged files
STAGED=$(git diff --cached --name-only --diff-filter=ACM | grep -E '\.(ts|js)$' || true)
if [ -n "$STAGED" ]; then
  echo "$STAGED" | xargs npx eslint --fix-dry-run --quiet
fi
```

**commit-msg — validate message format:**
```bash
#!/bin/bash
MSG=$(cat "$1")
PATTERN='^(feat|fix|docs|style|refactor|test|chore|perf|ci)(\(.+\))?: .{1,72}'

if ! echo "$MSG" | grep -qE "$PATTERN"; then
  echo "❌ Commit message must match conventional commits format:"
  echo "   type(scope): subject  (max 72 chars)"
  echo "   Types: feat fix docs style refactor test chore perf ci"
  exit 1
fi
```

**pre-push — run tests before pushing:**
```bash
#!/bin/bash
echo "Running tests before push..."
npm test --silent
if [ $? -ne 0 ]; then
  echo "❌ Tests failed. Push aborted."
  exit 1
fi
echo "✓ Tests passed."
```

**post-merge — install dependencies after pull:**
```bash
#!/bin/bash
# Re-install if package.json changed
if git diff-tree -r --name-only --no-commit-id ORIG_HEAD HEAD | grep -q "package.json"; then
  echo "package.json changed — running npm install..."
  npm install
fi
```

### Using Lefthook (recommended for teams)

```bash
npm install --save-dev lefthook

# lefthook.yml
pre-commit:
  commands:
    lint:
      glob: "*.{ts,js}"
      run: npx eslint {staged_files}

commit-msg:
  scripts:
    validate:
      runner: bash

# Install for each developer:
npx lefthook install
```

## Anti-patterns

### Slow hooks that block commit flow

```
# BAD — running full test suite in pre-commit
#!/bin/bash
npm test   # 2 minutes → developers use --no-verify immediately

# GOOD — run only fast checks in pre-commit, full tests in pre-push
# pre-commit: lint staged files (< 3s)
# pre-push: run test suite (acceptable gate before sharing)
```

### Hooks that aren't executable

```
# BAD — hook file exists but doesn't run
ls -la .git/hooks/pre-commit
# -rw-r--r-- (no execute bit)

# GOOD — always chmod after creating
chmod +x .git/hooks/pre-commit
```

### Local-only hooks with no setup documentation

```
# BAD — hooks only on one developer's machine, others wonder why CI catches what local doesn't
# GOOD — commit to hooks/ dir, document setup in README
## Git Hooks
Run `git config core.hooksPath hooks/` once after cloning.
```

## Decision Guide

| Scenario | Approach |
|----------|---------|
| Solo project, simple hook | Script in `.git/hooks/` |
| Team project, shared hooks | `hooks/` dir + `core.hooksPath` + setup script |
| Team with multiple languages | Lefthook or Husky |
| Validate commit message format | `commit-msg` hook with regex |
| Lint before committing | `pre-commit` hook on staged files only |
| Run tests before pushing | `pre-push` hook |
| Auto-install deps after pull | `post-merge` hook checking package.json diff |
| Hook is too slow | Move to `pre-push`, or make it opt-in |
