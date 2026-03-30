using System.ComponentModel;
using DevKit.Mcp.Analyzers;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using Microsoft.CodeAnalysis.CSharp;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

/// <summary>
/// Provides MCP tools for detecting .NET coding anti-patterns using fast syntax-only and deeper semantic analyzers.
/// </summary>
[McpServerToolType]
public sealed class AntipatternTool(RoslynWorkspaceService workspace, SolutionOptions options)
{
    private static readonly IReadOnlyList<IAntiPatternDetector> AllDetectors =
    [
        // Syntax-only (fast, no compilation needed)
        new AsyncVoidDetector(),
        new SyncOverAsyncDetector(),
        new InlineHttpClientDetector(),
        new DateTimeNowDetector(),
        new EmptyCatchDetector(),
        new ThreadSleepDetector(),
        new ConsoleWriteLineDetector(),
        new HardcodedSecretDetector(),
        new PragmaWithoutRestoreDetector(),

        // Semantic (requires compilation, loaded on demand)
        new MissingCancellationTokenDetector(),
        new EfCoreNoTrackingDetector(),
        new LoggingInterpolationDetector(),
    ];

    [McpServerTool, Description(
        "Scans .NET source files for anti-patterns using two-phase detection: " +
        "fast syntax-only analyzers run first, semantic analyzers only load compilations when needed. " +
        "Detects: async void, sync-over-async, new HttpClient(), DateTime.Now/UtcNow, empty catch, " +
        "Thread.Sleep, Console.WriteLine, hardcoded secrets, pragma without restore, " +
        "missing CancellationToken, EF Core missing AsNoTracking, logging string interpolation.")]
    /// <summary>
    /// Runs configured anti-pattern detectors across C# source files and returns all matches, ordered by severity.
    /// </summary>
    public async Task<IReadOnlyList<AntiPatternMatch>> DetectAntipatterns(
        [Description("Root directory to scan. Defaults to solution directory.")] string? rootPath = null,
        [Description("Comma-separated pattern IDs to run. Omit for all. E.g. 'AsyncVoid,SyncOverAsync'")] string? patterns = null,
        [Description("Include semantic analyzers (slower, requires compilation). Default true.")] bool includeSemantic = true,
        CancellationToken ct = default)
    {
        var root = rootPath
            ?? (options.SolutionPath is not null ? Path.GetDirectoryName(options.SolutionPath) : null)
            ?? Directory.GetCurrentDirectory();

        var activeDetectors = FilterDetectors(patterns, includeSemantic);
        var syntaxDetectors = activeDetectors.Where(d => d.Mode == DetectorMode.Syntax).ToList();
        var semanticDetectors = activeDetectors.Where(d => d.Mode == DetectorMode.Semantic).ToList();

        var csFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToList();

        var matches = new List<AntiPatternMatch>();

        // Phase 1 — Syntax (no compilation needed)
        foreach (var file in csFiles)
        {
            ct.ThrowIfCancellationRequested();
            var source = await File.ReadAllTextAsync(file, ct);
            var tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
            var projectName = ResolveProjectName(file, root);

            foreach (var detector in syntaxDetectors)
                matches.AddRange(detector.Analyze(tree, file, projectName));
        }

        // Phase 2 — Semantic (only when semantic detectors are active)
        if (semanticDetectors.Count > 0 && includeSemantic)
        {
            var solution = await workspace.GetSolutionAsync(ct);
            foreach (var project in solution.Projects)
            {
                ct.ThrowIfCancellationRequested();
                var compilation = await workspace.GetCompilationAsync(project, ct);
                if (compilation is null) continue;

                foreach (var document in project.Documents)
                {
                    ct.ThrowIfCancellationRequested();
                    if (document.FilePath is null) continue;

                    var tree = await document.GetSyntaxTreeAsync(ct);
                    if (tree is null) continue;

                    var model = compilation.GetSemanticModel(tree);

                    foreach (var detector in semanticDetectors)
                    {
                        var semanticMatches = await detector.AnalyzeSemanticAsync(
                            model, tree, document.FilePath, project.Name, ct);
                        matches.AddRange(semanticMatches);
                    }
                }
            }
        }

        return matches
            .OrderBy(m => m.Severity == "Error" ? 0 : 1)
            .ThenBy(m => m.PatternId)
            .ThenBy(m => m.FilePath)
            .ThenBy(m => m.Line)
            .ToList();
    }

    /// <summary>
    /// Lists all registered anti-pattern detector IDs and their execution mode (Syntax or Semantic).
    /// </summary>
    [McpServerTool, Description("Lists all available anti-pattern detector IDs and their descriptions.")]
    public IReadOnlyList<object> ListPatterns() =>
        AllDetectors.Select(d => new
        {
            Id = d.PatternId,
            Mode = d.Mode.ToString()
        }).ToList<object>();

    private static IReadOnlyList<IAntiPatternDetector> FilterDetectors(string? patterns, bool includeSemantic)
    {
        var all = includeSemantic
            ? AllDetectors
            : AllDetectors.Where(d => d.Mode == DetectorMode.Syntax).ToList();

        if (patterns is null) return all.ToList();

        var ids = patterns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return all.Where(d => ids.Contains(d.PatternId)).ToList();
    }

    private static string ResolveProjectName(string filePath, string root)
    {
        var dir = Path.GetDirectoryName(filePath);
        while (dir is not null && dir.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            var csproj = Directory.GetFiles(dir, "*.csproj").FirstOrDefault();
            if (csproj is not null) return Path.GetFileNameWithoutExtension(csproj);
            dir = Path.GetDirectoryName(dir);
        }
        return "Unknown";
    }
}
