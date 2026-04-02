import { z } from "zod";
const ClassifyInputSchema = {
    content: z.string().describe("Text content to classify into a memory type"),
};
const RULES = {
    feedback: [
        { pattern: /\b(don't|stop|never|always|avoid)\b/gi, weight: 0.4 },
        { pattern: /\b(we got burned|we were burned|we had issues|got burned)\b/gi, weight: 0.5 },
        { pattern: /\b(yes|perfect|exactly|keep doing|that's right|correct)\b/gi, weight: 0.3 },
        { pattern: /\b(instead|rather than|should be|should not|must not)\b/gi, weight: 0.25 },
    ],
    user: [
        { pattern: /\b(i'm a|i am a|i've been|my background|my role|i work as)\b/gi, weight: 0.6 },
        { pattern: /\b(i prefer|i like|i want|my preference|i use|i rely on)\b/gi, weight: 0.4 },
        { pattern: /\b(years of experience|senior|junior|lead|principal|staff)\b/gi, weight: 0.3 },
    ],
    project: [
        { pattern: /\b(we're doing|we are doing|the reason we|our approach)\b/gi, weight: 0.5 },
        { pattern: /\b(by (monday|tuesday|wednesday|thursday|friday|next|this) (week|month)?)\b/gi, weight: 0.6 },
        { pattern: /\b(deadline|sprint|milestone|release|by (next|end of))\b/gi, weight: 0.5 },
        { pattern: /\b(because legal|compliance|regulation|requirement|stakeholder)\b/gi, weight: 0.4 },
        { pattern: /\b(we decided|the team|our team|we chose|we went with)\b/gi, weight: 0.3 },
    ],
    reference: [
        { pattern: /\b(tracked in|can be found|board at|is where|refer to)\b/gi, weight: 0.5 },
        { pattern: /\b(linear|jira|confluence|grafana|slack|notion|github|gitlab)\b/gi, weight: 0.5 },
        { pattern: /https?:\/\/\S+/g, weight: 0.6 },
        { pattern: /\b(documentation|docs|wiki|runbook|playbook|at https?)\b/gi, weight: 0.4 },
    ],
};
function countMatches(text, pattern) {
    const matches = text.match(pattern);
    return matches ? matches.length : 0;
}
function scoreType(content, rules) {
    let score = 0;
    for (const rule of rules) {
        // Reset lastIndex for global regexes
        rule.pattern.lastIndex = 0;
        const matchCount = countMatches(content, rule.pattern);
        if (matchCount > 0) {
            score += rule.weight * Math.min(matchCount, 3);
        }
    }
    return Math.min(score, 1.0);
}
function suggestName(content) {
    const words = content
        .replace(/[^a-zA-Z0-9\s]/g, " ")
        .split(/\s+/)
        .filter((w) => w.length > 2);
    const stopWords = new Set(["the", "and", "for", "with", "this", "that", "from", "are", "not", "you", "was", "its", "but", "have", "had"]);
    const significant = words.filter((w) => !stopWords.has(w.toLowerCase())).slice(0, 6);
    return significant.join("_").toLowerCase().slice(0, 60);
}
function suggestDescription(content) {
    const trimmed = content.trim();
    const firstSentence = trimmed.split(/[.!?]/)[0]?.trim() ?? trimmed;
    return firstSentence.slice(0, 100);
}
export function registerClassifyTools(server) {
    server.tool("memory_classify", "Classify text content into a memory type (user/feedback/project/reference) using heuristic pattern matching. Returns type, confidence, reasoning, and suggested name/description.", ClassifyInputSchema, async (params) => {
        try {
            const content = params.content;
            const scores = {
                feedback: scoreType(content, RULES.feedback),
                user: scoreType(content, RULES.user),
                project: scoreType(content, RULES.project),
                reference: scoreType(content, RULES.reference),
            };
            // Find the type with highest score
            let bestType = "feedback";
            let bestScore = 0;
            for (const [type, score] of Object.entries(scores)) {
                if (score > bestScore) {
                    bestScore = score;
                    bestType = type;
                }
            }
            // Default if all scores too low
            if (bestScore < 0.3) {
                bestType = "feedback";
                bestScore = 0.3;
            }
            // Build reasoning
            const scoreSummary = Object.entries(scores)
                .map(([t, s]) => `${t}: ${Math.round(s * 100)}%`)
                .join(", ");
            const reasoning = `Pattern scores — ${scoreSummary}. Best match: '${bestType}' at ${Math.round(bestScore * 100)}%.`;
            const result = {
                type: bestType,
                confidence: Math.round(bestScore * 100) / 100,
                reasoning,
                suggested_name: suggestName(content),
                suggested_description: suggestDescription(content),
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
//# sourceMappingURL=classify.js.map