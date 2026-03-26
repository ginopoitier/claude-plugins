#!/usr/bin/env node
/**
 * Vue MCP Server — entry point
 * Registers all Vue/TypeScript analysis tools and starts the stdio MCP transport.
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { registerComponentTools } from "./tools/component-analyzer.js";
import { registerPiniaTools } from "./tools/pinia-analyzer.js";
import { registerTypeTools } from "./tools/type-checker.js";
import { registerProjectTools } from "./tools/project-analyzer.js";

// Resolve project root: --project arg > cwd
function resolveProjectRoot(args: string[]): string {
  const idx = args.indexOf("--project");
  if (idx >= 0 && idx + 1 < args.length) return args[idx + 1];
  return process.cwd();
}

const projectRoot = resolveProjectRoot(process.argv.slice(2));

const server = new McpServer({
  name: "vue-mcp",
  version: "0.1.0",
});

// Register all tool groups
registerComponentTools(server, projectRoot);
registerPiniaTools(server, projectRoot);
registerTypeTools(server, projectRoot);
registerProjectTools(server, projectRoot);

// Start stdio transport — stdout is reserved for MCP JSON-RPC; use stderr for logs
const transport = new StdioServerTransport();
await server.connect(transport);
