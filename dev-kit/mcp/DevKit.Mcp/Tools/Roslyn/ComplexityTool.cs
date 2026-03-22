using System.ComponentModel;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

[McpServerToolType]
public sealed class ComplexityTool(RoslynWorkspaceService workspace)
{
    [McpServerTool, Description(
        "Returns cyclomatic complexity per method across the solution or a project. " +
        "Counts branches: if, else if, case, catch, for/foreach/while, &&, ||, ?:, ??. " +
        "Flag threshold defaults to 10 — methods above this are good refactor candidates.")]
    public async Task<IReadOnlyList<ComplexityItem>> GetComplexityReport(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        [Description("Only return methods at or above this complexity. Default 10.")] int threshold = 10,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<ComplexityItem>();

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
                    var complexity = CalculateCyclomaticComplexity(method);
                    if (complexity < threshold) continue;

                    var lineSpan = method.GetLocation().GetLineSpan();
                    var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;
                    var containingType = method.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "unknown";

                    results.Add(new ComplexityItem(
                        method.Identifier.Text,
                        containingType,
                        document.FilePath,
                        lineSpan.StartLinePosition.Line + 1,
                        complexity,
                        method.ParameterList.Parameters.Count,
                        lineCount,
                        project.Name));
                }
            }
        }

        return results.OrderByDescending(r => r.CyclomaticComplexity).ToList();
    }

    [McpServerTool, Description(
        "Finds methods exceeding a line count or parameter count threshold. " +
        "Long methods are prime extract-method candidates.")]
    public async Task<IReadOnlyList<LongMethodItem>> FindLongMethods(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        [Description("Flag methods with at least this many lines. Default 30.")] int lineThreshold = 30,
        [Description("Flag methods with at least this many parameters. Default 5.")] int paramThreshold = 5,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<LongMethodItem>();

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
                    var lineSpan = method.GetLocation().GetLineSpan();
                    var lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;
                    var paramCount = method.ParameterList.Parameters.Count;

                    if (lineCount < lineThreshold && paramCount < paramThreshold) continue;

                    var containingType = method.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "unknown";

                    results.Add(new LongMethodItem(
                        method.Identifier.Text,
                        containingType,
                        document.FilePath,
                        lineSpan.StartLinePosition.Line + 1,
                        lineCount,
                        paramCount,
                        project.Name));
                }
            }
        }

        return results.OrderByDescending(r => r.LineCount).ToList();
    }

    private static int CalculateCyclomaticComplexity(SyntaxNode node)
    {
        // Start at 1 (one path through the method)
        var complexity = 1;

        foreach (var descendant in node.DescendantNodes())
        {
            complexity += descendant.Kind() switch
            {
                SyntaxKind.IfStatement => 1,
                SyntaxKind.ElseClause when descendant.ChildNodes().Any(n => n is IfStatementSyntax) => 1,
                SyntaxKind.CaseSwitchLabel => 1,
                SyntaxKind.CasePatternSwitchLabel => 1,
                SyntaxKind.WhenClause => 1,
                SyntaxKind.CatchClause => 1,
                SyntaxKind.ForStatement => 1,
                SyntaxKind.ForEachStatement => 1,
                SyntaxKind.WhileStatement => 1,
                SyntaxKind.DoStatement => 1,
                SyntaxKind.ConditionalExpression => 1,
                SyntaxKind.CoalesceExpression => 1,
                SyntaxKind.LogicalAndExpression => 1,
                SyntaxKind.LogicalOrExpression => 1,
                SyntaxKind.SwitchExpressionArm => 1,
                _ => 0
            };
        }

        return complexity;
    }
}
