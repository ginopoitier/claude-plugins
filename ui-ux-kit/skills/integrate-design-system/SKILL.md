---
name: integrate-design-system
description: >
  Wire an existing design system into a Vue project: add CSS imports, set up the barrel
  export, configure Tailwind, and verify everything resolves correctly.
  Load this skill when: "integrate design system", "add design system", "install design system",
  "wire up design system", "use design system in project", "import design system",
  "connect design system", "setup design system", "configure tailwind tokens".
user-invocable: true
argument-hint: "[--design-system-path <path>] [--project-path <path>] [--package <npm-package>]"
allowed-tools: [Read, Write, Edit, Bash, Glob, Grep]
---

# Integrate Design System

Wire an existing design system into a Vue + Tailwind project.

## Steps

### 1. Locate design system and project

From config or $ARGUMENTS:
- Design system: local path (`DESIGN_SYSTEM_PATH`) or npm package (`--package`)
- Project: current working directory or `--project-path`

### 2. Detect project structure

Check for:
- `vite.config.ts` — Vite project
- `src/assets/main.css` or `src/style.css` — CSS entry point
- `src/main.ts` — Vue app entry
- `tsconfig.json` — path aliases

### 3. Add CSS imports

Find the project's CSS entry file. Add imports at the top:

```css
/* main.css */
@import "tailwindcss";
@import "../design-system/tokens/base.css";
@import "../design-system/tokens/semantic.css";
```

Or if npm package:
```css
@import "tailwindcss";
@import "@company/design-system/tokens/base.css";
@import "@company/design-system/tokens/semantic.css";
```

### 4. Configure TypeScript path alias

If using local design system, add to `tsconfig.json`:
```json
{
  "compilerOptions": {
    "paths": {
      "@ds/*": ["./src/design-system/*"]
    }
  }
}
```

And to `vite.config.ts`:
```ts
resolve: {
  alias: {
    '@ds': path.resolve(__dirname, './src/design-system')
  }
}
```

### 5. Add global component registration (optional)

If user wants auto-imported components, add to `main.ts`:
```ts
import * as DesignSystem from '@ds/index'

const app = createApp(App)
Object.entries(DesignSystem).forEach(([name, component]) => {
  app.component(name, component)
})
```

Or suggest `unplugin-vue-components` for auto-import without global registration.

### 6. Verify integration

- Check that `@ds/components/DsButton/DsButton.vue` resolves
- Check that CSS variables from tokens are available (`var(--color-semantic-primary)`)
- Run a quick build check if possible

### 7. Summary

```
Integration complete:
  ✓ Token CSS imported in src/assets/main.css
  ✓ Path alias @ds → src/design-system added to vite.config.ts + tsconfig.json
  ✓ 12 design system components available

Usage:
  import DsButton from '@ds/components/DsPrimitive/DsButton.vue'
  // or with global registration: <DsButton variant="primary">Click</DsButton>
```

## Execution

$ARGUMENTS

Detect the project structure, add CSS imports, configure aliases, verify resolution, and summarize what's available.
