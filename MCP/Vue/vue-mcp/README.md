# Vue MCP Server

MCP server for **vue-kit** — Vue 3 SFC analysis, Pinia store inspection, TypeScript type checking, and Vite project structure tools for Claude Code.

## Tools

| Tool | Description |
|------|-------------|
| `analyze_vue_components` | Scan all `.vue` files for: missing `<script setup>`, Options API usage, untyped props/emits, inline styles, SignalR listener leaks |
| `find_vue_component` | Locate a component by name and return its script, props, and template excerpt |
| `validate_pinia_stores` | Scan stores for Composition vs Options API, missing return statements, async computed |
| `get_vue_type_errors` | Run `vue-tsc --noEmit` and return TypeScript type errors |
| `get_vue_composables` | List composable files (`use*.ts`) and their exported functions |
| `get_vue_project_structure` | Overview: Vite config, router routes, proxy paths, file counts |
| `find_missing_api_types` | Find `api.ts` files with `Promise<any>` or missing typed interfaces |

## Tech Stack

- **Runtime:** Node.js 20+
- **MCP SDK:** `@modelcontextprotocol/sdk`
- **Vue parsing:** `@vue/compiler-sfc`
- **Type checking:** delegates to `vue-tsc` / `tsc` in the target project
- **File scanning:** `glob`

## Build

```bash
npm install
npm run build
```

## Run

```bash
# Against a specific project
node dist/index.js --project /path/to/vue-project

# Auto-detect from cwd
node dist/index.js
```

## MCP Config

In `.mcp.json` of any Claude Code project:

```json
{
  "mcpServers": {
    "vue-mcp": {
      "command": "node",
      "args": ["G:/Claude/Kits/MCP/Vue/vue-mcp/dist/index.js", "--project", "${workspaceFolder}"]
    }
  }
}
```

Or when published as an npm global package (`npm install -g @ginopoitier/vue-mcp`):

```json
{
  "mcpServers": {
    "vue-mcp": {
      "command": "vue-mcp",
      "args": ["--project", "${workspaceFolder}"]
    }
  }
}
```
