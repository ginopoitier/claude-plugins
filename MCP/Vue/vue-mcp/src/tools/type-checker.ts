/**
 * Type Checker tools
 * Shells out to `vue-tsc` / `tsc` to surface TypeScript errors in Vue projects.
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { execFile } from "child_process";
import { promisify } from "util";
import { existsSync, readFileSync } from "fs";
import path from "path";

const execFileAsync = promisify(execFile);

interface TscError {
  file: string;
  line: number;
  column: number;
  code: string;
  message: string;
}

function parseTscOutput(output: string, projectRoot: string): TscError[] {
  const errors: TscError[] = [];
  // tsc format: path/to/file.ts(line,col): error TSxxxx: message
  const pattern = /^(.+?)\((\d+),(\d+)\):\s+error\s+(TS\d+):\s+(.+)$/gm;
  let match: RegExpExecArray | null;
  while ((match = pattern.exec(output)) !== null) {
    errors.push({
      file: path.relative(projectRoot, match[1]),
      line: parseInt(match[2], 10),
      column: parseInt(match[3], 10),
      code: match[4],
      message: match[5].trim(),
    });
  }
  return errors;
}

export function registerTypeTools(server: McpServer, projectRoot: string): void {
  server.tool(
    "get_vue_type_errors",
    "Run vue-tsc (or tsc) over the project and return all TypeScript type errors. Uses tsconfig.json at the project root.",
    {
      tsconfig: z
        .string()
        .optional()
        .describe("Path to tsconfig.json relative to project root. Defaults to tsconfig.json"),
      maxErrors: z
        .number()
        .optional()
        .default(50)
        .describe("Maximum number of errors to return"),
    },
    async ({ tsconfig, maxErrors }) => {
      const configFile = path.join(projectRoot, tsconfig ?? "tsconfig.json");
      if (!existsSync(configFile)) {
        return {
          content: [
            { type: "text", text: `tsconfig.json not found at ${configFile}` },
          ],
        };
      }

      // Prefer vue-tsc if available, fall back to tsc
      const checker = (() => {
        const vueTsc = path.join(projectRoot, "node_modules/.bin/vue-tsc");
        const tsc = path.join(projectRoot, "node_modules/.bin/tsc");
        if (existsSync(vueTsc)) return vueTsc;
        if (existsSync(tsc)) return tsc;
        return "tsc"; // rely on PATH
      })();

      let stdout = "";
      let stderr = "";
      try {
        const result = await execFileAsync(
          checker,
          ["--noEmit", "--project", configFile],
          { cwd: projectRoot, timeout: 60_000 }
        );
        stdout = result.stdout;
        stderr = result.stderr;
      } catch (e: unknown) {
        // tsc exits non-zero when there are errors — capture the output
        if (e && typeof e === "object" && "stdout" in e) {
          const execError = e as { stdout: unknown; stderr?: unknown };
          stdout = String(execError.stdout);
          stderr = String(execError.stderr ?? "");
        } else {
          return {
            content: [{ type: "text", text: `Failed to run type checker: ${String(e)}` }],
          };
        }
      }

      const errors = parseTscOutput(stdout + stderr, projectRoot).slice(0, maxErrors);

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(
              {
                checker: path.basename(checker),
                totalReturned: errors.length,
                truncated: errors.length === maxErrors,
                errors,
              },
              null,
              2
            ),
          },
        ],
      };
    }
  );

  server.tool(
    "get_vue_composables",
    "List all composable files (use*.ts) in the project and summarize their exported functions and reactive state.",
    {
      directory: z
        .string()
        .optional()
        .describe("Subdirectory to scan (relative to project root). Defaults to src/"),
    },
    async ({ directory }) => {
      const { glob } = await import("glob");
      const scanDir = path.join(projectRoot, directory ?? "src");
      const files = await glob("**/use*.ts", {
        cwd: scanDir,
        absolute: true,
        ignore: ["**/node_modules/**", "**/*.spec.ts", "**/*.test.ts"],
      });

      if (files.length === 0) {
        return {
          content: [{ type: "text", text: `No composable files found in ${scanDir}` }],
        };
      }

      const composables = files.map((file) => {
        const source = readFileSync(file, "utf-8");
        const exports = [...source.matchAll(/export\s+(?:function|const|async function)\s+(\w+)/g)]
          .map((m) => m[1]);
        const refs = [...source.matchAll(/const\s+(\w+)\s*=\s*ref\s*\(/g)].map((m) => m[1]);
        return {
          file: path.relative(projectRoot, file),
          exports,
          reactiveState: refs,
        };
      });

      return {
        content: [{ type: "text", text: JSON.stringify(composables, null, 2) }],
      };
    }
  );
}
