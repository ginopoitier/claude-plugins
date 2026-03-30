using System.ComponentModel;
using System.Text;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using Microsoft.CodeAnalysis;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

/// <summary>
/// Provides MCP tools for reporting XML documentation coverage and generating Mermaid architecture diagrams.
/// </summary>
[McpServerToolType]
public sealed class DocumentationTool(RoslynWorkspaceService workspace)
{
    [McpServerTool, Description(
        "Reports public APIs missing XML documentation. " +
        "Returns coverage percentage per project and a list of gaps (missing summary, params, returns). " +
        "Filters to public members only — internal/private members are excluded.")]
    /// <summary>
    /// Reports documentation coverage for public APIs and lists members missing an XML summary.
    /// </summary>
    public async Task<object> GetDocumentationCoverage(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        [Description("Only return members missing docs (omit to see all). Default true.")] bool gapsOnly = true,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var gaps = new List<DocumentationGap>();
        var stats = new Dictionary<string, (int Total, int Documented)>();

        var projects = projectName is null
            ? solution.Projects
            : solution.Projects.Where(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            ct.ThrowIfCancellationRequested();
            var compilation = await workspace.GetCompilationAsync(project, ct);
            if (compilation is null) continue;

            stats[project.Name] = (0, 0);

            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                if (document.FilePath is null) continue;

                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null) continue;
                var syntaxTree = await document.GetSyntaxTreeAsync(ct);
                if (syntaxTree is null) continue;

                var model = compilation.GetSemanticModel(syntaxTree);

                foreach (var node in root.DescendantNodes())
                {
                    var symbol = model.GetDeclaredSymbol(node);
                    if (symbol is null) continue;
                    if (symbol.DeclaredAccessibility != Accessibility.Public) continue;
                    if (symbol.IsImplicitlyDeclared) continue;

                    if (symbol is not (INamedTypeSymbol or IMethodSymbol or IPropertySymbol)) continue;

                    var (total, documented) = stats[project.Name];
                    total++;

                    var xml = symbol.GetDocumentationCommentXml();
                    var hasDoc = !string.IsNullOrWhiteSpace(xml) && xml.Contains("<summary>");

                    if (hasDoc)
                    {
                        stats[project.Name] = (total, documented + 1);
                    }
                    else
                    {
                        stats[project.Name] = (total, documented);
                        var loc = symbol.Locations.FirstOrDefault(l => l.IsInSource);
                        var lineSpan = loc?.GetLineSpan();

                        gaps.Add(new DocumentationGap(
                            symbol.Name,
                            symbol.Kind.ToString(),
                            lineSpan?.Path ?? document.FilePath,
                            (lineSpan?.StartLinePosition.Line ?? 0) + 1,
                            "MissingSummary",
                            project.Name));
                    }
                }
            }
        }

        var coverageReport = stats.Select(kv => new
        {
            Project = kv.Key,
            Total = kv.Value.Total,
            Documented = kv.Value.Documented,
            CoveragePercent = kv.Value.Total == 0 ? 100 : kv.Value.Documented * 100 / kv.Value.Total,
            Gaps = gaps.Count(g => g.ProjectName == kv.Key)
        }).ToList();

        return new
        {
            Summary = coverageReport,
            Gaps = gapsOnly ? gaps : gaps
        };
    }

    [McpServerTool, Description(
        "Generates a Mermaid graph diagram of project dependencies. " +
        "Color-codes by Clean Architecture layer: Domain (green), Application (blue), Infrastructure (orange), Api (red). " +
        "Paste directly into GitHub markdown, Obsidian, or Confluence.")]
    /// <summary>
    /// Generates a Mermaid diagram showing either project dependencies or a type's class hierarchy.
    /// </summary>
    public async Task<string> GenerateMermaidDiagram(
        [Description("'dependencies' for project graph, 'hierarchy' for type hierarchy of a specific type.")] string diagramType = "dependencies",
        [Description("For hierarchy diagrams: the type name to visualize.")] string? typeName = null,
        CancellationToken ct = default)
    {
        if (diagramType.Equals("hierarchy", StringComparison.OrdinalIgnoreCase) && typeName is not null)
            return await GenerateTypeHierarchyDiagram(typeName, ct);

        return await GenerateProjectDependencyDiagram(ct);
    }

    private async Task<string> GenerateProjectDependencyDiagram(CancellationToken ct)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("```mermaid");
        sb.AppendLine("graph TD");
        sb.AppendLine();

        // Layer color classes
        sb.AppendLine("    classDef domain fill:#22c55e,color:#fff,stroke:#16a34a");
        sb.AppendLine("    classDef application fill:#3b82f6,color:#fff,stroke:#2563eb");
        sb.AppendLine("    classDef infrastructure fill:#f97316,color:#fff,stroke:#ea580c");
        sb.AppendLine("    classDef api fill:#ef4444,color:#fff,stroke:#dc2626");
        sb.AppendLine("    classDef other fill:#94a3b8,color:#fff,stroke:#64748b");
        sb.AppendLine();

        var projectNodes = new List<ProjectNode>();
        foreach (var project in solution.Projects)
        {
            ct.ThrowIfCancellationRequested();
            var refs = project.ProjectReferences
                .Select(r => solution.GetProject(r.ProjectId)?.Name)
                .Where(n => n is not null)
                .Cast<string>()
                .ToList();

            projectNodes.Add(new ProjectNode(project.Name, project.FilePath ?? "", refs));
        }

        // Emit nodes with sanitized IDs
        foreach (var node in projectNodes)
        {
            var id = SanitizeId(node.Name);
            var layer = DetectLayer(node.Name);
            sb.AppendLine($"    {id}[\"{node.Name}\"]:::{layer}");
        }

        sb.AppendLine();

        // Emit edges
        foreach (var node in projectNodes)
        {
            var fromId = SanitizeId(node.Name);
            foreach (var dep in node.References)
            {
                var toId = SanitizeId(dep);
                sb.AppendLine($"    {fromId} --> {toId}");
            }
        }

        sb.AppendLine("```");
        return sb.ToString();
    }

    private async Task<string> GenerateTypeHierarchyDiagram(string typeName, CancellationToken ct)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var declarations = await FindSymbolTool.FindAcrossSolutionAsync(solution, typeName, ignoreCase: false, ct);
        var typeSymbol = declarations.OfType<INamedTypeSymbol>().FirstOrDefault();

        if (typeSymbol is null) return $"Type '{typeName}' not found.";

        var sb = new StringBuilder();
        sb.AppendLine("```mermaid");
        sb.AppendLine("classDiagram");

        // Base type
        if (typeSymbol.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
            sb.AppendLine($"    {baseType.Name} <|-- {typeSymbol.Name}");

        // Interfaces
        foreach (var iface in typeSymbol.Interfaces)
            sb.AppendLine($"    {iface.Name} <|.. {typeSymbol.Name}");

        // Members
        sb.AppendLine($"    class {typeSymbol.Name} {{");
        foreach (var member in typeSymbol.GetMembers()
                     .Where(m => m.DeclaredAccessibility == Accessibility.Public && !m.IsImplicitlyDeclared))
        {
            var prefix = member.IsStatic ? "$ " : "+ ";
            sb.AppendLine($"        {prefix}{member.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
        }
        sb.AppendLine("    }");

        sb.AppendLine("```");
        return sb.ToString();
    }

    private static string SanitizeId(string name) =>
        new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

    private static string DetectLayer(string projectName) =>
        projectName.Split('.').LastOrDefault()?.ToLowerInvariant() switch
        {
            "domain" => "domain",
            "application" => "application",
            "infrastructure" => "infrastructure",
            "api" or "web" or "host" => "api",
            _ => "other"
        };
}
