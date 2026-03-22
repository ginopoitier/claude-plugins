using DevKit.Mcp.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DevKit.Mcp.Analyzers;

// ── MissingCancellationToken ──────────────────────────────────────────────────

public sealed class MissingCancellationTokenDetector : IAntiPatternDetector
{
    public string PatternId => "MissingCancellationToken";
    public DetectorMode Mode => DetectorMode.Semantic;

    public async Task<IEnumerable<AntiPatternMatch>> AnalyzeSemanticAsync(
        SemanticModel model, SyntaxTree tree, string filePath, string projectName, CancellationToken ct)
    {
        await Task.CompletedTask;
        var results = new List<AntiPatternMatch>();
        var root = tree.GetRoot(ct);

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            // Only check async methods or methods returning Task/ValueTask
            var returnType = method.ReturnType.ToString();
            var isAsync = method.Modifiers.Any(SyntaxKind.AsyncKeyword) ||
                          returnType.StartsWith("Task") || returnType.StartsWith("ValueTask");
            if (!isAsync) continue;

            // Skip if already has CancellationToken parameter
            var hasCt = method.ParameterList.Parameters
                .Any(p => p.Type?.ToString().Contains("CancellationToken") == true);
            if (hasCt) continue;

            // Skip interface methods and abstract methods (they get it from implementations)
            var symbol = model.GetDeclaredSymbol(method);
            if (symbol is null) continue;
            if (symbol.IsAbstract) continue;
            if (symbol.ContainingType.TypeKind == TypeKind.Interface) continue;

            // Skip overrides — they must match the base signature
            if (symbol.IsOverride) continue;

            var line = tree.GetLineSpan(method.Span).StartLinePosition.Line + 1;
            results.Add(new AntiPatternMatch(
                PatternId,
                $"Async method '{method.Identifier.Text}' is missing a CancellationToken parameter",
                "Add CancellationToken ct = default as the last parameter and pass it to all async calls",
                filePath, line,
                method.Identifier.Text + "(...)",
                projectName));
        }

        return results;
    }
}

// ── EfCoreNoTracking ──────────────────────────────────────────────────────────

public sealed class EfCoreNoTrackingDetector : IAntiPatternDetector
{
    public string PatternId => "EfCoreNoTracking";
    public DetectorMode Mode => DetectorMode.Semantic;

    public async Task<IEnumerable<AntiPatternMatch>> AnalyzeSemanticAsync(
        SemanticModel model, SyntaxTree tree, string filePath, string projectName, CancellationToken ct)
    {
        await Task.CompletedTask;

        // Only check query handlers (read-only handlers, no SaveChanges)
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var isQueryHandler = fileName.Contains("Query", StringComparison.OrdinalIgnoreCase) &&
                             fileName.Contains("Handler", StringComparison.OrdinalIgnoreCase);
        if (!isQueryHandler) return [];

        var results = new List<AntiPatternMatch>();
        var root = tree.GetRoot(ct);

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;

            var methodName = access.Name.Identifier.Text;
            if (methodName is not ("Where" or "FirstOrDefault" or "SingleOrDefault" or "ToList" or "ToListAsync"))
                continue;

            // Check if there's an AsNoTracking in the invocation chain
            var chain = invocation.ToString();
            if (chain.Contains("AsNoTracking")) continue;

            // Only flag db set accesses (expression contains db field access)
            var typeInfo = model.GetTypeInfo(access.Expression);
            var typeName = typeInfo.Type?.Name ?? "";
            if (!typeName.Contains("DbSet") && !typeName.Contains("IQueryable")) continue;

            var line = tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;
            results.Add(new AntiPatternMatch(
                PatternId,
                "EF Core query in a read handler is missing AsNoTracking()",
                "Add .AsNoTracking() before .Where()/.Select() — prevents unnecessary change tracking overhead",
                filePath, line,
                chain[..Math.Min(100, chain.Length)],
                projectName));
        }

        return results;
    }
}

// ── LoggingInterpolation ──────────────────────────────────────────────────────

public sealed class LoggingInterpolationDetector : IAntiPatternDetector
{
    public string PatternId => "LoggingInterpolation";
    public DetectorMode Mode => DetectorMode.Semantic;

    private static readonly HashSet<string> LogMethods = new(StringComparer.OrdinalIgnoreCase)
        { "LogInformation", "LogWarning", "LogError", "LogDebug", "LogCritical", "LogTrace" };

    public async Task<IEnumerable<AntiPatternMatch>> AnalyzeSemanticAsync(
        SemanticModel model, SyntaxTree tree, string filePath, string projectName, CancellationToken ct)
    {
        await Task.CompletedTask;
        var results = new List<AntiPatternMatch>();
        var root = tree.GetRoot(ct);

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;
            if (!LogMethods.Contains(access.Name.Identifier.Text)) continue;

            var args = invocation.ArgumentList.Arguments;
            if (args.Count == 0) continue;

            // First arg should be the message template — check if it's string interpolation
            var firstArg = args[0].Expression;
            if (firstArg is not InterpolatedStringExpressionSyntax) continue;

            var line = tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;
            results.Add(new AntiPatternMatch(
                PatternId,
                $"{access.Name.Identifier.Text}() uses string interpolation",
                "Use message templates with named properties: Log.Info(\"Order {OrderId} placed\", orderId) — enables structured logging in Seq",
                filePath, line,
                invocation.ToString()[..Math.Min(100, invocation.ToString().Length)],
                projectName));
        }

        return results;
    }
}
