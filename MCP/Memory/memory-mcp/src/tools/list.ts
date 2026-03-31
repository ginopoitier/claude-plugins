import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { StorageService, MemoryFile } from "../services/storage.js";
import { detectProjectId } from "../services/projectDetector.js";

const ListInputSchema = {
  type: z
    .enum(["user", "feedback", "project", "reference"])
    .optional()
    .describe("Filter by memory type"),
  project_id: z.string().optional().describe("Project ID (auto-detected if omitted)"),
  sort: z
    .enum(["updated", "created", "name", "confidence"])
    .default("updated")
    .describe("Sort field (default: updated)"),
  format: z
    .enum(["full", "summary"])
    .default("full")
    .describe("full includes body; summary omits it"),
};

type SortField = "updated" | "created" | "name" | "confidence";

function sortMemories(memories: MemoryFile[], sortField: SortField): MemoryFile[] {
  return [...memories].sort((a, b) => {
    switch (sortField) {
      case "updated":
        return (b.frontmatter.updated ?? "").localeCompare(a.frontmatter.updated ?? "");
      case "created":
        return (b.frontmatter.created ?? "").localeCompare(a.frontmatter.created ?? "");
      case "name":
        return (a.frontmatter.name ?? "").localeCompare(b.frontmatter.name ?? "");
      case "confidence":
        return (b.frontmatter.confidence ?? 0) - (a.frontmatter.confidence ?? 0);
      default:
        return 0;
    }
  });
}

export function registerListTools(server: McpServer, storage: StorageService): void {
  server.tool(
    "memory_list",
    "List all memories for a project, with optional type filtering and sorting. Summary format omits body content.",
    ListInputSchema,
    async (params) => {
      try {
        const projectId = detectProjectId(params.project_id);
        let memories = storage.readAllMemories(projectId);

        if (params.type) {
          memories = memories.filter((m) => m.frontmatter.type === params.type);
        }

        const sortField = (params.sort ?? "updated") as SortField;
        memories = sortMemories(memories, sortField);

        const isSummary = (params.format ?? "full") === "summary";

        const items = memories.map((m) => {
          const base = {
            file: m.relPath,
            name: m.frontmatter.name,
            description: m.frontmatter.description,
            type: m.frontmatter.type,
            tags: m.frontmatter.tags,
            created: m.frontmatter.created,
            updated: m.frontmatter.updated,
            confidence: m.frontmatter.confidence,
            source: m.frontmatter.source,
          };
          if (isSummary) return base;
          return { ...base, body: m.body };
        });

        const byType = {
          user: memories.filter((m) => m.frontmatter.type === "user").length,
          feedback: memories.filter((m) => m.frontmatter.type === "feedback").length,
          project: memories.filter((m) => m.frontmatter.type === "project").length,
          reference: memories.filter((m) => m.frontmatter.type === "reference").length,
        };

        const result = {
          memories: items,
          total: items.length,
          by_type: byType,
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
