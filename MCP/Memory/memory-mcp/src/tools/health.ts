import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { StorageService } from "../services/storage.js";
import { cosineSimilarity, TfIdfDocument } from "../services/tfidf.js";
import { detectProjectId } from "../services/projectDetector.js";

const STALE_DAYS: Record<string, number> = {
  project: 30,
  reference: 90,
};

const HealthInputSchema = {
  project_id: z.string().optional().describe("Project ID (auto-detected if omitted)"),
};

interface HealthIssue {
  severity: "error" | "warning" | "info";
  file: string;
  issue: string;
}

function daysSince(isoDate: string): number {
  const then = new Date(isoDate).getTime();
  const now = Date.now();
  return Math.floor((now - then) / (1000 * 60 * 60 * 24));
}

function indexedFiles(indexContent: string): Set<string> {
  const entries = new Set<string>();
  for (const line of indexContent.split("\n")) {
    const match = line.match(/\(memory\/([^)]+\.md)\)/);
    if (match) entries.add(`memory/${match[1]}`);
  }
  return entries;
}

export function registerHealthTools(server: McpServer, storage: StorageService): void {
  server.tool(
    "memory_health",
    "Analyse memory health for a project: counts by type, missing fields, stale entries, duplicates, index sync status, coverage gaps, and overall score.",
    HealthInputSchema,
    async (params) => {
      try {
        const projectId = detectProjectId(params.project_id);
        const memories = storage.readAllMemories(projectId);
        const issues: HealthIssue[] = [];

        // Counts by type
        const byType = {
          user: 0,
          feedback: 0,
          project: 0,
          reference: 0,
        };
        for (const m of memories) {
          const t = m.frontmatter.type;
          if (t in byType) (byType as Record<string, number>)[t]++;
        }

        // Missing required fields
        for (const m of memories) {
          const missing: string[] = [];
          if (!m.frontmatter.name) missing.push("name");
          if (!m.frontmatter.description) missing.push("description");
          if (!m.frontmatter.type) missing.push("type");
          if (missing.length > 0) {
            issues.push({
              severity: "error",
              file: m.relPath,
              issue: `Missing required fields: ${missing.join(", ")}`,
            });
          }
        }

        // Stale check
        for (const m of memories) {
          const staleDays = STALE_DAYS[m.frontmatter.type];
          if (staleDays !== undefined && m.frontmatter.updated) {
            const age = daysSince(m.frontmatter.updated);
            if (age > staleDays) {
              issues.push({
                severity: "warning",
                file: m.relPath,
                issue: `Stale: last updated ${age} days ago (limit: ${staleDays} days for type '${m.frontmatter.type}')`,
              });
            }
          }
        }

        // Duplicate detection (info level)
        const docs: TfIdfDocument[] = memories.map((m) => ({
          id: m.relPath,
          fields: {
            name: m.frontmatter.name ?? "",
            description: m.frontmatter.description ?? "",
            tags: (m.frontmatter.tags ?? []).join(" "),
            body: m.body,
          },
        }));

        for (let i = 0; i < docs.length; i++) {
          for (let j = i + 1; j < docs.length; j++) {
            const sim = cosineSimilarity(docs[i]!, docs[j]!);
            if (sim >= 0.7) {
              issues.push({
                severity: "info",
                file: docs[i]!.id,
                issue: `Possible duplicate of ${docs[j]!.id} (similarity: ${Math.round(sim * 100)}%)`,
              });
            }
          }
        }

        // Index sync check
        const indexContent = storage.readIndex(projectId);
        const indexed = indexedFiles(indexContent);
        const actualFiles = new Set(memories.map((m) => m.relPath));

        const driftDetails: string[] = [];
        for (const f of actualFiles) {
          if (!indexed.has(f)) driftDetails.push(`${f} not in index`);
        }
        for (const f of indexed) {
          if (!actualFiles.has(f)) driftDetails.push(`${f} in index but file missing`);
        }

        const indexSync = driftDetails.length === 0 ? "ok" : "drift";

        // Coverage gaps
        const coverageGaps: string[] = [];
        if (byType.user === 0) coverageGaps.push("No user memories (role/preferences not captured)");
        if (byType.reference === 0) coverageGaps.push("No reference memories (no system/tool pointers)");

        // Score calculation
        const errorCount = issues.filter((i) => i.severity === "error").length;
        const warningCount = issues.filter((i) => i.severity === "warning").length;
        const infoCount = issues.filter((i) => i.severity === "info").length;
        const rawScore = 1.0 - (errorCount * 0.2 + warningCount * 0.1 + infoCount * 0.02);
        const score = Math.max(0, Math.min(1, rawScore));

        const result: Record<string, unknown> = {
          total: memories.length,
          by_type: byType,
          issues,
          index_sync: indexSync,
          coverage_gaps: coverageGaps,
          score: Math.round(score * 100) / 100,
          project_id: projectId,
        };

        if (driftDetails.length > 0) {
          result["drift_details"] = driftDetails;
        }

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
