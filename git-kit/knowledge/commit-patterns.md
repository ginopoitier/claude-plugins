# Commit Patterns

## Conventional Commits Specification

Full spec: `<type>(<scope>): <description>`

### Type Reference

| Type | When to Use | Example |
|------|-------------|---------|
| `feat` | New feature visible to users | `feat(auth): add OAuth2 login` |
| `fix` | Bug fix | `fix(cart): prevent negative quantity` |
| `docs` | Documentation only | `docs(readme): update install steps` |
| `style` | Formatting, whitespace, no logic | `style(api): fix indentation` |
| `refactor` | Restructure without behavior change | `refactor(orders): extract validation` |
| `test` | Adding or fixing tests | `test(auth): add refresh token tests` |
| `chore` | Tooling, deps, CI, config | `chore(deps): upgrade typescript 5.4` |
| `perf` | Performance improvement | `perf(search): cache indexed results` |
| `ci` | CI/CD configuration | `ci: add parallel test matrix` |

### Breaking Changes

```
feat(api)!: change pagination from offset to cursor

BREAKING CHANGE: The /users endpoint now returns a cursor instead of offset.
Update client code to use `cursor` instead of `page` parameter.
```

The `!` suffix and `BREAKING CHANGE:` footer both signal a breaking change.

## Anatomy of a Good Commit Message

```
feat(auth): add JWT refresh token rotation         ← subject (max 72 chars)
                                                    ← blank line separates subject/body
Refresh tokens now rotate on each use to limit     ← body: explains WHY
the blast radius of a stolen token. Old tokens     ← wrap at 72 chars
are invalidated immediately after rotation.

Implements security recommendation from #AUTH-42.  ← references
Fixes #891                                          ← footer keywords
```

### Footer Keywords (GitHub/GitLab/Bitbucket)

```
Fixes #123          # closes the issue on merge
Closes #456         # same as Fixes
Refs #789           # references without closing
Co-authored-by: Name <email>
Reviewed-by: Name <email>
```

## Atomic Commit Checklist

Before committing, verify:
- [ ] Does this commit do exactly one thing?
- [ ] Can I describe it in one sentence without "and"?
- [ ] Are all staged changes related to this one thing?
- [ ] Does the working tree still build after this commit alone?
- [ ] Is the commit message in imperative mood?
- [ ] Is the subject under 72 characters?

## Common Anti-Pattern Messages and Fixes

| Bad Message | Better Message |
|-------------|----------------|
| `fix` | `fix(auth): resolve null pointer on expired session` |
| `WIP` | Don't commit WIP — stash or branch instead |
| `updates` | `chore(deps): update eslint to 9.x` |
| `refactored code` | `refactor(orders): extract shipping calculator` |
| `added tests` | `test(checkout): add edge cases for discount stacking` |
| `various fixes` | Split into separate commits per fix |
