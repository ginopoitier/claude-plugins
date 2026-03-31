import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { StorageService } from "../services/storage.js";
import { detectProjectId } from "../services/projectDetector.js";

const SyncIndexInputSchema = {
  project_id: z.string().optional().describe("Project ID (auto-detected if omitted)"),
};

function parseIndexEntries(indexContent: string): Map<string, string> {
  // Returns map of relPath → full line
  const entries = new Map<string, string>();
  for (const line of indexContent.split("\n")) {
    const match = line.match(/\(memory\/([^)]+\.md)\)/);
    if (match) {
      entries.set(`memory/${match[1]}`, line);
    }
  }
  return entries;
}

export function registerSyncIndexTools(server: McpServer, storage: StorageService): void {
  server.tool(
    "memory_sync_index",
    "Rebuild MEMORY.md index from actual memory files on disk. Reports added, removed, and unchanged entries.",
    SyncIndexInputSchema,
    async (params) => {
      try {
        const projectId = detectProjectId(params.project_id);
        const indexContent = storage.readIndex(projectId);
        const indexed = parseIndexEntries(indexContent);

        const memories = storage.readAllMemories(projectId);
        const actualFiles = new Set(memories.map((m) => m.relPath));

        // Determine added (files not in index), removed (index entries without files), unchanged
        const added: string[] = [];
        const removed: string[] = [];
        const unchanged: string[] = [];

        for (const relPath of actualFiles) {
          if (indexed.has(relPath)) {
            unchanged.push(relPath);
          } else {
            added.push(relPath);
          }
        }

        for (const relPath of indexed.keys()) {
          if (!actualFiles.has(relPath)) {
            removed.push(relPath);
          }
        }

        // Rebuild full MEMORY.md from actual files
        const lines: string[] = [];
        for (const m of memories) {
          const hook = m.frontmatter.description?.split(/[.\n]/)[0]?.trim() ?? "";
          lines.push(`- [${m.frontmatter.name}](${m.relPath}) — ${hook}`);
        }

        const newIndex = lines.join("\n") + (lines.length > 0 ? "\n" : "");
        storage.writeIndex(projectId, newIndex);

        const indexPath = storage.getIndexPath(projectId);

        const result = {
          added,
          removed,
          unchanged,
          index_path: indexPath,
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
