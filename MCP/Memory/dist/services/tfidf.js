function tokenize(text) {
    return text
        .toLowerCase()
        .replace(/[^a-z0-9\s]/g, " ")
        .split(/\s+/)
        .filter((t) => t.length > 2);
}
function termFrequency(tokens) {
    const freq = new Map();
    for (const token of tokens) {
        freq.set(token, (freq.get(token) ?? 0) + 1);
    }
    return freq;
}
function normalizedTF(tokens) {
    const freq = termFrequency(tokens);
    const maxFreq = Math.max(...freq.values(), 1);
    const normalized = new Map();
    for (const [term, count] of freq) {
        normalized.set(term, count / maxFreq);
    }
    return normalized;
}
function documentFrequency(term, allDocs) {
    let df = 0;
    for (const doc of allDocs) {
        const combined = `${doc.fields.name} ${doc.fields.description} ${doc.fields.tags} ${doc.fields.body}`;
        const tokens = tokenize(combined);
        if (tokens.includes(term))
            df++;
    }
    return df;
}
function idf(term, allDocs) {
    const N = allDocs.length;
    const df = documentFrequency(term, allDocs);
    return Math.log((N + 1) / (df + 1)) + 1;
}
export function scoreQuery(query, doc, allDocs) {
    const queryTokens = tokenize(query);
    if (queryTokens.length === 0)
        return 0;
    // Build weighted token list from doc fields
    const nameTokens = tokenize(doc.fields.name);
    const descTokens = tokenize(doc.fields.description);
    const tagTokens = tokenize(doc.fields.tags);
    const bodyTokens = tokenize(doc.fields.body);
    const weightedTokens = [
        ...Array(3).fill(null).flatMap(() => nameTokens),
        ...Array(2).fill(null).flatMap(() => descTokens),
        ...Array(2).fill(null).flatMap(() => tagTokens),
        ...bodyTokens,
    ];
    const docTF = normalizedTF(weightedTokens);
    let score = 0;
    for (const qToken of queryTokens) {
        const tf = docTF.get(qToken) ?? 0;
        if (tf > 0) {
            const idfVal = idf(qToken, allDocs);
            score += tf * idfVal;
        }
    }
    score = score / queryTokens.length;
    return Math.min(score, 1.0);
}
function docToVector(doc, vocabulary) {
    const nameTokens = tokenize(doc.fields.name);
    const descTokens = tokenize(doc.fields.description);
    const tagTokens = tokenize(doc.fields.tags);
    const bodyTokens = tokenize(doc.fields.body);
    const weightedTokens = [
        ...Array(3).fill(null).flatMap(() => nameTokens),
        ...Array(2).fill(null).flatMap(() => descTokens),
        ...Array(2).fill(null).flatMap(() => tagTokens),
        ...bodyTokens,
    ];
    const freq = termFrequency(weightedTokens);
    return vocabulary.map((term) => freq.get(term) ?? 0);
}
function dotProduct(a, b) {
    return a.reduce((sum, val, i) => sum + val * (b[i] ?? 0), 0);
}
function magnitude(v) {
    return Math.sqrt(v.reduce((sum, val) => sum + val * val, 0));
}
export function cosineSimilarity(doc1, doc2) {
    const tokens1 = tokenize(`${doc1.fields.name} ${doc1.fields.description} ${doc1.fields.tags} ${doc1.fields.body}`);
    const tokens2 = tokenize(`${doc2.fields.name} ${doc2.fields.description} ${doc2.fields.tags} ${doc2.fields.body}`);
    const vocab = Array.from(new Set([...tokens1, ...tokens2]));
    if (vocab.length === 0)
        return 0;
    const v1 = docToVector(doc1, vocab);
    const v2 = docToVector(doc2, vocab);
    const mag1 = magnitude(v1);
    const mag2 = magnitude(v2);
    if (mag1 === 0 || mag2 === 0)
        return 0;
    return Math.min(dotProduct(v1, v2) / (mag1 * mag2), 1.0);
}
//# sourceMappingURL=tfidf.js.map