using System.ComponentModel;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

[McpServerToolType]
public sealed class DeadCodeTool(RoslynWorkspaceService workspace)
{
    [McpServerTool, Description(
        "Finds unused private and internal types, methods, and properties across the solution. " +
        "Checks reference counts via Roslyn — more accurate than regex-based approaches. " +
        "Excludes: test classes, entry points, serialization types, interface implementations.")]
    public async Task<IReadOnlyList<DeadCodeItem>> FindDeadCode(
        [Description("Filter to a specific project. Omit to scan all.")] string? projectName = null,
        [Description("Include private members. Default true.")] bool includePrivate = true,
        [Description("Include internal members. Default true.")] bool includeInternal = true,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<DeadCodeItem>();

        var projects = projectName is null
            ? solution.Projects
            : solution.Projects.Where(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            ct.ThrowIfCancellationRequested();
            var compilation = await workspace.GetCompilationAsync(project, ct);
            if (compilation is null) continue;

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                ct.ThrowIfCancellationRequested();
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = await syntaxTree.GetRootAsync(ct);

                // Collect candidate symbols from this file
                var candidates = root.DescendantNodes()
                    .Select(n => semanticModel.GetDeclaredSymbol(n))
                    .Where(s => s is INamedTypeSymbol or IMethodSymbol or IPropertySymbol or IFieldSymbol)
                    .Where(s => s is not null)
                    .Cast<ISymbol>()
                    .Distinct(SymbolEqualityComparer.Default)
                    .ToList();

                foreach (var symbol in candidates)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!ShouldAnalyze(symbol, includePrivate, includeInternal)) continue;

                    var refs = await SymbolFinder.FindReferencesAsync(symbol, solution, ct);
                    var refCount = refs.Sum(r => r.Locations.Count());

                    if (refCount > 0) continue;

                    var loc = symbol.Locations.FirstOrDefault(l => l.IsInSource);
                    if (loc is null) continue;
                    var lineSpan = loc.GetLineSpan();

                    results.Add(new DeadCodeItem(
                        symbol.Name,
                        symbol.Kind.ToString(),
                        symbol.ContainingType?.Name ?? "n/a",
                        lineSpan.Path ?? "unknown",
                        lineSpan.StartLinePosition.Line + 1,
                        project.Name));
                }
            }
        }

        return results.OrderBy(r => r.ProjectName).ThenBy(r => r.FilePath).ThenBy(r => r.Line).ToList();
    }

    private static bool ShouldAnalyze(ISymbol symbol, bool includePrivate, bool includeInternal)
    {
        var accessibility = symbol.DeclaredAccessibility;

        if (accessibility == Accessibility.Private && !includePrivate) return false;
        if (accessibility == Accessibility.Internal && !includeInternal) return false;

        // Only analyze private and internal (public might be external API surface)
        if (accessibility is not (Accessibility.Private or Accessibility.Internal)) return false;

        // Skip compiler-generated
        if (symbol.IsImplicitlyDeclared) return false;

        // Skip abstract/virtual members — referenced via polymorphism
        if (symbol.IsAbstract || symbol.IsVirtual) return false;

        // Skip overrides
        if (symbol is IMethodSymbol { IsOverride: true }) return false;

        // Skip interface implementations
        if (symbol is IMethodSymbol ms && ms.ExplicitInterfaceImplementations.Length > 0) return false;
        if (symbol is IPropertySymbol ps && ps.ExplicitInterfaceImplementations.Length > 0) return false;

        // Skip entry points and constructors of public types
        if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } ctor &&
            ctor.ContainingType.DeclaredAccessibility == Accessibility.Public) return false;

        // Skip types with serialization attributes
        if (symbol is INamedTypeSymbol type)
        {
            var attrNames = type.GetAttributes().Select(a => a.AttributeClass?.Name ?? "").ToHashSet();
            if (attrNames.Overlaps(new[] { "SerializableAttribute", "JsonSerializableAttribute", "DataContractAttribute" }))
                return false;
        }

        return true;
    }
}
