---
name: release
description: >
  Tag a release, create a GitHub release with auto-generated release notes from commits,
  and optionally bump the version. Load this skill when:
  "create release", "tag release", "github release", "release", "publish release",
  "bump version", "release notes", "cut a release", "ship release".
user-invocable: true
argument-hint: "[<version> | --patch | --minor | --major | --list]"
allowed-tools: Bash, Read, Edit
---

# Release

## Core Principles

1. **Semantic versioning** — releases follow `vMAJOR.MINOR.PATCH` (e.g. `v1.3.0`).
2. **Tag before release** — create the git tag locally, push it, then create the GitHub release from that tag.
3. **Release notes from commits** — auto-generate from conventional commits since the last tag; group by type.
4. **Confirm before tagging** — show the version, tag, and notes preview; get confirmation before any `git tag` or `gh release create`.
5. **Never force-push tags** — if a tag already exists, stop and ask the user.

## Patterns

### Detect current version

```bash
# From latest git tag
LATEST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0")
LATEST_VERSION=${LATEST_TAG#v}   # strip leading v

# From a version file (if present — check in order)
for f in Directory.Build.props version.txt package.json; do
  [[ -f "$f" ]] && echo "Found version file: $f"
done
```

### Calculate next version

```bash
IFS='.' read -r MAJOR MINOR PATCH <<< "$LATEST_VERSION"

case "$BUMP" in
  --major) MAJOR=$((MAJOR + 1)); MINOR=0; PATCH=0 ;;
  --minor) MINOR=$((MINOR + 1)); PATCH=0 ;;
  --patch) PATCH=$((PATCH + 1)) ;;
esac

NEXT_VERSION="v${MAJOR}.${MINOR}.${PATCH}"
```

### Generate release notes from commits

```bash
# Commits since last tag
git log "${LATEST_TAG}..HEAD" --pretty=format:"%s" | grep -E "^(feat|fix|chore|docs|refactor|perf|test|breaking)(\(.+\))?:" | sort
```

Group into sections:
- **Features** (`feat:`)
- **Bug fixes** (`fix:`)
- **Breaking changes** (`BREAKING CHANGE:` or `!` suffix)
- **Other** (`chore:`, `docs:`, `refactor:`, `perf:`)

### Create tag and release

```bash
# 1. Create and push the tag
git tag -a "$NEXT_VERSION" -m "Release $NEXT_VERSION"
git push origin "$NEXT_VERSION"

# 2. Create GitHub release
gh release create "$NEXT_VERSION" \
  --title "Release $NEXT_VERSION" \
  --notes "{generated release notes}" \
  --target "$DEFAULT_BRANCH"
  # add --prerelease if version contains -rc, -beta, -alpha
```

### List existing releases

```bash
gh release list
```

## Decision Guide

| Input | Action |
|-------|--------|
| `v1.3.0` (explicit version) | Tag and release at that exact version |
| `--patch` | Bump patch from latest tag |
| `--minor` | Bump minor from latest tag |
| `--major` | Bump major from latest tag |
| `--list` | `gh release list` |
| No argument | Show latest tag, ask which bump type |

## Execution

1. Read `DEFAULT_BRANCH` from `.claude/github.config.md` (default: `main`).
2. Get `LATEST_TAG` from git. If no tags exist, start from `v0.0.0`.
3. Determine the next version from `$ARGUMENTS` or by asking the user.
4. Generate release notes from commits since `LATEST_TAG`.
5. Show preview: version, tag, release notes. Ask for confirmation.
6. On confirmation: `git tag`, `git push origin {tag}`, `gh release create`.
7. Print the release URL.

$ARGUMENTS
