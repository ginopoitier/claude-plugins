export interface TfIdfDocument {
    id: string;
    fields: {
        name: string;
        description: string;
        tags: string;
        body: string;
    };
}
export declare function scoreQuery(query: string, doc: TfIdfDocument, allDocs: TfIdfDocument[]): number;
export declare function cosineSimilarity(doc1: TfIdfDocument, doc2: TfIdfDocument): number;
//# sourceMappingURL=tfidf.d.ts.map