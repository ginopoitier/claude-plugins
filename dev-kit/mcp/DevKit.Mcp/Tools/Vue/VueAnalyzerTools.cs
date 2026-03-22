using System.ComponentModel;
using System.Text.RegularExpressions;
using DevKit.Mcp.Models;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Vue;

[McpServerToolType]
public sealed class VueAnalyzerTools
{
    [McpServerTool, Description(
        "Analyzes Vue 3 component files for common issues: " +
        "missing script setup, Options API usage, inline styles, missing cleanup of SignalR/event listeners, " +
        "direct API calls in templates, and missing TypeScript types on props/emits.")]
    public async Task<IReadOnlyList<VueAnalysisResult>> AnalyzeVueComponents(
        [Description("Root directory to scan. Defaults to current directory.")] string? rootPath = null,
        CancellationToken ct = default)
    {
        var root = rootPath ?? Directory.GetCurrentDirectory();
        var results = new List<VueAnalysisResult>();

        var vueFiles = Directory.GetFiles(root, "*.vue", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}"))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}dist{Path.DirectorySeparatorChar}"))
            .ToList();

        foreach (var file in vueFiles)
        {
            ct.ThrowIfCancellationRequested();
            var content = await File.ReadAllTextAsync(file, ct);
            var issues = new List<string>();

            // Check for script setup
            if (!content.Contains("<script setup") && content.Contains("<script"))
                issues.Add("Uses <script> without setup — should use <script setup lang=\"ts\">");

            // Options API
            if (Regex.IsMatch(content, @"defineComponent\s*\(|export default \{"))
                issues.Add("Uses Options API / defineComponent — migrate to <script setup>");

            // Inline styles
            if (Regex.IsMatch(content, @":style=""(?!undefined|null|{}\s*"")") || content.Contains("style=\""))
                if (!Regex.IsMatch(content, @":style=""(?:dark|theme|style|display|width|height|transform|opacity|color|background)"))
                    issues.Add("Inline styles detected — use TailwindCSS classes instead");

            // SignalR listener without cleanup
            if (content.Contains("conn.on(") && !content.Contains("conn.off(") && !content.Contains("onUnmounted"))
                issues.Add("SignalR .on() listener without corresponding .off() in onUnmounted — memory leak risk");

            // Direct fetch/axios in template script
            if (Regex.IsMatch(content, @"fetch\s*\(|axios\.\w+\s*\(") && !content.Contains("from '@/"))
                issues.Add("Direct fetch/axios call — move to features/{name}/api.ts");

            // Props without TypeScript
            if (content.Contains("defineProps(") && !content.Contains("defineProps<"))
                issues.Add("Props defined without TypeScript generic — use defineProps<{ prop: Type }>()");

            // Emits without TypeScript
            if (content.Contains("defineEmits(") && !content.Contains("defineEmits<"))
                issues.Add("Emits defined without TypeScript — use defineEmits<{ event: [arg: Type] }>()");

            // Relative imports instead of @/
            if (Regex.IsMatch(content, @"from '\.\.\/\.\.\/|from '\.\.\/\.\.\/\.\.\/"))
                issues.Add("Deep relative imports (../../) — use @/ alias instead");

            if (issues.Count > 0)
                results.Add(new VueAnalysisResult(
                    Path.GetFileName(file),
                    file,
                    issues));
        }

        return results.OrderBy(r => r.FilePath).ToList();
    }

    [McpServerTool, Description(
        "Validates Pinia store files for common issues: " +
        "stores missing $reset, state mutation outside actions, missing error handling in async actions, " +
        "and direct DOM access in stores.")]
    public async Task<IReadOnlyList<StoreValidationResult>> ValidatePiniaStores(
        [Description("Root directory to scan. Defaults to current directory.")] string? rootPath = null,
        CancellationToken ct = default)
    {
        var root = rootPath ?? Directory.GetCurrentDirectory();
        var results = new List<StoreValidationResult>();

        var storeFiles = Directory.GetFiles(root, "*Store.ts", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(root, "*store.ts", SearchOption.AllDirectories))
            .Distinct()
            .Where(f => !f.Contains("node_modules") && !f.Contains("dist"))
            .ToList();

        foreach (var file in storeFiles)
        {
            ct.ThrowIfCancellationRequested();
            var content = await File.ReadAllTextAsync(file, ct);
            var issues = new List<string>();

            if (!content.Contains("defineStore")) continue; // Not a Pinia store

            // Missing $reset
            if (!content.Contains("$reset"))
                issues.Add("No $reset function — stores should expose $reset() for cleanup on logout/navigation");

            // Async action without try/catch
            var asyncMatches = Regex.Matches(content, @"async\s+function\s+\w+|async\s+\w+\s*\(");
            foreach (Match match in asyncMatches)
            {
                var afterAsync = content.Substring(match.Index, Math.Min(500, content.Length - match.Index));
                if (!afterAsync.Contains("try {") && !afterAsync.Contains("try{"))
                    issues.Add($"Async function near line {CountLines(content, match.Index)} may be missing error handling");
            }

            // Direct document/window access
            if (content.Contains("document.") || content.Contains("window.location"))
                issues.Add("Direct DOM/window access in store — stores should not access DOM directly");

            // using localStorage directly for auth tokens
            if (content.Contains("localStorage.setItem") && file.Contains("auth", StringComparison.OrdinalIgnoreCase))
                issues.Add("Direct localStorage access for tokens — consider using a dedicated token service");

            if (issues.Count > 0)
                results.Add(new StoreValidationResult(
                    Path.GetFileName(file),
                    file,
                    issues));
        }

        return results.OrderBy(r => r.FilePath).ToList();
    }

    [McpServerTool, Description(
        "Finds TypeScript types that are missing for API response shapes — " +
        "api.ts files where responses are typed as 'any' or untyped, and response shapes that have no corresponding interface.")]
    public async Task<IReadOnlyList<MissingApiTypeItem>> FindMissingApiTypes(
        [Description("Root directory to scan. Defaults to current directory.")] string? rootPath = null,
        CancellationToken ct = default)
    {
        var root = rootPath ?? Directory.GetCurrentDirectory();
        var results = new List<MissingApiTypeItem>();

        var apiFiles = Directory.GetFiles(root, "api.ts", SearchOption.AllDirectories)
            .Where(f => !f.Contains("node_modules") && !f.Contains("dist"))
            .ToList();

        foreach (var file in apiFiles)
        {
            ct.ThrowIfCancellationRequested();
            var content = await File.ReadAllTextAsync(file, ct);

            // Find any-typed responses
            var anyMatches = Regex.Matches(content, @"Promise<any>|:\s*any\b|as any\b");
            foreach (Match match in anyMatches)
            {
                var lineNum = CountLines(content, match.Index);
                results.Add(new MissingApiTypeItem(
                    file,
                    lineNum,
                    "AnyTyped",
                    $"Line {lineNum}: `any` type used — define an explicit interface for this API response"));
            }

            // Find axios/fetch calls without generic type
            var untypedCalls = Regex.Matches(content,
                @"api\.(?:get|post|put|patch|delete)\s*\((?!<)");
            foreach (Match match in untypedCalls)
            {
                var lineNum = CountLines(content, match.Index);
                results.Add(new MissingApiTypeItem(
                    file,
                    lineNum,
                    "UntypedCall",
                    $"Line {lineNum}: API call without type parameter — use api.get<ResponseType>(...)"));
            }
        }

        return results.OrderBy(r => r.FilePath).ThenBy(r => r.Line).ToList();
    }

    private static int CountLines(string text, int position)
        => text[..position].Count(c => c == '\n') + 1;
}
