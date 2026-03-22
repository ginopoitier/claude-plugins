# Rule: Git Workflow

## DO
- Use **feature branches** from `main`: `feature/`, `fix/`, `chore/` prefixes
- Write **conventional commits**: `feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `test:`
- Keep commits **atomic** — one logical change per commit
- **Squash** before merging a feature branch (clean history on main)
- Write PR descriptions explaining *why*, not *what* (the diff shows what)
- Use **draft PRs** for work in progress — don't leave unfinished branches open
- Reference issue/ticket numbers in commit messages: `feat: add order cancellation (#42)`
- Always pull `--rebase` to keep a linear history: `git pull --rebase origin main`
- Tag releases with semantic versioning: `v1.2.3`

## DON'T
- Don't commit directly to `main` — always via PR
- Don't commit `.env` files, secrets, or build artifacts (`bin/`, `obj/`, `node_modules/`)
- Don't force-push to `main` or shared branches
- Don't leave `TODO:` comments in committed code — create an issue instead
- Don't merge without at least one review
- Don't use `git add .` blindly — stage files explicitly to avoid committing junk
- Don't rewrite public history (amend, rebase) on branches others are working from

## Branch Naming
```
feature/add-order-cancellation
fix/payment-null-reference
chore/upgrade-ef-core-9
docs/update-deployment-guide
refactor/extract-order-service
test/add-integration-tests-orders
```

## Commit Message Format
```
<type>(<scope>): <short summary>

[optional body: why this change was needed]

[optional footer: Breaking changes, issue refs]
```

## .gitignore Essentials
Always include in every .NET project:
- `bin/`, `obj/`, `.vs/`, `*.user`
- `*.env`, `appsettings.*.json` (except `Development`)
- `publish/`, `artifacts/`
