using System.ComponentModel;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

/// <summary>
/// Provides MCP tools for locating symbols, resolving references, and navigating type hierarchies across a Roslyn solution.
/// </summary>
[McpServerToolType]
public sealed class FindSymbolTool(RoslynWorkspaceService workspace)
{
    // ── FindSymbol ─────────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Finds where a type, method, property, or interface is defined. " +
        "Returns file path, line, and signature. ~50 tokens vs 1000+ for file reads.")]
    /// <summary>
    /// Searches the solution for a symbol by name, returning its file location and signature.
    /// </summary>
    public async Task<IReadOnlyList<SymbolLocation>> FindSymbol(
        [Description("Name to search for.")] string name,
        [Description("Filter by kind: Class, Interface, Method, Property, Field, Enum. Omit for all.")] string? kind = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var declarations = await FindAcrossSolutionAsync(solution, name, ignoreCase: false, ct);
        if (!declarations.Any())
            declarations = await FindAcrossSolutionAsync(solution, name, ignoreCase: true, ct);

        var results = new List<SymbolLocation>();
        foreach (var symbol in declarations)
        {
            if (kind is not null && !symbol.Kind.ToString().Equals(kind, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var loc in symbol.Locations.Where(l => l.IsInSource))
            {
                var lineSpan = loc.GetLineSpan();
                var project = solution.Projects.FirstOrDefault(p =>
                    p.Documents.Any(d => d.FilePath == lineSpan.Path));

                results.Add(new SymbolLocation(
                    symbol.Name, symbol.Kind.ToString(),
                    lineSpan.Path ?? "unknown",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    symbol.ContainingNamespace?.ToDisplayString() ?? "",
                    project?.Name ?? "unknown"));
            }
        }

        return results.OrderBy(r => r.ProjectName).ThenBy(r => r.FilePath).ThenBy(r => r.Line).ToList();
    }

    // ── FindReferences ─────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Finds all usages/references of a symbol across the solution. " +
        "Returns each call site with file and line. Use before refactoring.")]
    /// <summary>
    /// Finds all usage sites of a named symbol across the solution.
    /// </summary>
    public async Task<IReadOnlyList<SymbolLocation>> FindReferences(
        [Description("Exact symbol name to find references for.")] string name,
        [Description("Filter to a specific project. Omit to search all.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var declarations = await FindAcrossSolutionAsync(solution, name, ignoreCase: false, ct);
        var symbol = declarations.FirstOrDefault();
        if (symbol is null) return [];

        var references = await SymbolFinder.FindReferencesAsync(symbol, solution, ct);
        var results = new List<SymbolLocation>();

        foreach (var refGroup in references)
        {
            foreach (var loc in refGroup.Locations)
            {
                var project = solution.GetDocument(loc.Document.Id)?.Project;
                if (projectName is not null && project?.Name != projectName) continue;

                var lineSpan = loc.Location.GetLineSpan();
                var sourceText = await loc.Document.GetTextAsync(ct);
                var lineText = sourceText.Lines[lineSpan.StartLinePosition.Line].ToString().Trim();

                results.Add(new SymbolLocation(
                    name, "Reference",
                    lineSpan.Path ?? loc.Document.FilePath ?? "unknown",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    lineText,
                    project?.Name ?? "unknown",
                    project?.Name ?? "unknown"));
            }
        }

        return results.OrderBy(r => r.ProjectName).ThenBy(r => r.FilePath).ThenBy(r => r.Line).ToList();
    }

    // ── FindImplementations ────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Finds all types that implement an interface or inherit from a base class. " +
        "Use to understand the full type hierarchy before modifying a contract.")]
    /// <summary>
    /// Returns all types that implement a given interface or inherit from a base class.
    /// </summary>
    public async Task<IReadOnlyList<ImplementationInfo>> FindImplementations(
        [Description("Interface or base class name, e.g. 'IOrderRepository', 'BaseHandler'.")] string typeName,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var declarations = await FindAcrossSolutionAsync(solution, typeName, ignoreCase: false, ct);
        var typeSymbol = declarations.OfType<INamedTypeSymbol>().FirstOrDefault();
        if (typeSymbol is null) return [];

        var implementations = await SymbolFinder.FindImplementationsAsync(typeSymbol, solution, cancellationToken: ct);
        var derivedClasses = typeSymbol.TypeKind == TypeKind.Class
            ? await SymbolFinder.FindDerivedClassesAsync(typeSymbol, solution, transitive: true, cancellationToken: ct)
            : [];

        var all = implementations.Cast<INamedTypeSymbol>()
            .Concat(derivedClasses)
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<INamedTypeSymbol>();

        return BuildImplementationInfoList(all, solution);
    }

    // ── FindCallers ────────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Finds all methods that call the specified method. " +
        "Use to understand impact before changing a method signature.")]
    /// <summary>
    /// Returns all methods that directly call the specified method, with call-site file and line.
    /// </summary>
    public async Task<IReadOnlyList<CallerInfo>> FindCallers(
        [Description("Method name to find callers of.")] string methodName,
        [Description("Filter to a specific project. Omit to search all.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var declarations = await FindAcrossSolutionAsync(solution, methodName, ignoreCase: false, ct);
        var method = declarations.OfType<IMethodSymbol>().FirstOrDefault();
        if (method is null) return [];

        var callers = await SymbolFinder.FindCallersAsync(method, solution, ct);
        var results = new List<CallerInfo>();

        foreach (var caller in callers)
        {
            foreach (var loc in caller.Locations)
            {
                var project = solution.Projects.FirstOrDefault(p =>
                    p.Documents.Any(d => d.FilePath == loc.GetLineSpan().Path));
                if (projectName is not null && project?.Name != projectName) continue;

                var lineSpan = loc.GetLineSpan();
                var doc = solution.Projects
                    .SelectMany(p => p.Documents)
                    .FirstOrDefault(d => d.FilePath == lineSpan.Path);

                var snippet = string.Empty;
                if (doc is not null)
                {
                    var text = await doc.GetTextAsync(ct);
                    var lineIdx = lineSpan.StartLinePosition.Line;
                    if (lineIdx < text.Lines.Count)
                        snippet = text.Lines[lineIdx].ToString().Trim();
                }

                results.Add(new CallerInfo(
                    caller.CallingSymbol.Name,
                    caller.CallingSymbol.ContainingType?.Name ?? "unknown",
                    lineSpan.Path ?? "unknown",
                    lineSpan.StartLinePosition.Line + 1,
                    snippet,
                    project?.Name ?? "unknown"));
            }
        }

        return results.OrderBy(r => r.ProjectName).ThenBy(r => r.FilePath).ThenBy(r => r.Line).ToList();
    }

    // ── FindOverrides ──────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Finds all overrides of a virtual or abstract method. " +
        "Use before changing base method behavior to see all affected implementations.")]
    /// <summary>
    /// Returns all overriding implementations of a virtual or abstract method across the solution.
    /// </summary>
    public async Task<IReadOnlyList<OverrideInfo>> FindOverrides(
        [Description("Virtual or abstract method name.")] string methodName,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var declarations = await FindAcrossSolutionAsync(solution, methodName, ignoreCase: false, ct);
        var method = declarations.OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.IsVirtual || m.IsAbstract);
        if (method is null) return [];

        var overrides = await SymbolFinder.FindOverridesAsync(method, solution, cancellationToken: ct);
        var results = new List<OverrideInfo>();

        foreach (var ov in overrides.OfType<IMethodSymbol>())
        {
            foreach (var loc in ov.Locations.Where(l => l.IsInSource))
            {
                var lineSpan = loc.GetLineSpan();
                var project = solution.Projects.FirstOrDefault(p =>
                    p.Documents.Any(d => d.FilePath == lineSpan.Path));

                results.Add(new OverrideInfo(
                    ov.Name,
                    ov.ContainingType?.Name ?? "unknown",
                    lineSpan.Path ?? "unknown",
                    lineSpan.StartLinePosition.Line + 1,
                    ov.IsSealed,
                    project?.Name ?? "unknown"));
            }
        }

        return results.OrderBy(r => r.ProjectName).ThenBy(r => r.FilePath).ToList();
    }

    // ── GetTypeHierarchy ───────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Returns the full type hierarchy for a class or interface — base types, interfaces, derived types. " +
        "Use to understand inheritance before making structural changes.")]
    /// <summary>
    /// Builds the complete type hierarchy for a class or interface, including base types, interfaces, and derived types.
    /// </summary>
    public async Task<TypeHierarchyResult?> GetTypeHierarchy(
        [Description("Type name to get the hierarchy for.")] string typeName,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var declarations = await FindAcrossSolutionAsync(solution, typeName, ignoreCase: false, ct);
        var typeSymbol = declarations.OfType<INamedTypeSymbol>().FirstOrDefault();
        if (typeSymbol is null) return null;

        var location = typeSymbol.Locations.FirstOrDefault(l => l.IsInSource);
        var project = FindProjectForSymbol(solution, typeSymbol);

        var baseType = typeSymbol.BaseType is { SpecialType: not SpecialType.System_Object }
            ? typeSymbol.BaseType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            : null;

        var interfaces = typeSymbol.Interfaces
            .Select(i => i.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))
            .ToList();

        var derivedClasses = typeSymbol.TypeKind == TypeKind.Class
            ? await SymbolFinder.FindDerivedClassesAsync(typeSymbol, solution, transitive: true, cancellationToken: ct)
            : [];

        var implementations = typeSymbol.TypeKind == TypeKind.Interface
            ? await SymbolFinder.FindImplementationsAsync(typeSymbol, solution, cancellationToken: ct)
            : [];

        return new TypeHierarchyResult(
            typeSymbol.Name,
            typeSymbol.ContainingNamespace?.ToDisplayString() ?? "",
            project?.Name ?? "unknown",
            location?.GetLineSpan().Path ?? "unknown",
            baseType,
            interfaces,
            BuildImplementationInfoList(derivedClasses.Cast<INamedTypeSymbol>(), solution),
            BuildImplementationInfoList(implementations.Cast<INamedTypeSymbol>(), solution));
    }

    // ── GetSymbolDetail ────────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Returns full detail for a symbol: signature, parameters, return type, accessibility, XML docs. " +
        "Cheaper than reading the file — use before working with a method.")]
    /// <summary>
    /// Returns the full detail of a symbol — signature, parameters, return type, accessibility, and XML docs.
    /// </summary>
    public async Task<SymbolDetail?> GetSymbolDetail(
        [Description("Symbol name (type, method, property).")] string name,
        [Description("Filter by kind: Class, Interface, Method, Property. Omit for first match.")] string? kind = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var declarations = await FindAcrossSolutionAsync(solution, name, ignoreCase: false, ct);

        var symbol = kind is null
            ? declarations.FirstOrDefault()
            : declarations.FirstOrDefault(s => s.Kind.ToString().Equals(kind, StringComparison.OrdinalIgnoreCase));

        if (symbol is null) return null;

        var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        var project = FindProjectForSymbol(solution, symbol);
        var lineSpan = location?.GetLineSpan();

        var parameters = symbol is IMethodSymbol m
            ? m.Parameters.Select(p => new ParameterDetail(
                p.Name, p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                p.HasExplicitDefaultValue,
                p.HasExplicitDefaultValue ? p.ExplicitDefaultValue?.ToString() : null)).ToList()
            : (IReadOnlyList<ParameterDetail>)[];

        var returnType = symbol switch
        {
            IMethodSymbol ms => ms.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            IPropertySymbol ps => ps.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            IFieldSymbol fs => fs.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            _ => null
        };

        return new SymbolDetail(
            symbol.Name,
            symbol.Kind.ToString(),
            symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            symbol.ContainingNamespace?.ToDisplayString() ?? "",
            project?.Name ?? "unknown",
            lineSpan?.Path ?? "unknown",
            (lineSpan?.StartLinePosition.Line ?? 0) + 1,
            returnType,
            parameters,
            GetXmlDoc(symbol),
            symbol.IsStatic,
            symbol.IsAbstract,
            symbol is IMethodSymbol { IsVirtual: true },
            symbol is IMethodSymbol { IsOverride: true },
            symbol.DeclaredAccessibility.ToString());
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Searches every project in the solution for declarations matching <paramref name="name"/>.
    /// </summary>
    internal static async Task<IEnumerable<ISymbol>> FindAcrossSolutionAsync(
        Solution solution, string name, bool ignoreCase, CancellationToken ct)
    {
        var results = new List<ISymbol>();
        foreach (var project in solution.Projects)
        {
            var decls = await SymbolFinder.FindDeclarationsAsync(project, name, ignoreCase, ct);
            results.AddRange(decls);
        }
        return results;
    }

    private static IReadOnlyList<ImplementationInfo> BuildImplementationInfoList(
        IEnumerable<INamedTypeSymbol> symbols, Solution solution)
    {
        var results = new List<ImplementationInfo>();
        foreach (var sym in symbols)
        {
            var loc = sym.Locations.FirstOrDefault(l => l.IsInSource);
            if (loc is null) continue;
            var lineSpan = loc.GetLineSpan();
            var project = solution.Projects.FirstOrDefault(p =>
                p.Documents.Any(d => d.FilePath == lineSpan.Path));

            results.Add(new ImplementationInfo(
                sym.Name, sym.TypeKind.ToString(),
                lineSpan.Path ?? "unknown",
                lineSpan.StartLinePosition.Line + 1,
                sym.ContainingNamespace?.ToDisplayString() ?? "",
                project?.Name ?? "unknown"));
        }
        return results.OrderBy(r => r.ProjectName).ThenBy(r => r.TypeName).ToList();
    }

    private static Project? FindProjectForSymbol(Solution solution, ISymbol symbol)
    {
        var loc = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        if (loc is null) return null;
        return solution.Projects.FirstOrDefault(p =>
            p.Documents.Any(d => d.FilePath == loc.GetLineSpan().Path));
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
