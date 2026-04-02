import { execSync } from "child_process";
import path from "path";
export function sanitizeProjectId(id) {
    return id
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/^-+|-+$/g, "")
        .replace(/-{2,}/g, "-");
}
export function detectProjectId(flagValue) {
    // 1. Explicit flag
    if (flagValue && flagValue.trim()) {
        return sanitizeProjectId(flagValue.trim());
    }
    // 2. Environment variable
    const envId = process.env["MEMORY_PROJECT_ID"];
    if (envId && envId.trim()) {
        return sanitizeProjectId(envId.trim());
    }
    // 3. Git remote origin slug
    try {
        const remote = execSync("git remote get-url origin", {
            encoding: "utf-8",
            stdio: ["pipe", "pipe", "pipe"],
        }).trim();
        if (remote) {
            // Strip protocol prefix and .git suffix
            // e.g. https://github.com/user/repo.git → user/repo
            // e.g. git@github.com:user/repo.git → user/repo
            let slug = remote
                .replace(/^(https?:\/\/[^/]+\/|git@[^:]+:)/, "")
                .replace(/\.git$/, "");
            // Replace slashes with dashes
            slug = slug.replace(/\//g, "-");
            if (slug)
                return sanitizeProjectId(slug);
        }
    }
    catch {
        // git not available or not a git repo — fall through
    }
    // 4. basename of cwd
    const cwd = process.cwd();
    return sanitizeProjectId(path.basename(cwd));
}
//# sourceMappingURL=projectDetector.js.map