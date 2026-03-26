/**
 * Project Analyzer tools
 * Reads Vite config, router, and project structure to give Claude an overview.
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { readFileSync, existsSync } from "fs";
import { glob } from "glob";
import path from "path";

interface RouteEntry {
  path: string;
  component: string;
  name?: string;
}

function parseRoutesFromSource(source: string): RouteEntry[] {
  const routes: RouteEntry[] = [];
  // Match { path: '...', component: ..., name: '...' } blocks (simplified)
  const blockPattern = /\{[^{}]*path\s*:\s*['"`]([^'"`]+)['"`][^{}]*\}/g;
  let match: RegExpExecArray | null;
  while ((match = blockPattern.exec(source)) !== null) {
    const block = match[0];
    const pathMatch = block.match(/path\s*:\s*['"`]([^'"`]+)['"`]/);
    const compMatch = block.match(/component\s*:\s*([^\s,}]+)/);
    const nameMatch = block.match(/name\s*:\s*['"`]([^'"`]+)['"`]/);
    if (pathMatch) {
      routes.push({
        path: pathMatch[1],
        component: compMatch?.[1] ?? "unknown",
        name: nameMatch?.[1],
      });
    }
  }
  return routes;
}

export function registerProjectTools(server: McpServer, projectRoot: string): void {
  server.tool(
    "get_vue_project_structure",
    "Return an overview of the Vue project: Vite config (port, proxy targets, aliases), router routes, Pinia store count, component count, and composable count. Does not read all files — fast structural scan only.",
    {},
    async () => {
      const result: Record<string, unknown> = { projectRoot };

      // ── package.json ─────────────────────────────────────────────────────
      const pkgPath = path.join(projectRoot, "package.json");
      if (existsSync(pkgPath)) {
        const pkg = JSON.parse(readFileSync(pkgPath, "utf-8")) as {
          name?: string;
          version?: string;
          dependencies?: Record<string, string>;
          devDependencies?: Record<string, string>;
        };
        result.package = {
          name: pkg.name,
          version: pkg.version,
          vue: pkg.dependencies?.["vue"],
          vite: pkg.devDependencies?.["vite"],
          typescript: pkg.devDependencies?.["typescript"],
          pinia: pkg.dependencies?.["pinia"],
          vueRouter: pkg.dependencies?.["vue-router"],
          signalr: pkg.dependencies?.["@microsoft/signalr"],
        };
      }

      // ── Vite config proxy and aliases ────────────────────────────────────
      const viteConfigCandidates = ["vite.config.ts", "vite.config.js"];
      for (const candidate of viteConfigCandidates) {
        const viteConfigPath = path.join(projectRoot, candidate);
        if (existsSync(viteConfigPath)) {
          const viteSource = readFileSync(viteConfigPath, "utf-8");
          // Extract proxy targets (simplified regex scan)
          const proxyMatches = [...viteSource.matchAll(/['"`](\/[^'"`]+)['"`]\s*:/g)].map(
            (m) => m[1]
          );
          // Extract path aliases (@ -> src patterns)
          const aliasMatches = [...viteSource.matchAll(/@([^'"`\s]+)\s*:\s*['"`]([^'"`]+)['"`]/g)].map(
            (m) => ({ alias: `@${m[1]}`, path: m[2] })
          );
          result.vite = {
            config: candidate,
            proxyPaths: [...new Set(proxyMatches)],
            aliases: aliasMatches,
          };
          break;
        }
      }

      // ── Router ───────────────────────────────────────────────────────────
      const routerCandidates = await glob("src/**/router{/index,}.{ts,js}", {
        cwd: projectRoot,
        absolute: true,
      });
      if (routerCandidates.length > 0) {
        const routerSource = readFileSync(routerCandidates[0], "utf-8");
        result.router = {
          file: path.relative(projectRoot, routerCandidates[0]),
          routes: parseRoutesFromSource(routerSource),
        };
      }

      // ── File counts ──────────────────────────────────────────────────────
      const [components, stores, composables, pages] = await Promise.all([
        glob("src/**/*.vue", { cwd: projectRoot, ignore: ["**/node_modules/**"] }),
        glob("src/**/*{Store,store}.ts", { cwd: projectRoot, ignore: ["**/node_modules/**"] }),
        glob("src/**/use*.ts", { cwd: projectRoot, ignore: ["**/node_modules/**"] }),
        glob("src/**/pages/**/*.vue", { cwd: projectRoot, ignore: ["**/node_modules/**"] }),
      ]);

      result.counts = {
        components: components.length,
        pages: pages.length,
        stores: stores.length,
        composables: composables.length,
      };

      return {
        content: [{ type: "text", text: JSON.stringify(result, null, 2) }],
      };
    }
  );

  server.tool(
    "find_missing_api_types",
    "Find TypeScript files in the API client layer (features/**/api.ts) that are missing typed request/response interfaces.",
    {
      directory: z
        .string()
        .optional()
        .describe("Subdirectory to scan (relative to project root). Defaults to src/"),
    },
    async ({ directory }) => {
      const scanDir = path.join(projectRoot, directory ?? "src");
      const apiFiles = await glob("**/api.ts", {
        cwd: scanDir,
        absolute: true,
        ignore: ["**/node_modules/**"],
      });

      if (apiFiles.length === 0) {
        return {
          content: [{ type: "text", text: "No api.ts files found." }],
        };
      }

      const results = apiFiles.map((file) => {
        const source = readFileSync(file, "utf-8");
        // Detect functions returning any / unknown (untyped responses)
        const untypedReturns = [...source.matchAll(/:\s*Promise\s*<\s*any\s*>/g)].length;
        const hasInterfaces = /^(export\s+)?interface\s+\w+/m.test(source);
        const issues: string[] = [];
        if (untypedReturns > 0) issues.push(`${untypedReturns} Promise<any> return type(s)`);
        if (!hasInterfaces) issues.push("No typed interfaces defined in this file");
        return {
          file: path.relative(projectRoot, file),
          issues,
        };
      });

      const withIssues = results.filter((r) => r.issues.length > 0);
      return {
        content: [
          {
            type: "text",
            text: JSON.stringify({ total: apiFiles.length, withIssues: withIssues.length, files: results }, null, 2),
          },
        ],
      };
    }
  );
}
