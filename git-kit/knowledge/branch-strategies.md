# Branch Strategies

## GitHub Flow (default — simplest)

Best for: continuous deployment, small teams, web apps.

```
main (always deployable)
  └── feature/add-login       ← branch from main
  └── fix/null-pointer        ← branch from main
  └── chore/upgrade-deps      ← branch from main
```

**Workflow:**
1. Branch from `main`
2. Commit work on feature branch
3. Open PR → code review → merge to `main`
4. Deploy `main`

**Protected:** `main` only.

---

## Gitflow (structured — more ceremony)

Best for: versioned software, scheduled releases, large teams.

```
main (tagged releases only)
develop (integration branch)
  └── feature/add-login       ← branch from develop
  └── feature/new-checkout    ← branch from develop
release/2.4.0                 ← branch from develop when ready
hotfix/critical-fix           ← branch from main, merge to main + develop
```

**Workflow:**
1. Feature branches from `develop`
2. Merge features to `develop`
3. Create `release/x.y.z` from `develop` when ready
4. QA/fix on release branch, merge to `main` + tag + merge back to `develop`
5. Hotfixes from `main` → merge to `main` + `develop`

**Protected:** `main`, `develop`.

---

## Trunk-Based Development (fastest — least structure)

Best for: high-trust teams, mature CI/CD, continuous delivery.

```
main (trunk — everyone commits here)
  └── feature/short-lived     ← max 1-2 day branches
```

**Workflow:**
1. Commit directly to `main` OR use very short-lived branches (< 2 days)
2. Use feature flags to hide incomplete work
3. CI runs on every commit — must stay green
4. Deploy continuously or on a schedule

**Protected:** `main` only (but everyone pushes to it daily).

---

## Choosing a Strategy

| Factor | GitHub Flow | Gitflow | Trunk |
|--------|------------|---------|-------|
| Release cadence | Continuous | Scheduled | Continuous |
| Team size | Small–medium | Medium–large | Any |
| Hotfix urgency | Low | High (structured) | Low |
| CI maturity | Moderate | Low | High |
| Feature flags used | Sometimes | Rarely | Always |

## Branch Naming Quick Reference

```
feature/<description>    # new functionality
fix/<description>        # bug fixes
hotfix/<description>     # urgent production fix (gitflow)
release/<version>        # release prep (gitflow)
chore/<description>      # tooling, deps
refactor/<description>   # restructuring
```
