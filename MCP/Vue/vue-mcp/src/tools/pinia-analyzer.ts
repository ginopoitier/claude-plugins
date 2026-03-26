/**
 * Pinia Analyzer tools
 * Scans store files and reports structure, actions, state shape, and common issues.
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { readFileSync } from "fs";
import { glob } from "glob";
import path from "path";

interface StoreInfo {
  file: string;
  storeId: string | null;
  isCompositionApi: boolean;       // defineStore with setup function
  isOptionsApi: boolean;           // defineStore with options object
  stateKeys: string[];
  actionNames: string[];
  getterNames: string[];
  issues: string[];
}

function analyzeStore(filePath: string): StoreInfo {
  const source = readFileSync(filePath, "utf-8");
  const rel = path.basename(filePath);
  const issues: string[] = [];

  // Extract store ID
  const storeIdMatch = source.match(/defineStore\s*\(\s*['"`]([^'"`]+)['"`]/);
  const storeId = storeIdMatch?.[1] ?? null;

  if (!storeId) {
    issues.push("Missing or non-literal store ID in defineStore()");
  }

  // Detect API style: setup function vs options object
  // Composition API: defineStore('id', () => { ... })
  const isCompositionApi = /defineStore\s*\([^)]*,\s*\(\s*\)/.test(source) ||
    /defineStore\s*\([^)]*,\s*async\s*\(\s*\)/.test(source) ||
    /defineStore\s*\([^)]*,\s*\(\s*\)\s*=>/.test(source);

  // Options API: defineStore('id', { state, getters, actions })
  const isOptionsApi = !isCompositionApi && /defineStore\s*\([^)]*,\s*\{/.test(source);

  if (isOptionsApi) {
    issues.push(
      "Options API defineStore detected. Prefer Composition API style: defineStore('id', () => { ... })"
    );
  }

  // State keys: ref( / reactive( / computed( in setup body
  const stateMatches = [...source.matchAll(/const\s+(\w+)\s*=\s*ref\s*\(/g)];
  const reactiveMatches = [...source.matchAll(/const\s+(\w+)\s*=\s*reactive\s*\(/g)];
  const stateKeys = [
    ...stateMatches.map((m) => m[1]),
    ...reactiveMatches.map((m) => m[1]),
  ];

  // Getter names: computed(
  const getterMatches = [...source.matchAll(/const\s+(\w+)\s*=\s*computed\s*\(/g)];
  const getterNames = getterMatches.map((m) => m[1]);

  // Action names: async functions and regular functions
  const actionMatches = [
    ...source.matchAll(/(?:async\s+)?function\s+(\w+)\s*\(/g),
    ...source.matchAll(/const\s+(\w+)\s*=\s*async\s*\(/g),
    ...source.matchAll(/const\s+(\w+)\s*=\s*\(\s*\)\s*=>/g),
  ];
  const actionNames = actionMatches
    .map((m) => m[1])
    .filter((n) => !stateKeys.includes(n) && !getterNames.includes(n));

  // Issue: direct mutations from outside the store (can't detect statically, but check for $patch misuse)
  if (source.includes("$patch") && source.includes("store.$patch")) {
    issues.push(
      "$patch called on store reference outside of store — prefer calling an action instead"
    );
  }

  // Issue: missing return statement in setup store
  if (isCompositionApi && !source.includes("return {")) {
    issues.push(
      "Composition API store may be missing return statement — state and actions must be explicitly returned"
    );
  }

  // Issue: direct API calls inside computed
  if (/computed\s*\(\s*async/.test(source)) {
    issues.push(
      "async computed() detected — computed getters cannot be async. Use an action + ref pattern instead"
    );
  }

  return {
    file: filePath,
    storeId,
    isCompositionApi,
    isOptionsApi,
    stateKeys,
    actionNames,
    getterNames,
    issues,
  };
}

export function registerPiniaTools(server: McpServer, projectRoot: string): void {
  server.tool(
    "validate_pinia_stores",
    "Scan all Pinia store files and report structure (state, actions, getters), API style (Composition vs Options), and common issues like missing return statements, async computed, and Options API usage.",
    {
      directory: z
        .string()
        .optional()
        .describe("Subdirectory to scan (relative to project root). Defaults to src/"),
    },
    async ({ directory }) => {
      const scanDir = path.join(projectRoot, directory ?? "src");
      const files = await glob("**/*{Store,store}.ts", {
        cwd: scanDir,
        absolute: true,
        ignore: ["**/node_modules/**", "**/*.spec.ts", "**/*.test.ts"],
      });

      if (files.length === 0) {
        return {
          content: [
            { type: "text", text: `No Pinia store files found in ${scanDir}` },
          ],
        };
      }

      const stores = files.map(analyzeStore);
      const issues = stores.filter((s) => s.issues.length > 0);

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(
              {
                summary: {
                  total: stores.length,
                  compositionApi: stores.filter((s) => s.isCompositionApi).length,
                  optionsApi: stores.filter((s) => s.isOptionsApi).length,
                  withIssues: issues.length,
                },
                stores: stores.map((s) => ({
                  file: path.relative(projectRoot, s.file),
                  storeId: s.storeId,
                  style: s.isCompositionApi ? "composition" : s.isOptionsApi ? "options" : "unknown",
                  state: s.stateKeys,
                  actions: s.actionNames,
                  getters: s.getterNames,
                  issues: s.issues,
                })),
              },
              null,
              2
            ),
          },
        ],
      };
    }
  );
}
