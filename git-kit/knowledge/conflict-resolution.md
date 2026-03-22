# Conflict Resolution

## Conflict Marker Anatomy

```
<<<<<<< HEAD                     ← your current branch (ours)
const timeout = 5000;
||||||| base                     ← common ancestor (with diff3 style)
const timeout = 3000;
=======                          ← divider
const timeout = 10000;
>>>>>>> feature/config           ← incoming branch (theirs)
```

Enable the 3-way marker (shows the ancestor/base too):
```bash
git config --global merge.conflictstyle diff3
```

## Resolution Strategies

### 1. Manual (most conflicts)
Read both sides, understand the intent, write the correct final state:
```typescript
// Resolved: use the new value from config branch (intentional change)
const timeout = 10000;
```
Then: `git add <file>` → `git merge --continue`

### 2. Accept ours
```bash
git checkout --ours <file>
git add <file>
```

### 3. Accept theirs
```bash
git checkout --theirs <file>
git add <file>
```

### 4. Strategy for all conflicts at once
```bash
git merge -X ours    # all conflicts resolved as ours
git merge -X theirs  # all conflicts resolved as theirs
```

## Types of Conflicts

### Same line modified by both
Most common. Read both versions, decide which is correct, or write a merged version.

### File deleted on one side, modified on other
```bash
git status   # shows "deleted by us" or "deleted by them"

# If you want to keep the file:
git checkout --theirs <file>   # restore it
git add <file>

# If you want it deleted:
git rm <file>
```

### Binary file conflicts
Binary files can't be merged manually — you must choose one side:
```bash
git checkout --ours image.png    # keep our version
git checkout --theirs image.png  # keep their version
git add image.png
```

## Verification After Resolving

```bash
# 1. Check no markers remain
git diff --check

# 2. Confirm all conflicts resolved
git status   # should show no "both modified" files

# 3. Run the build
dotnet build   # or npm run build, cargo build, etc.

# 4. Run tests
dotnet test    # or npm test, pytest, etc.

# 5. Complete the merge
git merge --continue
```

## Common Conflict Scenarios

| Scenario | Resolution Approach |
|----------|-------------------|
| Two developers edited same function | Read both, merge intent manually |
| Config value changed on both branches | Determine which value is correct |
| Dependency version bumped on both | Use the higher/newer version |
| File reformatted on one side | Accept the reformatted side, re-apply logic changes |
| Auto-generated file (e.g., lock file) | Regenerate: `npm install` or equivalent |
| Database migration ordering | Keep both migrations, fix numbering |
