import { z } from "zod";
import { cosineSimilarity } from "../services/tfidf.js";
import { detectProjectId } from "../services/projectDetector.js";
const CONFLICT_PATTERN = /\b(always|never|don't|avoid|use|prefer)\b/gi;
const DeduplicateInputSchema = {
    project_id: z.string().optional().describe("Project ID (auto-detected if omitted)"),
    threshold: z
        .number()
        .min(0)
        .max(1)
        .default(0.7)
        .describe("Similarity threshold to consider duplicates (default 0.7)"),
};
function extractConflictRoots(text) {
    const tokens = text.toLowerCase().replace(/[^a-z0-9\s]/g, " ").split(/\s+/);
    // Remove stop words and short tokens
    const stops = new Set(["the", "and", "for", "with", "this", "that", "from", "are", "not", "you"]);
    return tokens.filter((t) => t.length > 3 && !stops.has(t));
}
function hasConflict(body1, body2) {
    const hasOpposing1 = CONFLICT_PATTERN.test(body1);
    CONFLICT_PATTERN.lastIndex = 0;
    const hasOpposing2 = CONFLICT_PATTERN.test(body2);
    CONFLICT_PATTERN.lastIndex = 0;
    if (!hasOpposing1 || !hasOpposing2)
        return false;
    // Check if they share root terms (indicating they're about the same subject)
    const roots1 = new Set(extractConflictRoots(body1));
    const roots2 = extractConflictRoots(body2);
    const sharedRoots = roots2.filter((r) => roots1.has(r));
    return sharedRoots.length >= 2;
}
export function registerDeduplicateTools(server, storage) {
    server.tool("memory_deduplicate", "Find near-duplicate memories using cosine similarity. Groups similar pairs and flags potential conflicts where files make opposing claims.", DeduplicateInputSchema, async (params) => {
        try {
            const projectId = detectProjectId(params.project_id);
            const memories = storage.readAllMemories(projectId);
            const threshold = params.threshold ?? 0.7;
            const docs = memories.map((m) => ({
                id: m.relPath,
                fields: {
                    name: m.frontmatter.name ?? "",
                    description: m.frontmatter.description ?? "",
                    tags: (m.frontmatter.tags ?? []).join(" "),
                    body: m.body,
                },
            }));
            const pairs = [];
            for (let i = 0; i < docs.length; i++) {
                for (let j = i + 1; j < docs.length; j++) {
                    const sim = cosineSimilarity(docs[i], docs[j]);
                    if (sim >= threshold) {
                        pairs.push({ i, j, similarity: sim });
                    }
                }
            }
            // Group pairs using union-find
            const parent = Array.from({ length: docs.length }, (_, idx) => idx);
            function find(x) {
                if (parent[x] !== x)
                    parent[x] = find(parent[x]);
                return parent[x];
            }
            function union(x, y) {
                parent[find(x)] = find(y);
            }
            for (const pair of pairs) {
                union(pair.i, pair.j);
            }
            // Build groups
            const groupMap = new Map();
            for (let i = 0; i < docs.length; i++) {
                const root = find(i);
                if (!groupMap.has(root))
                    groupMap.set(root, []);
                groupMap.get(root).push(i);
            }
            // Only keep groups with more than 1 member
            const groups = Array.from(groupMap.values()).filter((g) => g.length > 1);
            const resultGroups = groups.map((group) => {
                // Compute average pairwise similarity for this group
                let totalSim = 0;
                let pairCount = 0;
                for (let gi = 0; gi < group.length; gi++) {
                    for (let gj = gi + 1; gj < group.length; gj++) {
                        totalSim += cosineSimilarity(docs[group[gi]], docs[group[gj]]);
                        pairCount++;
                    }
                }
                const avgSimilarity = pairCount > 0 ? totalSim / pairCount : 0;
                const files = group.map((idx) => docs[idx].id);
                // Check for conflicts across group members
                let conflict = false;
                for (let gi = 0; gi < group.length && !conflict; gi++) {
                    for (let gj = gi + 1; gj < group.length && !conflict; gj++) {
                        const body1 = memories[group[gi]]?.body ?? "";
                        const body2 = memories[group[gj]]?.body ?? "";
                        if (hasConflict(body1, body2))
                            conflict = true;
                    }
                }
                const recommendation = conflict
                    ? "Review for conflicting guidance — merge carefully or keep distinct with clarifying names"
                    : "Consider merging into a single memory or removing the less complete duplicate";
                return {
                    similarity: Math.round(avgSimilarity * 1000) / 1000,
                    files,
                    recommendation,
                    conflict,
                };
            });
            const result = {
                groups: resultGroups,
                clean: resultGroups.length === 0,
                project_id: projectId,
                threshold,
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
//# sourceMappingURL=deduplicate.js.map