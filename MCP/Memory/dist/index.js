#!/usr/bin/env node
/**
 * memory-mcp — MCP server + CLI entrypoint
 *
 * MCP mode (default): starts stdio transport and registers all memory tools.
 * CLI mode: invoked with a subcommand (search, list, health, sync-index, classify).
 */
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import yargs from "yargs";
import { hideBin } from "yargs/helpers";
import { StorageService } from "./services/storage.js";
import { detectProjectId } from "./services/projectDetector.js";
import { scoreQuery } from "./services/tfidf.js";
import { registerSearchTools } from "./tools/search.js";
import { registerStoreTools } from "./tools/store.js";
import { registerListTools } from "./tools/list.js";
import { registerDeduplicateTools } from "./tools/deduplicate.js";
import { registerHealthTools } from "./tools/health.js";
import { registerClassifyTools } from "./tools/classify.js";
import { registerSyncIndexTools } from "./tools/syncIndex.js";
import { registerExportTools } from "./tools/export.js";
const CLI_COMMANDS = ["search", "list", "health", "sync-index", "classify"];
const firstArg = process.argv[2];
const isCliMode = firstArg !== undefined && CLI_COMMANDS.includes(firstArg);
if (isCliMode) {
    await runCli();
}
else {
    await runMcpServer();
}
// ---------------------------------------------------------------------------
// MCP Server mode
// ---------------------------------------------------------------------------
async function runMcpServer() {
    const server = new McpServer({
        name: "memory-mcp",
        version: "0.1.0",
    });
    const storage = new StorageService();
    registerSearchTools(server, storage);
    registerStoreTools(server, storage);
    registerListTools(server, storage);
    registerDeduplicateTools(server, storage);
    registerHealthTools(server, storage);
    registerClassifyTools(server);
    registerSyncIndexTools(server, storage);
    registerExportTools(server, storage);
    const transport = new StdioServerTransport();
    await server.connect(transport);
}
// ---------------------------------------------------------------------------
// CLI mode
// ---------------------------------------------------------------------------
async function runCli() {
    const storage = new StorageService();
    const cli = yargs(hideBin(process.argv))
        .scriptName("memory-mcp")
        .usage("$0 <command> [options]")
        .strict();
    // --- search ---
    cli.command("search", "Search memories using TF-IDF scoring", (y) => y
        .option("query", { alias: "q", type: "string", demandOption: true, description: "Search query" })
        .option("limit", { alias: "l", type: "number", default: 5, description: "Max results" })
        .option("type", { alias: "t", type: "string", description: "Filter by type" })
        .option("project", { alias: "p", type: "string", description: "Project ID" })
        .option("min-score", { alias: "s", type: "number", default: 0.2, description: "Minimum score" }), async (argv) => {
        const projectId = detectProjectId(argv.project);
        let memories = storage.readAllMemories(projectId);
        if (argv.type) {
            memories = memories.filter((m) => m.frontmatter.type === argv.type);
        }
        const docs = memories.map((m) => ({
            id: m.relPath,
            fields: {
                name: m.frontmatter.name ?? "",
                description: m.frontmatter.description ?? "",
                tags: (m.frontmatter.tags ?? []).join(" "),
                body: m.body,
            },
        }));
        const minScore = argv["min-score"] ?? 0.2;
        const limit = argv.limit ?? 5;
        const results = docs
            .map((doc, i) => ({ memory: memories[i], score: scoreQuery(argv.query, doc, docs) }))
            .filter((s) => s.score >= minScore)
            .sort((a, b) => b.score - a.score)
            .slice(0, limit)
            .map((s) => ({
            file: s.memory.relPath,
            name: s.memory.frontmatter.name,
            type: s.memory.frontmatter.type,
            score: Math.round(s.score * 1000) / 1000,
            excerpt: s.memory.body.slice(0, 150),
        }));
        console.log(JSON.stringify({ results, total: results.length, project_id: projectId }, null, 2));
    });
    // --- list ---
    cli.command("list", "List all memories", (y) => y
        .option("type", { alias: "t", type: "string", description: "Filter by type" })
        .option("format", { alias: "f", type: "string", default: "full", description: "full or summary" })
        .option("project", { alias: "p", type: "string", description: "Project ID" }), async (argv) => {
        const projectId = detectProjectId(argv.project);
        let memories = storage.readAllMemories(projectId);
        if (argv.type) {
            memories = memories.filter((m) => m.frontmatter.type === argv.type);
        }
        const isSummary = argv.format === "summary";
        const items = memories.map((m) => {
            const base = {
                file: m.relPath,
                name: m.frontmatter.name,
                description: m.frontmatter.description,
                type: m.frontmatter.type,
                tags: m.frontmatter.tags,
                updated: m.frontmatter.updated,
                confidence: m.frontmatter.confidence,
            };
            if (isSummary)
                return base;
            return { ...base, body: m.body };
        });
        const byType = {
            user: memories.filter((m) => m.frontmatter.type === "user").length,
            feedback: memories.filter((m) => m.frontmatter.type === "feedback").length,
            project: memories.filter((m) => m.frontmatter.type === "project").length,
            reference: memories.filter((m) => m.frontmatter.type === "reference").length,
        };
        console.log(JSON.stringify({ memories: items, total: items.length, by_type: byType, project_id: projectId }, null, 2));
    });
    // --- health ---
    cli.command("health", "Check memory health for a project", (y) => y.option("project", { alias: "p", type: "string", description: "Project ID" }), async (argv) => {
        const projectId = detectProjectId(argv.project);
        // Delegate to the same logic by spawning the MCP handler inline
        // For CLI, we replicate a minimal health check inline to avoid circular deps
        const memories = storage.readAllMemories(projectId);
        const byType = {
            user: memories.filter((m) => m.frontmatter.type === "user").length,
            feedback: memories.filter((m) => m.frontmatter.type === "feedback").length,
            project: memories.filter((m) => m.frontmatter.type === "project").length,
            reference: memories.filter((m) => m.frontmatter.type === "reference").length,
        };
        const issues = [];
        for (const m of memories) {
            const missing = [];
            if (!m.frontmatter.name)
                missing.push("name");
            if (!m.frontmatter.description)
                missing.push("description");
            if (!m.frontmatter.type)
                missing.push("type");
            if (missing.length > 0) {
                issues.push({ severity: "error", file: m.relPath, issue: `Missing required fields: ${missing.join(", ")}` });
            }
        }
        const coverageGaps = [];
        if (byType.user === 0)
            coverageGaps.push("No user memories");
        if (byType.reference === 0)
            coverageGaps.push("No reference memories");
        const errorCount = issues.filter((i) => i.severity === "error").length;
        const warningCount = issues.filter((i) => i.severity === "warning").length;
        const score = Math.max(0, Math.min(1, 1.0 - errorCount * 0.2 - warningCount * 0.1));
        console.log(JSON.stringify({ total: memories.length, by_type: byType, issues, coverage_gaps: coverageGaps, score, project_id: projectId }, null, 2));
    });
    // --- sync-index ---
    cli.command("sync-index", "Sync MEMORY.md index with actual memory files", (y) => y.option("project", { alias: "p", type: "string", description: "Project ID" }), async (argv) => {
        const projectId = detectProjectId(argv.project);
        const indexContent = storage.readIndex(projectId);
        const indexed = new Map();
        for (const line of indexContent.split("\n")) {
            const match = line.match(/\(memory\/([^)]+\.md)\)/);
            if (match)
                indexed.set(`memory/${match[1]}`, line);
        }
        const memories = storage.readAllMemories(projectId);
        const actualFiles = new Set(memories.map((m) => m.relPath));
        const added = [];
        const removed = [];
        const unchanged = [];
        for (const relPath of actualFiles) {
            if (indexed.has(relPath))
                unchanged.push(relPath);
            else
                added.push(relPath);
        }
        for (const relPath of indexed.keys()) {
            if (!actualFiles.has(relPath))
                removed.push(relPath);
        }
        const lines = memories.map((m) => {
            const hook = m.frontmatter.description?.split(/[.\n]/)[0]?.trim() ?? "";
            return `- [${m.frontmatter.name}](${m.relPath}) — ${hook}`;
        });
        storage.writeIndex(projectId, lines.join("\n") + (lines.length > 0 ? "\n" : ""));
        console.log(JSON.stringify({ added, removed, unchanged, index_path: storage.getIndexPath(projectId), project_id: projectId }, null, 2));
    });
    // --- classify ---
    cli.command("classify", "Classify content into a memory type", (y) => y.option("content", { alias: "c", type: "string", demandOption: true, description: "Content to classify" }), async (argv) => {
        // Inline heuristic classification (mirrors classify.ts logic)
        const content = argv.content;
        const PATTERNS = {
            feedback: [/\b(don't|stop|never|always|avoid)\b/gi, /\b(we got burned|got burned)\b/gi, /\b(yes|perfect|exactly|keep doing)\b/gi],
            user: [/\b(i'm a|i am a|my background|my role|i work as)\b/gi, /\b(i prefer|my preference)\b/gi],
            project: [/\b(we're doing|deadline|sprint|milestone|release)\b/gi, /\b(because legal|compliance)\b/gi],
            reference: [/\b(tracked in|can be found|linear|jira|confluence|grafana)\b/gi, /https?:\/\/\S+/g],
        };
        const scores = { feedback: 0, user: 0, project: 0, reference: 0 };
        for (const [type, patterns] of Object.entries(PATTERNS)) {
            for (const pattern of patterns) {
                pattern.lastIndex = 0;
                const count = (content.match(pattern) ?? []).length;
                if (count > 0)
                    scores[type] += 0.4 * Math.min(count, 3);
            }
            scores[type] = Math.min(scores[type], 1.0);
        }
        let bestType = "feedback";
        let bestScore = 0;
        for (const [t, s] of Object.entries(scores)) {
            if (s > bestScore) {
                bestScore = s;
                bestType = t;
            }
        }
        if (bestScore < 0.3) {
            bestType = "feedback";
            bestScore = 0.3;
        }
        const words = content.replace(/[^a-zA-Z0-9\s]/g, " ").split(/\s+/).filter((w) => w.length > 2).slice(0, 6);
        const suggested_name = words.join("_").toLowerCase();
        const suggested_description = content.trim().split(/[.!?]/)[0]?.trim().slice(0, 100) ?? "";
        console.log(JSON.stringify({ type: bestType, confidence: Math.round(bestScore * 100) / 100, suggested_name, suggested_description }, null, 2));
    });
    await cli.parseAsync();
}
//# sourceMappingURL=index.js.map