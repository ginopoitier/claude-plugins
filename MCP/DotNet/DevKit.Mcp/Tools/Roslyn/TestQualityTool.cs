using System.ComponentModel;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

[McpServerToolType]
public sealed class TestQualityTool(RoslynWorkspaceService workspace)
{
    [McpServerTool, Description(
        "Cross-references handler files with test files by naming convention. " +
        "Reports handlers with no matching test class. " +
        "Looks for *Handler.cs and expects *Tests.cs or *HandlerTests.cs alongside.")]
    public async Task<IReadOnlyList<TestGapItem>> GetTestGapReport(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<TestGapItem>();

        // Collect all test class names across the solution
        var testTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                if (document.FilePath is null) continue;
                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null) continue;

                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var hasFactAttr = classDecl.DescendantNodes().OfType<AttributeSyntax>()
                        .Any(a => a.Name.ToString() is "Fact" or "Theory" or "Test" or "TestClass");
                    var hasFactMethod = classDecl.Members.OfType<MethodDeclarationSyntax>()
                        .Any(m => m.AttributeLists.SelectMany(al => al.Attributes)
                            .Any(a => a.Name.ToString() is "Fact" or "Theory" or "Test"));

                    if (hasFactAttr || hasFactMethod)
                        testTypeNames.Add(classDecl.Identifier.Text);
                }
            }
        }

        var projects = projectName is null
            ? solution.Projects
            : solution.Projects.Where(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        foreach (var project in projects)
        {
            // Skip test projects themselves
            if (project.Name.Contains("Test", StringComparison.OrdinalIgnoreCase)) continue;

            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                if (document.FilePath is null) continue;

                var fileName = Path.GetFileNameWithoutExtension(document.FilePath);
                if (!fileName.EndsWith("Handler", StringComparison.OrdinalIgnoreCase)) continue;

                // Derive expected test names
                var expectedTests = new[]
                {
                    fileName + "Tests",
                    fileName.Replace("Handler", "Tests"),
                    fileName.Replace("Handler", "") + "Tests"
                };

                var matchingTest = expectedTests.FirstOrDefault(t => testTypeNames.Contains(t));
                var hasTests = matchingTest is not null;

                results.Add(new TestGapItem(
                    fileName,
                    document.FilePath,
                    hasTests,
                    matchingTest,
                    project.Name));
            }
        }

        return results.OrderBy(r => r.HasTests).ThenBy(r => r.ProjectName).ThenBy(r => r.HandlerName).ToList();
    }

    [McpServerTool, Description(
        "Finds test anti-patterns: Thread.Sleep in tests, mocked DbContext (should use real DB), " +
        "tests with no assertions, Assert.True(x == y) instead of Assert.Equal, and tests missing error-path coverage.")]
    public async Task<IReadOnlyList<TestSmellItem>> FindTestSmells(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<TestSmellItem>();

        var projects = projectName is null
            ? solution.Projects.Where(p => p.Name.Contains("Test", StringComparison.OrdinalIgnoreCase))
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
                    var isTest = method.AttributeLists.SelectMany(al => al.Attributes)
                        .Any(a => a.Name.ToString() is "Fact" or "Theory" or "Test");
                    if (!isTest) continue;

                    var methodText = method.Body?.ToString() ?? "";
                    var line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                    // Thread.Sleep in tests
                    if (methodText.Contains("Thread.Sleep"))
                        results.Add(new TestSmellItem("ThreadSleep",
                            "Thread.Sleep in test — use FakeTimeProvider or await Task.Delay with very short duration",
                            document.FilePath, line, project.Name));

                    // Mocked DbContext
                    if (methodText.Contains("Mock<") && (methodText.Contains("DbContext") || methodText.Contains("AppDbContext")))
                        results.Add(new TestSmellItem("MockedDbContext",
                            "Mocked DbContext detected — use a real test database via Testcontainers for accurate integration tests",
                            document.FilePath, line, project.Name));

                    // No assertions
                    var hasAssertion = methodText.Contains(".Should()") ||
                                       methodText.Contains("Assert.") ||
                                       methodText.Contains("Verify(") ||
                                       methodText.Contains("response.Status");
                    if (!hasAssertion)
                        results.Add(new TestSmellItem("NoAssertions",
                            $"Test '{method.Identifier.Text}' has no assertions — it will always pass and provides no value",
                            document.FilePath, line, project.Name));

                    // Assert.True(x == y) instead of Assert.Equal
                    foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        if (invocation.Expression.ToString() is not "Assert.True" and not "Assert.False") continue;
                        var argText = invocation.ArgumentList.Arguments.FirstOrDefault()?.ToString() ?? "";
                        if (!argText.Contains("==") && !argText.Contains("!=")) continue;

                        results.Add(new TestSmellItem("WeakAssertion",
                            $"Assert.True/False with equality check — use Assert.Equal/NotEqual for better failure messages",
                            document.FilePath,
                            invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            project.Name));
                    }
                }
            }
        }

        return results.OrderBy(r => r.SmellType).ThenBy(r => r.FilePath).ToList();
    }
}
