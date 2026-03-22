# Commit Conventions

## DO
- Write commit messages in imperative mood: "Add feature" not "Added feature"
- Keep the subject line under 72 characters
- Use conventional commit format when `COMMIT_STYLE=conventional`: `type(scope): message`
- Make commits atomic — one logical change per commit
- Reference issue numbers in the footer: `Fixes #123`
- Use `git add -p` to stage hunks selectively when multiple logical changes are present
- Write a body when the subject alone doesn't explain *why* the change was made

## DON'T
- Don't commit with messages like "WIP", "fix", "asdf", or "update stuff"
- Don't mix unrelated changes in a single commit
- Don't commit generated files, build artifacts, lock file noise, or secrets
- Don't use past tense — use imperative ("Fix the bug", not "Fixed the bug")
- Don't exceed 72 chars in the subject line (truncated in `git log --oneline` and GitHub)
- Don't amend commits that have already been pushed and shared

## Conventional Commit Types (when COMMIT_STYLE=conventional)
- `feat` — new feature
- `fix` — bug fix
- `docs` — documentation only
- `style` — formatting, whitespace, no logic change
- `refactor` — restructure without behavior change
- `test` — adding or fixing tests
- `chore` — build, tooling, dependencies
- `perf` — performance improvement
- `ci` — CI/CD configuration changes

## Examples

```
# GOOD — imperative, under 72 chars
feat(auth): add JWT refresh token rotation

# GOOD — with body explaining why
fix(orders): prevent double-submit on slow connections

The submit button was not disabled fast enough on mobile,
causing duplicate order submissions on 3G connections.

Fixes #892

# BAD — vague, past tense, no scope
Fixed some stuff in the order form
```

## Deep Reference
@~/.claude/knowledge/git-kit/commit-patterns.md
