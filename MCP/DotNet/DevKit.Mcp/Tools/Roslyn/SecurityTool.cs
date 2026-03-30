using System.ComponentModel;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

/// <summary>
/// Provides an MCP tool for scanning C# source for OWASP-aligned security vulnerabilities via Roslyn syntax analysis.
/// </summary>
[McpServerToolType]
public sealed class SecurityTool(RoslynWorkspaceService workspace, SolutionOptions options)
{
    [McpServerTool, Description(
        "Scans for OWASP-aligned security vulnerabilities: SQL injection via string concatenation, " +
        "hardcoded connection strings/secrets, path traversal, dangerous deserialization, " +
        "and user input passed to sensitive APIs. Returns severity: Critical, High, Medium.")]
    /// <summary>
    /// Scans source files for security vulnerabilities including SQL injection, hardcoded secrets, path traversal, and dangerous deserialization.
    /// </summary>
    public async Task<IReadOnlyList<SecurityVulnerability>> ScanSecurityVulnerabilities(
        [Description("Root directory to scan. Defaults to solution directory.")] string? rootPath = null,
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var root = rootPath
            ?? (options.SolutionPath is not null ? Path.GetDirectoryName(options.SolutionPath) : null)
            ?? Directory.GetCurrentDirectory();

        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<SecurityVulnerability>();

        var projects = projectName is null
            ? solution.Projects
            : solution.Projects.Where(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            ct.ThrowIfCancellationRequested();
            var compilation = await workspace.GetCompilationAsync(project, ct);

            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                if (document.FilePath is null) continue;

                var syntaxTree = await document.GetSyntaxTreeAsync(ct);
                if (syntaxTree is null) continue;

                var rootNode = await syntaxTree.GetRootAsync(ct);
                var model = compilation?.GetSemanticModel(syntaxTree);

                ScanSqlInjection(rootNode, document.FilePath, project.Name, results);
                ScanHardcodedConnStrings(rootNode, document.FilePath, project.Name, results);
                ScanPathTraversal(rootNode, document.FilePath, project.Name, results, model);
                ScanDangerousDeserialization(rootNode, document.FilePath, project.Name, results);
                ScanDangerousReflection(rootNode, document.FilePath, project.Name, results, model);
            }
        }

        return results
            .OrderBy(r => r.Severity == "Critical" ? 0 : r.Severity == "High" ? 1 : 2)
            .ThenBy(r => r.FilePath)
            .ToList();
    }

    private static void ScanSqlInjection(SyntaxNode root, string filePath, string projectName,
        List<SecurityVulnerability> results)
    {
        var sqlKeywords = new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "EXEC", "EXECUTE" };

        foreach (var addition in root.DescendantNodes().OfType<BinaryExpressionSyntax>()
                     .Where(b => b.IsKind(SyntaxKind.AddExpression)))
        {
            var text = addition.ToString();
            if (!sqlKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase))) continue;

            // Check if it mixes a string literal and a variable (injection risk)
            var hasLiteral = addition.DescendantNodes().OfType<LiteralExpressionSyntax>()
                .Any(l => l.IsKind(SyntaxKind.StringLiteralExpression));
            var hasVariable = addition.DescendantNodes().OfType<IdentifierNameSyntax>().Any();

            if (!hasLiteral || !hasVariable) continue;

            var line = addition.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            results.Add(new SecurityVulnerability(
                "SqlInjection",
                "SQL query built via string concatenation — vulnerable to injection",
                filePath, line,
                text[..Math.Min(120, text.Length)],
                "Critical", projectName));
        }

        // Also catch string interpolation with SQL
        foreach (var interpolation in root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
        {
            var text = interpolation.ToString();
            if (!sqlKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase))) continue;

            var line = interpolation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            results.Add(new SecurityVulnerability(
                "SqlInjection",
                "SQL query uses string interpolation — use parameterized queries via EF Core or Dapper",
                filePath, line,
                text[..Math.Min(120, text.Length)],
                "Critical", projectName));
        }
    }

    private static void ScanHardcodedConnStrings(SyntaxNode root, string filePath, string projectName,
        List<SecurityVulnerability> results)
    {
        var connStringKeywords = new[] { "Server=", "Data Source=", "Initial Catalog=", "Password=", "Pwd=" };

        foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>()
                     .Where(l => l.IsKind(SyntaxKind.StringLiteralExpression)))
        {
            var value = literal.Token.ValueText;
            if (value.Length < 10) continue;
            if (!connStringKeywords.Any(k => value.Contains(k, StringComparison.OrdinalIgnoreCase))) continue;

            var line = literal.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            results.Add(new SecurityVulnerability(
                "HardcodedConnectionString",
                "Hardcoded connection string detected — move to user secrets or environment variables",
                filePath, line,
                value[..Math.Min(60, value.Length)] + "...",
                "High", projectName));
        }
    }

    private static void ScanPathTraversal(SyntaxNode root, string filePath, string projectName,
        List<SecurityVulnerability> results, SemanticModel? model)
    {
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;
            var method = access.Name.Identifier.Text;
            var type = access.Expression.ToString();

            if (!((type == "Path" && method is "Combine" or "GetFullPath") ||
                  (type is "File" or "Directory" or "System.IO.File") && method is "Open" or "ReadAllText" or "WriteAllText" or "Delete"))
                continue;

            // Check if any argument comes from a method parameter (potential user input)
            var args = invocation.ArgumentList.Arguments;
            var hasExternalInput = args.Any(a =>
                a.Expression is IdentifierNameSyntax id &&
                !id.Identifier.Text.StartsWith("_") && // not a field
                char.IsLower(id.Identifier.Text[0]));  // likely a parameter

            if (!hasExternalInput) continue;

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            results.Add(new SecurityVulnerability(
                "PathTraversal",
                $"{type}.{method}() called with what appears to be user-supplied input — validate and sanitize the path",
                filePath, line,
                invocation.ToString()[..Math.Min(120, invocation.ToString().Length)],
                "High", projectName));
        }
    }

    private static void ScanDangerousDeserialization(SyntaxNode root, string filePath, string projectName,
        List<SecurityVulnerability> results)
    {
        // BinaryFormatter and XmlSerializer with TypeName handling
        var dangerousTypes = new[] { "BinaryFormatter", "NetDataContractSerializer", "LosFormatter" };

        foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (!dangerousTypes.Any(t => creation.Type.ToString().Contains(t))) continue;

            var line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            results.Add(new SecurityVulnerability(
                "InsecureDeserialization",
                $"{creation.Type} is vulnerable to deserialization attacks — use System.Text.Json instead",
                filePath, line,
                creation.ToString(),
                "Critical", projectName));
        }
    }

    private static void ScanDangerousReflection(SyntaxNode root, string filePath, string projectName,
        List<SecurityVulnerability> results, SemanticModel? model)
    {
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;
            if (access.Expression.ToString() != "Type" || access.Name.Identifier.Text != "GetType") continue;

            var args = invocation.ArgumentList.Arguments;
            if (args.Count == 0) continue;

            // Check if type name comes from a variable (potential user input)
            var firstArg = args[0].Expression;
            if (firstArg is LiteralExpressionSyntax) continue; // constant, safe

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            results.Add(new SecurityVulnerability(
                "DangerousReflection",
                "Type.GetType() called with a non-constant argument — may allow type confusion attacks",
                filePath, line,
                invocation.ToString(),
                "Medium", projectName));
        }
    }
}
