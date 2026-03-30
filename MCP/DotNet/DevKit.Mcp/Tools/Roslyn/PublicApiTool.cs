using System.ComponentModel;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using Microsoft.CodeAnalysis;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

/// <summary>
/// Provides an MCP tool for enumerating the public API surface of a named type without reading its source file directly.
/// </summary>
[McpServerToolType]
public sealed class PublicApiTool(RoslynWorkspaceService workspace)
{
    [McpServerTool, Description(
        "Returns all public members of a type — methods, properties, constructors. " +
        "Much cheaper than reading the full file. Use to understand a type's contract before working with it.")]
    /// <summary>
    /// Returns all public members of a type — methods, properties, and constructors — with their signatures and XML docs.
    /// </summary>
    public async Task<PublicApiResult?> GetPublicApi(
        [Description("Type name to inspect, e.g. 'OrderHandler', 'IOrderRepository'.")] string typeName,
        [Description("Filter to a specific project. Omit to search all.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var declarations = new List<ISymbol>();

        foreach (var proj in solution.Projects)
        {
            var decls = await Microsoft.CodeAnalysis.FindSymbols.SymbolFinder
                .FindDeclarationsAsync(proj, typeName, ignoreCase: false, ct);
            declarations.AddRange(decls);
        }

        INamedTypeSymbol? typeSymbol;
        if (projectName is null)
        {
            typeSymbol = declarations.OfType<INamedTypeSymbol>().FirstOrDefault();
        }
        else
        {
            var targetProject = solution.Projects.FirstOrDefault(p => p.Name == projectName);
            var compilation = targetProject is not null
                ? await targetProject.GetCompilationAsync(ct)
                : null;
            typeSymbol = declarations
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault(s => compilation?.GetTypeByMetadataName(s.MetadataName) is not null);
        }

        if (typeSymbol is null) return null;

        var project = FindProjectForSymbol(solution, typeSymbol);
        var location = typeSymbol.Locations.FirstOrDefault(l => l.IsInSource);

        var members = typeSymbol.GetMembers()
            .Where(m => m.DeclaredAccessibility == Accessibility.Public && !m.IsImplicitlyDeclared)
            .Select(m => new PublicMember(
                m.Name,
                m.Kind.ToString(),
                m.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                GetXmlDoc(m),
                m.IsStatic,
                m.IsAbstract))
            .OrderBy(m => m.Kind)
            .ThenBy(m => m.Name)
            .ToList();

        return new PublicApiResult(
            typeSymbol.Name,
            typeSymbol.ContainingNamespace?.ToDisplayString() ?? "",
            project?.Name ?? "unknown",
            location?.GetLineSpan().Path ?? "unknown",
            members);
    }

    private static Project? FindProjectForSymbol(Solution solution, ISymbol symbol)
    {
        var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        if (location is null) return null;
        return solution.Projects.FirstOrDefault(p =>
            p.Documents.Any(d => d.FilePath == location.GetLineSpan().Path));
    }

    private static string? GetXmlDoc(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml)) return null;
        var start = xml.IndexOf("<summary>", StringComparison.OrdinalIgnoreCase);
        var end = xml.IndexOf("</summary>", StringComparison.OrdinalIgnoreCase);
        if (start < 0 || end < 0) return null;
        return xml[(start + 9)..end].Trim();
    }
}
