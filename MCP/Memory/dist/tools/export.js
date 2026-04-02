import { z } from "zod";
import { detectProjectId } from "../services/projectDetector.js";
const ExportInputSchema = {
    project_id: z.string().optional().describe("Project ID (auto-detected if omitted)"),
    format: z
        .enum(["markdown", "json"])
        .default("markdown")
        .describe("Export format (default: markdown)"),
    type: z
        .enum(["user", "feedback", "project", "reference"])
        .optional()
        .describe("Filter by memory type"),
};
export function registerExportTools(server, storage) {
    server.tool("memory_export", "Export all memories for a project as a single Markdown document or JSON array, with optional type filtering.", ExportInputSchema, async (params) => {
        try {
            const projectId = detectProjectId(params.project_id);
            let memories = storage.readAllMemories(projectId);
            if (params.type) {
                memories = memories.filter((m) => m.frontmatter.type === params.type);
            }
            const format = params.format ?? "markdown";
            let content;
            if (format === "markdown") {
                const sections = memories.map((m) => {
                    return `# ${m.frontmatter.name}\n\n> ${m.frontmatter.description}\n\n${m.body}\n\n---`;
                });
                content = sections.join("\n\n");
            }
            else {
                const jsonData = memories.map((m) => ({
                    file: m.relPath,
                    ...m.frontmatter,
                    body: m.body,
                }));
                content = JSON.stringify(jsonData, null, 2);
            }
            const result = {
                content,
                count: memories.length,
                format,
                project_id: projectId,
            };
            return {
                content: [{ type: "text", text: JSON.stringify(result, null, 2) }],
            };
        }
        catch (e) {
            const err = e;
            return {
                content: [{ type: "text", text: JSON.stringify({ error: err.message }) }],
            };
        }
    });
}
//# sourceMappingURL=export.js.map