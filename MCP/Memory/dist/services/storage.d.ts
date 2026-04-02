export interface MemoryFrontmatter {
    name: string;
    description: string;
    type: "user" | "feedback" | "project" | "reference";
    tags: string[];
    created: string;
    updated: string;
    confidence: number;
    source: "manual" | "auto-capture" | "promoted-instinct";
}
export interface MemoryFile {
    relPath: string;
    frontmatter: MemoryFrontmatter;
    body: string;
}
export declare class StorageService {
    private basePath;
    constructor(basePath?: string);
    getProjectDir(projectId: string): string;
    getMemoryDir(projectId: string): string;
    getIndexPath(projectId: string): string;
    ensureMemoryDir(projectId: string): void;
    readMemoryFile(projectId: string, relPath: string): MemoryFile;
    writeMemoryFile(projectId: string, relPath: string, memory: MemoryFile): void;
    deleteMemoryFile(projectId: string, relPath: string): void;
    listMemoryFiles(projectId: string): string[];
    readAllMemories(projectId: string): MemoryFile[];
    readIndex(projectId: string): string;
    writeIndex(projectId: string, content: string): void;
    updateIndexEntry(projectId: string, memory: MemoryFile): void;
    removeIndexEntry(projectId: string, relPath: string): void;
}
//# sourceMappingURL=storage.d.ts.map