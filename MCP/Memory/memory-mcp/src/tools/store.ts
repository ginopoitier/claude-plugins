import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { StorageService, MemoryFile, MemoryFrontmatter } from "../services/storage.js";
import { detectProjectId } from "../services/projectDetector.js";

function slugify(name: string): string {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "")
    .slice(0, 40);
}

const StoreInputSchema = {
  name: z.string().describe("Memory name"),
  description: z.string().describe("One-line description / hook"),
  type: z.enum(["user", "feedback", "project", "reference"]).describe("Memory type"),
  body: z.string().describe("Full memory body content"),
  project_id: z.string().optional().describe("Project ID (auto-detected if omitted)"),
  file_name: z.string().optional().describe("Override filename (e.g. feedback_testing.md)"),
  confidence: z.number().min(0).max(1).default(1.0).describe("Confidence score 0-1"),
  source: z
    .enum(["manual", "auto-capture", "promoted-instinct"])
    .default("manual")
    .describe("How this memory was created"),
};

const GetInputSchema = {
  file_path: z.string().describe("Relative path like memory/feedback_testing.md"),
  project_id: z.string().optional().describe("Project ID (auto-detected if omitted)"),
};

const DeleteInputSchema = {
  file_path: z.string().describe("Relative path like memory/feedback_testing.md"),
  project_id: z.string().optional().describe("Project ID (auto-detected if omitted)"),
};

export function registerStoreTools(server: McpServer, storage: StorageService): void {
  // memory_store
  server.tool(
    "memory_store",
    "Create or update a memory file. Auto-derives filename from type and name if not specified. Updates the MEMORY.md index automatically.",
    StoreInputSchema,
    async (params) => {
      try {
        const projectId = detectProjectId(params.project_id);
        const fileName = params.file_name ?? `${params.type}_${slugify(params.name)}.md`;
        const relPath = `memory/${fileName}`;

        let existing: MemoryFile | null = null;
        try {
          existing = storage.readMemoryFile(projectId, relPath);
        } catch {
          // File doesn't exist yet
        }

        const now = new Date().toISOString();
        const frontmatter: MemoryFrontmatter = {
          name: params.name,
          description: params.description,
          type: params.type,
          tags: existing?.frontmatter.tags ?? [],
          created: existing?.frontmatter.created ?? now,
          updated: now,
          confidence: params.confidence ?? 1.0,
          source: params.source ?? "manual",
        };

        const memory: MemoryFile = {
          relPath,
          frontmatter,
          body: params.body,
        };

        storage.writeMemoryFile(projectId, relPath, memory);
        storage.updateIndexEntry(projectId, memory);

        const result = {
          file: relPath,
          action: existing ? "updated" : "created",
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

  // memory_get
  server.tool(
    "memory_get",
    "Read a memory file by relative path and return all its frontmatter fields and body content.",
    GetInputSchema,
    async (params) => {
      try {
        const projectId = detectProjectId(params.project_id);
        // Normalize path — handle both absolute and relative
        let relPath = params.file_path;
        if (relPath.startsWith("/") || relPath.match(/^[A-Za-z]:\\/)) {
          // Absolute path: extract the memory/... part
          const match = relPath.match(/memory[\\/][^\\/]+\.md/);
          if (match) relPath = match[0].replace(/\\/g, "/");
        }

        const memory = storage.readMemoryFile(projectId, relPath);
        const result = {
          file: memory.relPath,
          ...memory.frontmatter,
          body: memory.body,
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

  // memory_delete
  server.tool(
    "memory_delete",
    "Delete a memory file and remove its entry from the MEMORY.md index.",
    DeleteInputSchema,
    async (params) => {
      try {
        const projectId = detectProjectId(params.project_id);
        let relPath = params.file_path;
        if (relPath.startsWith("/") || relPath.match(/^[A-Za-z]:\\/)) {
          const match = relPath.match(/memory[\\/][^\\/]+\.md/);
          if (match) relPath = match[0].replace(/\\/g, "/");
        }

        storage.deleteMemoryFile(projectId, relPath);
        storage.removeIndexEntry(projectId, relPath);

        const result = {
          deleted: relPath,
          index_updated: true,
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
