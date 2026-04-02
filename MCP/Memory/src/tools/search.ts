import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { StorageService } from "../services/storage.js";
import { scoreQuery, TfIdfDocument } from "../services/tfidf.js";
import { detectProjectId } from "../services/projectDetector.js";

const SearchInputSchema = {
  query: z.string().describe("Search query"),
  type: z
    .enum(["user", "feedback", "project", "reference"])
    .optional()
    .describe("Filter by memory type"),
  project_id: z.string().optional().describe("Project ID (auto-detected if omitted)"),
  limit: z.number().int().min(1).max(20).default(5).describe("Max results (default 5, max 20)"),
  min_score: z.number().min(0).max(1).default(0.2).describe("Minimum relevance score (default 0.2)"),
};

export function registerSearchTools(server: McpServer, storage: StorageService): void {
  server.tool(
    "memory_search",
    "Search memories using TF-IDF semantic scoring. Returns ranked results filtered by type and minimum score.",
    SearchInputSchema,
    async (params) => {
      try {
        const projectId = detectProjectId(params.project_id);
        let memories = storage.readAllMemories(projectId);

        if (params.type) {
          memories = memories.filter((m) => m.frontmatter.type === params.type);
        }

        // Build TF-IDF documents
        const docs: TfIdfDocument[] = memories.map((m) => ({
          id: m.relPath,
          fields: {
            name: m.frontmatter.name ?? "",
            description: m.frontmatter.description ?? "",
            tags: (m.frontmatter.tags ?? []).join(" "),
            body: m.body,
          },
        }));

        // Score each document
        const scored = docs.map((doc, i) => ({
          memory: memories[i]!,
          score: scoreQuery(params.query, doc, docs),
        }));

        // Filter and sort
        const limit = params.limit ?? 5;
        const minScore = params.min_score ?? 0.2;

        const results = scored
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

        const result = {
          results,
          total: results.length,
          project_id: projectId,
        };

        return {
          content: [{ type: "text" as const, text: JSON.stringify(result, null, 2) }],
        };
      } catch (e) {
        const err = e as Error;
        return {
          content: [{ type: "text" as const, text: JSON.stringify({ error: err.message }) }],
        };
      }
    }
  );
}
