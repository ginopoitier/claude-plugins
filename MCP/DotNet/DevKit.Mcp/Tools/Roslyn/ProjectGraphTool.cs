using System.ComponentModel;
using System.Xml.Linq;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

/// <summary>
/// Provides MCP tools for visualizing the project dependency graph and validating Clean Architecture layer boundaries.
/// </summary>
[McpServerToolType]
public sealed class ProjectGraphTool(SolutionOptions options)
{
    [McpServerTool, Description(
        "Returns the project dependency graph for a .NET solution — which projects reference which. " +
        "Use this to understand solution structure without reading individual files.")]
    /// <summary>
    /// Parses the solution file and returns each project with its direct project references.
    /// </summary>
    public IReadOnlyList<ProjectNode> GetProjectGraph(
        [Description("Path to the .sln file. Defaults to the solution passed at server startup.")] string? solutionPath = null)
    {
        var slnPath = solutionPath ?? options.SolutionPath
            ?? throw new InvalidOperationException("No solution path available.");

        var slnDir = Path.GetDirectoryName(slnPath)!;
        var csprojPaths = FindCsprojsFromSln(slnPath, slnDir);

        return csprojPaths
            .Select(path => ParseProjectNode(path, slnDir))
            .OrderBy(n => n.Name)
            .ToList();
    }

    [McpServerTool, Description(
        "Checks a .NET solution for Clean Architecture layer violations — " +
        "Application referencing Infrastructure, Domain referencing Application, etc.")]
    /// <summary>
    /// Detects Clean Architecture layer violations where inner layers reference outer layers by inspecting project references.
    /// </summary>
    public IReadOnlyList<LayerViolation> CheckLayerViolations(
        [Description("Path to the .sln file. Defaults to the solution passed at server startup.")] string? solutionPath = null)
    {
        var slnPath = solutionPath ?? options.SolutionPath
            ?? throw new InvalidOperationException("No solution path available.");

        var slnDir = Path.GetDirectoryName(slnPath)!;
        var nodes = FindCsprojsFromSln(slnPath, slnDir)
            .Select(p => ParseProjectNode(p, slnDir))
            .ToList();

        var violations = new List<LayerViolation>();

        foreach (var project in nodes)
        {
            var layer = DetectLayer(project.Name);
            if (layer is null) continue;

            foreach (var dep in project.References)
            {
                var depLayer = DetectLayer(dep);
                if (depLayer is null) continue;

                var violation = (layer, depLayer) switch
                {
                    ("Domain", "Application") => $"Domain must not reference Application",
                    ("Domain", "Infrastructure") => $"Domain must not reference Infrastructure",
                    ("Domain", "Api") => $"Domain must not reference Api",
                    ("Application", "Infrastructure") => $"Application must not reference Infrastructure — define an interface instead",
                    ("Application", "Api") => $"Application must not reference Api",
                    _ => null
                };

                if (violation is not null)
                    violations.Add(new LayerViolation(project.Name, dep, "LayerViolation", violation));
            }
        }

        return violations;
    }

    // --- helpers ---

    private static IEnumerable<string> FindCsprojsFromSln(string slnPath, string slnDir)
    {
        var lines = File.ReadAllLines(slnPath);
        return lines
            .Where(l => l.TrimStart().StartsWith("Project(") && l.Contains(".csproj"))
            .Select(l =>
            {
                // Project("{...}") = "Name", "relative\path.csproj", "{guid}"
                var parts = l.Split('"');
                var relativePath = parts.Length >= 6 ? parts[5] : null;
                return relativePath is not null
                    ? Path.GetFullPath(Path.Combine(slnDir, relativePath.Replace('\\', Path.DirectorySeparatorChar)))
                    : null;
            })
            .Where(p => p is not null && File.Exists(p))
            .Select(p => p!);
    }

    private static ProjectNode ParseProjectNode(string csprojPath, string slnDir)
    {
        var name = Path.GetFileNameWithoutExtension(csprojPath);
        var xml = XDocument.Load(csprojPath);
        var ns = xml.Root?.Name.Namespace ?? XNamespace.None;

        var deps = xml.Descendants(ns + "ProjectReference")
            .Select(r => r.Attribute("Include")?.Value)
            .Where(v => v is not null)
            .Select(v => Path.GetFileNameWithoutExtension(v!))
            .OrderBy(d => d)
            .ToList();

        return new ProjectNode(name, csprojPath, deps);
    }

    private static string? DetectLayer(string projectName)
    {
        var lower = projectName.ToLowerInvariant();
        if (lower.EndsWith(".domain") || lower.Contains(".domain.")) return "Domain";
        if (lower.EndsWith(".application") || lower.Contains(".application.")) return "Application";
        if (lower.EndsWith(".infrastructure") || lower.Contains(".infrastructure.")) return "Infrastructure";
        if (lower.EndsWith(".api") || lower.EndsWith(".web") || lower.Contains(".presentation.")) return "Api";
        return null;
    }
}
