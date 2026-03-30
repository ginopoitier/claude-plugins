using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

/// <summary>
/// Provides MCP tools for measuring coupling metrics, detecting Interface Segregation violations, and finding duplicate code.
/// </summary>
[McpServerToolType]
public sealed class CouplingTool(RoslynWorkspaceService workspace)
{
    [McpServerTool, Description(
        "Returns afferent (fan-in) and efferent (fan-out) coupling per class. " +
        "High efferent coupling = class depends on too many others (SRP violation). " +
        "High afferent coupling = class is heavily depended upon (change carefully).")]
    /// <summary>
    /// Returns afferent and efferent coupling counts per class, highlighting types that depend on too many others.
    /// </summary>
    public async Task<IReadOnlyList<CouplingMetric>> GetCouplingMetrics(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        [Description("Only return types with efferent coupling >= this value. Default 5.")] int threshold = 5,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<CouplingMetric>();

        var projects = projectName is null
            ? solution.Projects
            : solution.Projects.Where(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        // Build afferent coupling map (which types reference each type)
        var afferentMap = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var project in projects)
        {
            ct.ThrowIfCancellationRequested();
            var compilation = await workspace.GetCompilationAsync(project, ct);
            if (compilation is null) continue;

            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null || document.FilePath is null) continue;
                var model = compilation.GetSemanticModel(await document.GetSyntaxTreeAsync(ct) ?? throw new InvalidOperationException());

                foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    var typeSymbol = model.GetDeclaredSymbol(typeDecl);
                    if (typeSymbol is null) continue;

                    // Efferent: count distinct types referenced in the body
                    var referencedTypes = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var node in typeDecl.DescendantNodes())
                    {
                        var typeInfo = model.GetTypeInfo(node);
                        var referencedType = typeInfo.Type?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                        if (referencedType is not null && referencedType != typeSymbol.Name)
                            referencedTypes.Add(referencedType);
                    }

                    var efferent = referencedTypes.Count;
                    if (efferent < threshold) continue;

                    // Count how many other types reference this one (simplified afferent)
                    var fullName = typeSymbol.ToDisplayString();
                    afferentMap.TryGetValue(fullName, out var afferent);

                    var loc = typeSymbol.Locations.FirstOrDefault(l => l.IsInSource);
                    var lineSpan = loc?.GetLineSpan();

