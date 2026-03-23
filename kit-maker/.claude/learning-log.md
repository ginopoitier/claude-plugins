
## Kit Evolution — 2026-03-23

### Template fixes
- Updated `templates/skill-template/SKILL.md`: added `## Execution` section and `$ARGUMENTS` line after finding 45/45 skills were missing them. Root cause: template never included them.
- Updated `rules/skill-format.md`: added items 6 (`## Execution`) and 7 (`$ARGUMENTS`) to Required Body Sections list.

### Marketplace structure corrected (critical)
- Rewrote `knowledge/marketplace-spec.md`: previous version documented `kit.manifest.json` and `install.sh` as required. Both are wrong. Actual format: per-kit `.claude-plugin/plugin.json` + ONE root `.claude-plugin/marketplace.json`.
- Updated `rules/kit-structure.md`: added explicit DON'Ts for `kit.manifest.json` and `install.sh`; clarified marketplace.json location.
- Updated `rules/versioning.md`: clarified that version must be bumped in BOTH `plugin.json` AND the root `marketplace.json` entry. Default per-session bump is PATCH (0.0.1).
- Updated `skills/scaffold-kit/SKILL.md`: replaced all `kit.manifest.json` references with `plugin.json`; removed `install.sh` from structure; added `hooks/` as required in minimal kit.

### Hooks pattern formalized
- Updated `rules/kit-structure.md`: added `hooks/check-settings.sh` + `hooks/hooks.json` as **required** (not optional) for every kit. Pattern appeared in 5/5 new kits this session.
- Added hooks spec to `knowledge/marketplace-spec.md`.
- Updated `skills/scaffold-kit/SKILL.md`: hooks generation is now Step 3 of Phase 3, alongside plugin.json.

### Version bump rule clarified
- `rules/versioning.md`: added "Default bump for any session's worth of changes is PATCH" guidance.
