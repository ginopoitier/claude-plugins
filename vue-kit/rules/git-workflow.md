# Rule: Git Workflow

> Git workflow conventions are owned by **git-kit**.
> Platform-specific PR workflows are owned by **github-kit** or **bitbucket-kit**.
>
> This file is intentionally minimal — do not duplicate rules that live in those kits.

## DO
- Follow the branch naming and commit conventions defined in git-kit rules
- Use `/pr` from github-kit or bitbucket-kit to create pull requests
- Reference Jira ticket keys in branch names so `/review` and `/pr` can auto-link them: `feature/ORD-456-order-status`
- Always work in a feature branch — never commit directly to `main`

## DON'T
- Don't commit `.env` files, secrets, or build artifacts (`bin/`, `obj/`, `node_modules/`)
- Don't force-push to `main` or shared branches

## .gitignore Essentials for .NET Projects
```
bin/
obj/
.vs/
*.user
*.env
appsettings.*.json   # except Development
publish/
artifacts/
```
