/**
 * Component Analyzer tools
 * Uses @vue/compiler-sfc to parse .vue Single File Components and detect issues.
 */

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { parse, compileScript } from "@vue/compiler-sfc";
import { readFileSync, existsSync } from "fs";
import { glob } from "glob";
import path from "path";

export interface ComponentIssue {
  file: string;
  line?: number;
  rule: string;
  severity: "error" | "warning" | "info";
  message: string;
}

interface SfcAnalysis {
  file: string;
  hasScriptSetup: boolean;
  usesOptionsApi: boolean;
  hasInlineStyles: boolean;
  hasDirectApiCallsInTemplate: boolean;
  missingPropsTypes: boolean;
  missingEmitsTypes: boolean;
  signalrListenersWithoutCleanup: boolean;
  issues: ComponentIssue[];
}

function analyzeComponent(filePath: string): SfcAnalysis {
  const source = readFileSync(filePath, "utf-8");
  const { descriptor, errors } = parse(source, { filename: filePath });

  const issues: ComponentIssue[] = [];
  const rel = path.basename(filePath);

  // Parse errors from compiler-sfc
  for (const err of errors) {
    issues.push({
      file: rel,
      rule: "parse-error",
      severity: "error",
      message: String(err),
    });
  }

  // ── Script analysis ──────────────────────────────────────────────────────

  const hasScriptSetup = descriptor.scriptSetup !== null;
  const hasOptionsScript =
    descriptor.script !== null && descriptor.scriptSetup === null;
  const usesOptionsApi = hasOptionsScript;

  if (usesOptionsApi) {
    issues.push({
      file: rel,
      rule: "no-options-api",
      severity: "error",
      message:
        "Options API detected. Migrate to <script setup> Composition API.",
    });
  }

  if (!hasScriptSetup && !hasOptionsScript) {
    issues.push({
      file: rel,
      rule: "missing-script-setup",
      severity: "warning",
      message: "Component has no <script setup> block.",
    });
  }

  // Props / emits type coverage (requires <script setup>)
  let missingPropsTypes = false;
  let missingEmitsTypes = false;

  if (hasScriptSetup && descriptor.scriptSetup) {
    const scriptContent = descriptor.scriptSetup.content;

    // defineProps without TypeScript generic = untyped
    if (
      /defineProps\s*\(/.test(scriptContent) &&
      !/defineProps\s*</.test(scriptContent)
    ) {
      missingPropsTypes = true;
      issues.push({
        file: rel,
        rule: "typed-props",
        severity: "warning",
        message: "defineProps() is untyped. Use defineProps<{ ... }>().",
      });
    }

    // defineEmits without TypeScript generic = untyped
    if (
      /defineEmits\s*\(/.test(scriptContent) &&
      !/defineEmits\s*</.test(scriptContent)
    ) {
      missingEmitsTypes = true;
      issues.push({
        file: rel,
        rule: "typed-emits",
        severity: "warning",
        message: "defineEmits() is untyped. Use defineEmits<{ ... }>().",
      });
    }
  }

  // ── Template analysis ────────────────────────────────────────────────────

  const templateContent = descriptor.template?.content ?? "";

  // Inline styles (style="...")
  const hasInlineStyles = /\bstyle\s*=\s*["'][^"']+["']/.test(templateContent);
  if (hasInlineStyles) {
    issues.push({
      file: rel,
      rule: "no-inline-styles",
      severity: "warning",
      message:
        "Inline styles detected in template. Use TailwindCSS utility classes.",
    });
  }

  // Direct fetch/axios calls in template expressions (rare but happens)
  const hasDirectApiCallsInTemplate =
    /\b(fetch|axios)\s*\(/.test(templateContent);
  if (hasDirectApiCallsInTemplate) {
    issues.push({
      file: rel,
      rule: "no-api-in-template",
      severity: "error",
      message:
        "Direct API call detected in template. Move fetch logic to a store action or composable.",
    });
  }

  // ── SignalR cleanup analysis ─────────────────────────────────────────────

  const scriptContent =
    descriptor.scriptSetup?.content ?? descriptor.script?.content ?? "";

  const hasConnectionOn = /connection\.on\s*\(/.test(scriptContent);
  const hasConnectionOff = /connection\.off\s*\(/.test(scriptContent);
  const signalrListenersWithoutCleanup = hasConnectionOn && !hasConnectionOff;

  if (signalrListenersWithoutCleanup) {
    issues.push({
      file: rel,
      rule: "signalr-cleanup",
      severity: "error",
      message:
        "SignalR connection.on() found without matching connection.off() in onUnmounted. Listener will leak.",
    });
  }

  // ── Style block analysis ─────────────────────────────────────────────────

  for (const style of descriptor.styles) {
    if (!style.scoped && !style.module) {
      issues.push({
        file: rel,
        rule: "scoped-styles",
        severity: "info",
        message:
          "Non-scoped <style> block. Add scoped or use TailwindCSS utilities instead.",
      });
    }
  }

  return {
    file: filePath,
    hasScriptSetup,
    usesOptionsApi,
    hasInlineStyles,
    hasDirectApiCallsInTemplate,
    missingPropsTypes,
    missingEmitsTypes,
    signalrListenersWithoutCleanup,
    issues,
  };
}

export function registerComponentTools(
  server: McpServer,
  projectRoot: string
): void {
  // ── analyze_vue_components ───────────────────────────────────────────────

  server.tool(
    "analyze_vue_components",
    "Analyze all .vue SFC files in the project for quality issues: missing <script setup>, Options API usage, untyped props/emits, inline styles, SignalR listener leaks, and direct API calls in templates.",
    {
      directory: z
        .string()
        .optional()
        .describe(
          "Subdirectory to scan (relative to project root). Defaults to src/"
        ),
      severity: z
        .enum(["error", "warning", "info", "all"])
        .optional()
        .default("all")
        .describe("Minimum severity level to report"),
    },
    async ({ directory, severity }) => {
      const scanDir = path.join(projectRoot, directory ?? "src");
      const files = await glob("**/*.vue", {
        cwd: scanDir,
        absolute: true,
        ignore: ["**/node_modules/**"],
      });

      if (files.length === 0) {
        return {
          content: [
            { type: "text", text: `No .vue files found in ${scanDir}` },
          ],
        };
      }

      const results: SfcAnalysis[] = [];
      for (const file of files) {
        try {
          results.push(analyzeComponent(file));
        } catch (e) {
          results.push({
            file,
            hasScriptSetup: false,
            usesOptionsApi: false,
            hasInlineStyles: false,
            hasDirectApiCallsInTemplate: false,
            missingPropsTypes: false,
            missingEmitsTypes: false,
            signalrListenersWithoutCleanup: false,
            issues: [
              {
                file,
                rule: "parse-failed",
                severity: "error",
                message: String(e),
              },
            ],
          });
        }
      }

      const severityRank = { error: 0, warning: 1, info: 2, all: 3 };
      const minRank = severityRank[severity ?? "all"];

      const filtered = results.flatMap((r) =>
        r.issues.filter((i) => severityRank[i.severity] <= minRank)
      );

      const summary = {
        total: files.length,
        withIssues: results.filter((r) => r.issues.length > 0).length,
        errors: filtered.filter((i) => i.severity === "error").length,
        warnings: filtered.filter((i) => i.severity === "warning").length,
        info: filtered.filter((i) => i.severity === "info").length,
      };

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify({ summary, issues: filtered }, null, 2),
          },
        ],
      };
    }
  );

  // ── find_vue_component ───────────────────────────────────────────────────

  server.tool(
    "find_vue_component",
    "Find a Vue component by name and return its file path, script setup content, props, emits, and exposed interface.",
    {
      name: z
        .string()
        .describe('Component name to search for (e.g. "UserCard", "user-card")'),
    },
    async ({ name }) => {
      const normalised = name
        .replace(/([A-Z])/g, "-$1")
        .toLowerCase()
        .replace(/^-/, "");
      const files = await glob(`**/${name}.vue`, {
        cwd: projectRoot,
        absolute: true,
        ignore: ["**/node_modules/**"],
      });
      // Also try kebab-case
      const kebabFiles = await glob(`**/${normalised}.vue`, {
        cwd: projectRoot,
        absolute: true,
        ignore: ["**/node_modules/**"],
      });

      const all = [...new Set([...files, ...kebabFiles])];
      if (all.length === 0) {
        return {
          content: [
            {
              type: "text",
              text: `Component "${name}" not found in ${projectRoot}`,
            },
          ],
        };
      }

      const results = all.map((file) => {
        const source = readFileSync(file, "utf-8");
        const { descriptor } = parse(source, { filename: file });
        return {
          path: path.relative(projectRoot, file),
          hasScriptSetup: descriptor.scriptSetup !== null,
          script: descriptor.scriptSetup?.content ?? descriptor.script?.content ?? null,
          template: descriptor.template?.content?.trim().slice(0, 500) ?? null,
        };
      });

      return {
        content: [{ type: "text", text: JSON.stringify(results, null, 2) }],
      };
    }
  );
}
