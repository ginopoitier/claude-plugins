using System.ComponentModel;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

/// <summary>
/// Provides MCP tools for detecting EF Core performance anti-patterns such as N+1 queries and missing AsNoTracking calls.
/// </summary>
[McpServerToolType]
public sealed class PerformanceTool(RoslynWorkspaceService workspace)
{
    [McpServerTool, Description(
        "Detects potential N+1 query patterns: EF Core calls inside loops, " +
        ".Include() inside foreach, navigation property access inside Select lambdas, " +
        "and FirstOrDefault/Find called repeatedly in loop bodies.")]
    /// <summary>
    /// Finds potential N+1 query patterns by detecting EF Core calls made inside iteration loops or LINQ lambdas.
    /// </summary>
    public async Task<IReadOnlyList<N1PatternItem>> FindN1Patterns(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<N1PatternItem>();

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

                // Pattern 1: EF Core query inside foreach/for loop
                foreach (var loop in root.DescendantNodes()
                             .Where(n => n is ForEachStatementSyntax or ForStatementSyntax or WhileStatementSyntax))
                {
                    var loopBody = loop.ChildNodes().LastOrDefault(); // body block
                    if (loopBody is null) continue;

                    foreach (var invocation in loopBody.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;
                        var method = access.Name.Identifier.Text;

                        if (method is "FirstOrDefault" or "SingleOrDefault" or "Find" or "FindAsync"
                            or "Where" or "Include" or "ToList" or "ToListAsync" or "FirstOrDefaultAsync")
                        {
                            var snippet = invocation.ToString()[..Math.Min(100, invocation.ToString().Length)];
                            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                            results.Add(new N1PatternItem(
                                $"Potential N+1: .{method}() called inside a loop — consider loading all data before the loop",
                                document.FilePath, line, snippet, project.Name));
                        }
                    }
                }

                // Pattern 2: .Include() inside a LINQ .Select() lambda
                foreach (var selectInvocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (selectInvocation.Expression is not MemberAccessExpressionSyntax selectAccess) continue;
                    if (selectAccess.Name.Identifier.Text != "Select") continue;

                    var lambdaArg = selectInvocation.ArgumentList.Arguments
                        .Select(a => a.Expression)
                        .OfType<LambdaExpressionSyntax>()
                        .FirstOrDefault();

                    if (lambdaArg is null) continue;

                    foreach (var inner in lambdaArg.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        if (inner.Expression is not MemberAccessExpressionSyntax innerAccess) continue;
                        if (innerAccess.Name.Identifier.Text != "Include") continue;

                        var line = inner.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        results.Add(new N1PatternItem(
                            ".Include() inside a .Select() lambda — use a single Include before Select, or project to DTO",
                            document.FilePath, line,
                            inner.ToString()[..Math.Min(100, inner.ToString().Length)],
                            project.Name));
                    }
                }
            }
        }

        return results.OrderBy(r => r.FilePath).ThenBy(r => r.Line).ToList();
    }

    [McpServerTool, Description(
        "Finds EF Core queries in read (query) handlers missing .AsNoTracking(). " +
        "Targets files in Queries/ folders or named *Query*Handler. " +
        "AsNoTracking() can improve performance by 20-40% for read-only queries.")]
    /// <summary>
    /// Finds EF Core database queries in query handlers that are missing an <c>AsNoTracking()</c> call.
    /// </summary>
    public async Task<IReadOnlyList<MissingNoTrackingItem>> FindMissingAsNoTracking(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<MissingNoTrackingItem>();

        var projects = projectName is null
            ? solution.Projects
            : solution.Projects.Where(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                if (document.FilePath is null) continue;

                // Only analyze query handler files
                var fileName = Path.GetFileNameWithoutExtension(document.FilePath);
                var isQueryHandler =
                    (fileName.Contains("Query", StringComparison.OrdinalIgnoreCase) &&
                     fileName.Contains("Handler", StringComparison.OrdinalIgnoreCase)) ||
                    document.FilePath.Contains(Path.DirectorySeparatorChar + "Queries" + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase);

                if (!isQueryHandler) continue;

                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null) continue;

                foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;
                    var method = access.Name.Identifier.Text;

                    if (method is not ("Where" or "Select" or "FirstOrDefault" or "FirstOrDefaultAsync"
                        or "ToList" or "ToListAsync" or "SingleOrDefault" or "SingleOrDefaultAsync"))
                        continue;

                    // Walk up the chain to check for AsNoTracking
                    var chain = BuildMethodChain(invocation);
                    if (chain.Contains("AsNoTracking")) continue;

                    // Check if this looks like a db access (chain contains a DbSet-looking access)
                    var chainText = string.Join(".", chain);
                    if (!chainText.Contains("db") && !chainText.Contains("Db") && !chainText.Contains("context")) continue;

                    var snippet = BuildChainSnippet(invocation);
                    var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                    results.Add(new MissingNoTrackingItem(
                        fileName, document.FilePath, line, snippet, project.Name));
                    break; // One report per handler is enough
                }
            }
        }

        return results.OrderBy(r => r.FilePath).ToList();
    }

    private static HashSet<string> BuildMethodChain(InvocationExpressionSyntax invocation)
    {
        var methods = new HashSet<string>(StringComparer.Ordinal);
        SyntaxNode? current = invocation;

        while (current is not null)
        {
            if (current is InvocationExpressionSyntax inv &&
                inv.Expression is MemberAccessExpressionSyntax ma)
            {
                methods.Add(ma.Name.Identifier.Text);
                current = inv.Expression;
            }
            else if (current is MemberAccessExpressionSyntax memberAccess)
            {
                methods.Add(memberAccess.Name.Identifier.Text);
                current = memberAccess.Expression;
            }
            else break;
        }

        return methods;
    }

    private static string BuildChainSnippet(InvocationExpressionSyntax invocation)
    {
        // Walk up to find the root of the chain
        SyntaxNode? current = invocation;
        while (current?.Parent is InvocationExpressionSyntax or MemberAccessExpressionSyntax)
            current = current.Parent;

        var text = current?.ToString() ?? invocation.ToString();
        return text[..Math.Min(120, text.Length)];
    }
}
