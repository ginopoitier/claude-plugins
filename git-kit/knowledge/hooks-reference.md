# Git Hooks Reference

## Hook Execution Order

```
git commit
  └── pre-commit         ← lint, format, tests (fast only)
  └── prepare-commit-msg ← populate default message
  └── commit-msg         ← validate message format
  └── post-commit        ← notifications, logging

git push
  └── pre-push           ← run full test suite, check branch

git merge
  └── pre-merge-commit   ← pre-merge validation
  └── post-merge         ← install deps if lock file changed

git rebase
  └── pre-rebase         ← block rebase on protected branches
  └── post-rewrite       ← cleanup after rebase/amend
```

## Hook Installation Methods

### Method 1: Direct `.git/hooks/` (local only)

```bash
cp hooks/pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

Simple. Not shared. Each developer installs manually.

### Method 2: `core.hooksPath` (shared via repo)

```bash
# Commit hooks to a tracked directory
mkdir hooks/
# Add hook scripts to hooks/

# Each developer runs once after cloning:
git config core.hooksPath hooks/

# Add to onboarding script:
echo 'git config core.hooksPath hooks/' >> scripts/setup.sh
```

Shared across the team. Requires one setup step per developer.

### Method 3: Lefthook (recommended for teams)

```yaml
# lefthook.yml — committed to repo
pre-commit:
  commands:
    lint:
      glob: "*.{ts,js,py}"
      run: npx eslint {staged_files}
    format:
      run: npx prettier --check {staged_files}

commit-msg:
  scripts:
    validate-message:
      runner: bash

pre-push:
  commands:
    tests:
      run: npm test
```

```bash
npm install --save-dev lefthook
npx lefthook install    # each developer runs this once
```

### Method 4: Husky (Node.js projects)

```bash
npm install --save-dev husky
npx husky init

# .husky/pre-commit
npm run lint-staged
```

## Hook Script Template

```bash
#!/bin/bash
# hook-name.sh
# Description: what this hook does
# Install: cp this file to .git/hooks/<hook-name> && chmod +x .git/hooks/<hook-name>

set -e   # exit on first error

# Your hook logic here
echo "Running pre-commit checks..."

# Exit 0 = allow the operation
# Exit non-zero = block the operation
exit 0
```

## Common Hook Patterns

### Lint only staged files (pre-commit, fast)

```bash
#!/bin/bash
STAGED=$(git diff --cached --name-only --diff-filter=ACM | grep -E '\.(ts|js)$' || true)
[ -z "$STAGED" ] && exit 0
echo "$STAGED" | xargs npx eslint --quiet
```

### Validate conventional commit message (commit-msg)

```bash
#!/bin/bash
MSG=$(cat "$1" | grep -v "^#" | head -1)
PATTERN='^(feat|fix|docs|style|refactor|test|chore|perf|ci)(\(.+\))?!?: .{1,72}'
echo "$MSG" | grep -qE "$PATTERN" || { echo "❌ Invalid commit format"; exit 1; }
```

### Auto-install deps after pull (post-merge)

```bash
#!/bin/bash
if git diff-tree -r --name-only --no-commit-id ORIG_HEAD HEAD | grep -q "package.json"; then
  echo "package.json changed — running npm install..."
  npm install --silent
fi
```

### Guard against pushing to protected branches (pre-push)

```bash
#!/bin/bash
BRANCH=$(git branch --show-current)
PROTECTED="main master release"
for b in $PROTECTED; do
  if [ "$BRANCH" = "$b" ]; then
    read -p "Pushing to $b — are you sure? (yes/no): " c
    [ "$c" = "yes" ] || exit 1
  fi
done
```

## Bypassing Hooks

```bash
git commit --no-verify    # skip pre-commit and commit-msg hooks
git push --no-verify      # skip pre-push hook
```

`--no-verify` is an escape hatch — use it intentionally, not habitually. If you're bypassing regularly, the hook is too slow or too strict.
