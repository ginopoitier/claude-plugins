# Dev Kit MCP Server

Custom MCP server for the dev kit — built from scratch so we own it and can extend it over time.

## Why custom over CWM.RoslynNavigator?

The dotnet-claude-kit uses `CWM.RoslynNavigator` (a 3rd-party dotnet global tool). We build our own because:
- We control the feature set — add tools as we need them
- We can extend it for Neo4j graph queries alongside Roslyn
- We can add Vue/TS tooling (TypeScript compiler API, project structure analysis)
- We can tune token efficiency for our specific patterns
- No dependency on an external maintainer

## Planned: `dev-kit-mcp`

A .NET MCP server built with the Model Context Protocol .NET SDK.

### Tool Groups

#### .NET / Roslyn Tools
| Tool | Input | Output | Token saving |
|------|-------|--------|-------------|
| `dotnet_find_symbol` | symbol name, solution path | file + line of definition | Replaces reading 3–10 files |
| `dotnet_find_references` | symbol name | all usages with file:line | Replaces grepping entire solution |
| `dotnet_get_public_api` | namespace or type name | public members + signatures | Replaces reading whole class |
| `dotnet_get_diagnostics` | project path | compiler errors + warnings | Replaces parsing build output |
| `dotnet_detect_antipatterns` | project path | list of violations | Replaces manual grep pass |
| `dotnet_get_project_graph` | solution path | project dependency tree | Replaces reading all .csproj |
| `dotnet_check_layer_violations` | solution path | Clean Arch violations | Custom — not in RoslynNavigator |

#### Neo4j Tools
| Tool | Input | Output |
|------|-------|--------|
| `neo4j_run_query` | Cypher query, params | results as JSON |
| `neo4j_get_schema` | — | node labels, rel types, properties |
| `neo4j_find_path` | from node id, to node id | shortest path |

#### TypeScript / Vue Tools (future)
| Tool | Input | Output |
|------|-------|--------|
| `ts_find_type` | type name, tsconfig path | type definition |
| `ts_get_exports` | file path | all exported symbols |
| `ts_check_errors` | tsconfig path | type errors |

### Tech Stack
- **.NET 9+** — MCP .NET SDK (`ModelContextProtocol`)
- **Microsoft.CodeAnalysis (Roslyn)** — for .NET semantic analysis
- **Neo4j.Driver** — for graph queries
- Runs as a stdio MCP server

## MCP Config (.mcp.json)

```json
{
  "mcpServers": {
    "dev-kit-mcp": {
      "command": "dotnet",
      "args": ["run", "--project", "G:/Claude/Kits/dev-kit/mcp/DevKit.Mcp"]
    }
  }
}
```

Or when published as a global tool:
```json
{
  "mcpServers": {
    "dev-kit-mcp": {
      "command": "dev-kit-mcp",
      "args": ["--solution", "${workspaceFolder}"]
    }
  }
}
```

## Build Plan

1. Create `DevKit.Mcp` .NET project with MCP SDK
2. Implement Roslyn tools first (biggest token win)
3. Add Neo4j tools
4. Add TypeScript tools via `typescript` npm package via Node interop or separate TS MCP

See `/dotnet-init` for scaffolding the project when ready to build.
