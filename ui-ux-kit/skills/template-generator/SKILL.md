---
name: template-generator
description: >
  Generate full Vue page layout templates using design system components and Tailwind.
  Covers landing pages, dashboards, auth pages, settings pages, and form pages.
  Load this skill when: "page template", "layout template", "generate page", "landing page",
  "dashboard layout", "auth page", "login page", "settings page", "form page",
  "page layout", "app shell", "sidebar layout", "create page".
user-invocable: true
argument-hint: "<template-type> [--name <PageName>] [--output <path>]"
allowed-tools: [Read, Write, Edit, Bash, Glob]
---

# Template Generator

Generate Vue page layout templates built on the design system.

## Available Templates

### `landing` — Marketing landing page
Sections: hero, features, testimonials, pricing, CTA, footer

### `dashboard` — App dashboard
Layout: sidebar nav + topbar + main content area + stats grid

### `auth` — Authentication page
Variants: login, register, forgot-password, reset-password

### `settings` — Settings page
Layout: sidebar tabs + form sections

### `form` — Multi-step or single form page
Layouts: single-column, two-column, wizard

### `data-table` — Data table page
Layout: filter bar + sortable table + pagination + row actions

### `profile` — User profile page
Layout: cover + avatar + info + activity feed

## Steps

### 1. Determine template type

From $ARGUMENTS or ask the user to choose from the list above.

### 2. Discover available components

Glob `DESIGN_SYSTEM_PATH/components/**/*.vue` to know what's available.
Templates only use components that exist in the design system.
Note any missing components that would need to be created first.

### 3. Generate template

Apply the appropriate layout from the templates directory.
Substitute actual design system component imports.
Use Tailwind classes referencing design tokens for spacing and color.

### 4. Write output

Default path: `src/pages/{PageName}.vue`
Or per `--output` flag.

### Example: Dashboard Template

```vue
<script setup lang="ts">
import DsContainer from '@/design-system/components/DsLayout/DsContainer.vue'
import DsStack from '@/design-system/components/DsLayout/DsStack.vue'
import DsCard from '@/design-system/components/DsComposite/DsCard.vue'
import DsBadge from '@/design-system/components/DsPrimitive/DsBadge.vue'
</script>

<template>
  <div class="flex min-h-screen bg-[var(--color-semantic-surface)]">
    <!-- Sidebar -->
    <aside class="w-64 border-r border-[var(--color-semantic-border)] p-6">
      <nav class="space-y-1">
        <slot name="sidebar-nav" />
      </nav>
    </aside>

    <!-- Main -->
    <div class="flex flex-1 flex-col">
      <!-- Topbar -->
      <header class="h-16 border-b border-[var(--color-semantic-border)] px-6 flex items-center justify-between">
        <slot name="topbar" />
      </header>

      <!-- Content -->
      <main class="flex-1 p-6">
        <DsContainer>
          <slot />
        </DsContainer>
      </main>
    </div>
  </div>
</template>
```

### 5. Add router entry suggestion

If Vue Router detected, suggest the route entry:
```ts
{ path: '/dashboard', component: () => import('./pages/DashboardPage.vue') }
```

## Execution

$ARGUMENTS

Detect the template type, check available components, generate the template, and write it to the output path.
