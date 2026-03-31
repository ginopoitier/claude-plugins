import fs from "fs";
import path from "path";
import os from "os";
import matter from "gray-matter";

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

export class StorageService {
  private basePath: string;

  constructor(basePath?: string) {
    this.basePath = basePath ?? path.join(os.homedir(), ".claude", "projects");
  }

  getProjectDir(projectId: string): string {
    return path.join(this.basePath, projectId);
  }

  getMemoryDir(projectId: string): string {
    return path.join(this.basePath, projectId, "memory");
  }

  getIndexPath(projectId: string): string {
    return path.join(this.basePath, projectId, "MEMORY.md");
  }

  ensureMemoryDir(projectId: string): void {
    const dir = this.getMemoryDir(projectId);
    fs.mkdirSync(dir, { recursive: true });
  }

  readMemoryFile(projectId: string, relPath: string): MemoryFile {
    const fullPath = path.join(this.getProjectDir(projectId), relPath);
    const raw = fs.readFileSync(fullPath, "utf-8");
    const parsed = matter(raw);
    const fm = parsed.data as MemoryFrontmatter;
    return {
      relPath,
      frontmatter: fm,
      body: parsed.content.trim(),
    };
  }

  writeMemoryFile(projectId: string, relPath: string, memory: MemoryFile): void {
    this.ensureMemoryDir(projectId);
    const fullPath = path.join(this.getProjectDir(projectId), relPath);
    const dir = path.dirname(fullPath);
    fs.mkdirSync(dir, { recursive: true });
    const content = matter.stringify(memory.body, memory.frontmatter as unknown as Record<string, unknown>);
    fs.writeFileSync(fullPath, content, "utf-8");
  }

  deleteMemoryFile(projectId: string, relPath: string): void {
    const fullPath = path.join(this.getProjectDir(projectId), relPath);
    fs.unlinkSync(fullPath);
  }

  listMemoryFiles(projectId: string): string[] {
    const memDir = this.getMemoryDir(projectId);
    if (!fs.existsSync(memDir)) return [];
    const files = fs.readdirSync(memDir);
    return files
      .filter((f) => f.endsWith(".md"))
      .map((f) => `memory/${f}`);
  }

  readAllMemories(projectId: string): MemoryFile[] {
    const relPaths = this.listMemoryFiles(projectId);
    const results: MemoryFile[] = [];
    for (const relPath of relPaths) {
      try {
        const mf = this.readMemoryFile(projectId, relPath);
        results.push(mf);
      } catch {
        process.stderr.write(`[memory-mcp] Skipping malformed file: ${relPath}\n`);
      }
    }
    return results;
  }

  readIndex(projectId: string): string {
    const indexPath = this.getIndexPath(projectId);
    if (!fs.existsSync(indexPath)) return "";
    return fs.readFileSync(indexPath, "utf-8");
  }

  writeIndex(projectId: string, content: string): void {
    const projectDir = this.getProjectDir(projectId);
    fs.mkdirSync(projectDir, { recursive: true });
    const indexPath = this.getIndexPath(projectId);
    fs.writeFileSync(indexPath, content, "utf-8");
  }

  updateIndexEntry(projectId: string, memory: MemoryFile): void {
    const current = this.readIndex(projectId);
    const lines = current ? current.split("\n") : [];

    // Remove existing entry for this file (match by relative path in link)
    const fileName = path.basename(memory.relPath);
    const filtered = lines.filter((line) => {
      // Match lines like: - [Name](memory/filename.md) — ...
      return !line.includes(`(memory/${fileName})`);
    });

    const hook = memory.frontmatter.description.split(/[.\n]/)[0]?.trim() ?? "";
    const newEntry = `- [${memory.frontmatter.name}](${memory.relPath}) — ${hook}`;

    // Add to end, ensure no trailing blank lines before appending
    const trimmed = filtered.join("\n").trimEnd();
    const updated = trimmed ? `${trimmed}\n${newEntry}\n` : `${newEntry}\n`;
    this.writeIndex(projectId, updated);
  }

  removeIndexEntry(projectId: string, relPath: string): void {
    const current = this.readIndex(projectId);
    if (!current) return;
    const fileName = path.basename(relPath);
    const lines = current.split("\n");
    const filtered = lines.filter((line) => !line.includes(`(memory/${fileName})`));
    this.writeIndex(projectId, filtered.join("\n"));
  }
}