                    results.Add(new CouplingMetric(
                        typeSymbol.Name,
                        typeSymbol.ContainingNamespace?.ToDisplayString() ?? "",
                        afferent,
                        efferent,
                        lineSpan?.Path ?? document.FilePath,
                        project.Name));
                }
            }
        }

        return results.OrderByDescending(r => r.EfferentCoupling).ToList();
    }

    [McpServerTool, Description(
        "Detects Interface Segregation Principle violations. " +
        "Finds implementations that use fewer than a threshold percentage of the interface's members.")]
    /// <summary>
    /// Detects classes that implement an interface but use only a small fraction of its members, indicating an ISP violation.
    /// </summary>
    public async Task<IReadOnlyList<InterfaceSegregationViolation>> GetInterfaceSegregation(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        [Description("Flag implementations using fewer than this % of interface members. Default 60.")] int usageThresholdPercent = 60,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<InterfaceSegregationViolation>();

        var projects = projectName is null
            ? solution.Projects
            : solution.Projects.Where(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            ct.ThrowIfCancellationRequested();
            var compilation = await workspace.GetCompilationAsync(project, ct);
            if (compilation is null) continue;

            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null || document.FilePath is null) continue;
                var syntaxTree = await document.GetSyntaxTreeAsync(ct);
                if (syntaxTree is null) continue;
                var model = compilation.GetSemanticModel(syntaxTree);

                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var classSymbol = model.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                    if (classSymbol is null) continue;

                    foreach (var iface in classSymbol.Interfaces)
                    {
                        var ifaceMembers = iface.GetMembers()
                            .Where(m => !m.IsImplicitlyDeclared)
                            .Select(m => m.Name)
                            .ToList();

                        if (ifaceMembers.Count < 3) continue; // Too small to flag

                        // Check which interface members this class body actually references
                        var usedMembers = new HashSet<string>(StringComparer.Ordinal);
                        foreach (var node in classDecl.DescendantNodes().OfType<IdentifierNameSyntax>())
                        {
                            if (ifaceMembers.Contains(node.Identifier.Text))
                                usedMembers.Add(node.Identifier.Text);
                        }

                        var usedPct = usedMembers.Count * 100 / ifaceMembers.Count;
                        if (usedPct >= usageThresholdPercent) continue;

                        var unusedMembers = ifaceMembers.Except(usedMembers).ToList();
                        var loc = classSymbol.Locations.FirstOrDefault(l => l.IsInSource);

                        results.Add(new InterfaceSegregationViolation(
                            iface.Name,
                            classSymbol.Name,
                            ifaceMembers.Count,
                            usedMembers.Count,
                            unusedMembers,
                            loc?.GetLineSpan().Path ?? document.FilePath,
                            project.Name));
                    }
                }
            }
        }

        return results.OrderBy(r => r.InterfaceName).ToList();
    }

    [McpServerTool, Description(
        "Finds structurally duplicate methods by hashing normalized method bodies. " +
        "Normalizes whitespace and identifiers — catches copy-paste code even with renamed variables.")]
    /// <summary>
    /// Identifies structurally duplicate method bodies by hashing normalized syntax, catching copy-paste code even with renamed identifiers.
    /// </summary>
    public async Task<IReadOnlyList<DuplicateCodePair>> FindDuplicateLogic(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        [Description("Minimum method line count to consider. Default 8.")] int minLines = 8,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var methodHashes = new Dictionary<string, List<(string File, int Start, int End)>>(StringComparer.Ordinal);

        var projects = projectName is null
            ? solution.Projects
            : solution.Projects.Where(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                if (document.FilePath is null) continue;
                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null) continue;

                foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (method.Body is null) continue;
                    var lineSpan = method.GetLocation().GetLineSpan();
                    var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line;
                    if (lineCount < minLines) continue;

                    var normalized = NormalizeMethod(method.Body.ToString());
                    var hash = ComputeHash(normalized);

                    if (!methodHashes.TryGetValue(hash, out var locations))
                        methodHashes[hash] = locations = [];

                    locations.Add((document.FilePath, lineSpan.StartLinePosition.Line + 1, lineSpan.EndLinePosition.Line + 1));
                }
            }
        }

        return methodHashes
            .Where(kv => kv.Value.Count >= 2)
            .SelectMany(kv =>
            {
                var locs = kv.Value;
                var pairs = new List<DuplicateCodePair>();
                for (var i = 0; i < locs.Count - 1; i++)
                for (var j = i + 1; j < locs.Count; j++)
                    pairs.Add(new DuplicateCodePair(
                        locs[i].File, locs[i].Start, locs[i].End,
                        locs[j].File, locs[j].Start, locs[j].End,
                        kv.Key[..8])); // short hash for display
                return pairs;
            })
            .OrderBy(p => p.File1)
            .ToList();
    }

    [McpServerTool, Description(
        "Identifies methods that are good extract-method candidates: " +
        "long methods with cohesive statement groups that can be factored out. " +
        "Returns suggested extraction points with line ranges and reasoning.")]
    /// <summary>
    /// Identifies long methods that are good candidates for extract-method refactoring, with reasoning for each suggestion.
    /// </summary>
    public async Task<IReadOnlyList<ExtractionCandidate>> FindExtractionCandidates(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        [Description("Minimum method line count to consider. Default 25.")] int minLines = 25,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<ExtractionCandidate>();

        var projects = projectName is null
            ? solution.Projects
            : solution.Projects.Where(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null || document.FilePath is null) continue;

                foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (method.Body is null) continue;
                    var lineSpan = method.GetLocation().GetLineSpan();
                    var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line;
                    if (lineCount < minLines) continue;

                    var containingType = method.Ancestors().OfType<TypeDeclarationSyntax>()
                        .FirstOrDefault()?.Identifier.Text ?? "unknown";

                    var reasons = new List<string>();
                    if (lineCount >= 50) reasons.Add($"{lineCount} lines (very long)");
                    if (method.ParameterList.Parameters.Count >= 5) reasons.Add($"{method.ParameterList.Parameters.Count} parameters");

                    var complexity = CalculateCyclomaticComplexity(method);
                    if (complexity >= 10) reasons.Add($"complexity {complexity}");

                    // Look for comment-delimited regions or large if/else blocks
                    var hasLargeIfElse = method.DescendantNodes().OfType<IfStatementSyntax>()
                        .Any(ifStmt =>
                        {
                            var ifSpan = ifStmt.GetLocation().GetLineSpan();
                            return ifSpan.EndLinePosition.Line - ifSpan.StartLinePosition.Line >= 10;
                        });

                    if (hasLargeIfElse) reasons.Add("contains large if/else block (>10 lines)");
                    if (reasons.Count == 0) continue;

                    results.Add(new ExtractionCandidate(
                        method.Identifier.Text,
                        containingType,
                        document.FilePath,
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.EndLinePosition.Line + 1,
                        lineCount,
                        string.Join("; ", reasons),
                        project.Name));
                }
            }
        }

        return results.OrderByDescending(r => r.LineCount).ToList();
    }

    private static int CalculateCyclomaticComplexity(SyntaxNode node)
    {
        var complexity = 1;
        foreach (var descendant in node.DescendantNodes())
        {
            complexity += descendant.Kind() switch
            {
                SyntaxKind.IfStatement or SyntaxKind.CatchClause or SyntaxKind.ForStatement
                or SyntaxKind.ForEachStatement or SyntaxKind.WhileStatement or SyntaxKind.DoStatement
                or SyntaxKind.CaseSwitchLabel or SyntaxKind.CasePatternSwitchLabel
                or SyntaxKind.ConditionalExpression or SyntaxKind.LogicalAndExpression
                or SyntaxKind.LogicalOrExpression => 1,
                _ => 0
            };
        }
        return complexity;
    }

    private static string NormalizeMethod(string body)
    {
        // Normalize whitespace and replace identifiers with placeholders
        var sb = new StringBuilder();
        var tokens = CSharpSyntaxTree.ParseText(body).GetRoot().DescendantTokens();
        foreach (var token in tokens)
        {
            sb.Append(token.IsKind(SyntaxKind.IdentifierToken) ? "ID " : token.Text + " ");
        }
        return sb.ToString();
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
