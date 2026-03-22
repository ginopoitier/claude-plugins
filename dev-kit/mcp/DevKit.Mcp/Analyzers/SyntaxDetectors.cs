using DevKit.Mcp.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DevKit.Mcp.Analyzers;

// ── AsyncVoid ─────────────────────────────────────────────────────────────────

public sealed class AsyncVoidDetector : IAntiPatternDetector
{
    public string PatternId => "AsyncVoid";
    public DetectorMode Mode => DetectorMode.Syntax;

    public IEnumerable<AntiPatternMatch> Analyze(SyntaxTree tree, string filePath, string projectName)
    {
        var root = tree.GetRoot();
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var isAsync = method.Modifiers.Any(SyntaxKind.AsyncKeyword);
            var isVoid = method.ReturnType.ToString() == "void";
            if (!isAsync || !isVoid) continue;

            // Allow event handlers: (object sender, EventArgs e)
            var parameters = method.ParameterList.Parameters;
            var isEventHandler = parameters.Count == 2 &&
                parameters[1].Type?.ToString().Contains("EventArgs") == true;
            if (isEventHandler) continue;

            var line = tree.GetLineSpan(method.Span).StartLinePosition.Line + 1;
            yield return new AntiPatternMatch(
                PatternId,
                "async void method detected",
                "Change return type to async Task — async void swallows exceptions and cannot be awaited",
                filePath, line,
                method.Identifier.Text + " (async void)",
                projectName);
        }
    }
}

// ── SyncOverAsync ─────────────────────────────────────────────────────────────

public sealed class SyncOverAsyncDetector : IAntiPatternDetector
{
    public string PatternId => "SyncOverAsync";
    public DetectorMode Mode => DetectorMode.Syntax;

    private static readonly HashSet<string> BlockingMembers =
        new(StringComparer.Ordinal) { "Result", "Wait", "GetAwaiter" };

    public IEnumerable<AntiPatternMatch> Analyze(SyntaxTree tree, string filePath, string projectName)
    {
        var root = tree.GetRoot();
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            var name = access.Name.Identifier.Text;
            if (!BlockingMembers.Contains(name)) continue;

            // GetAwaiter must be followed by .GetResult()
            if (name == "GetAwaiter")
            {
                var parent = access.Parent;
                if (parent is not InvocationExpressionSyntax inv) continue;
                var grandParent = inv.Parent;
                if (grandParent is not MemberAccessExpressionSyntax nextAccess) continue;
                if (nextAccess.Name.Identifier.Text != "GetResult") continue;
            }

            var line = tree.GetLineSpan(access.Span).StartLinePosition.Line + 1;
            yield return new AntiPatternMatch(
                PatternId,
                $".{name}() blocks the calling thread",
                "Use await instead — blocking on async code can cause deadlocks in ASP.NET contexts",
                filePath, line,
                access.ToString(),
                projectName, "Error");
        }
    }
}

// ── InlineHttpClient ──────────────────────────────────────────────────────────

public sealed class InlineHttpClientDetector : IAntiPatternDetector
{
    public string PatternId => "InlineHttpClient";
    public DetectorMode Mode => DetectorMode.Syntax;

    public IEnumerable<AntiPatternMatch> Analyze(SyntaxTree tree, string filePath, string projectName)
    {
        var root = tree.GetRoot();
        foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (creation.Type.ToString() != "HttpClient") continue;

            var line = tree.GetLineSpan(creation.Span).StartLinePosition.Line + 1;
            yield return new AntiPatternMatch(
                PatternId,
                "new HttpClient() bypasses connection pooling",
                "Inject IHttpClientFactory and call CreateClient() — avoids socket exhaustion",
                filePath, line,
                "new HttpClient()",
                projectName);
        }
    }
}

// ── DateTimeNow ───────────────────────────────────────────────────────────────

public sealed class DateTimeNowDetector : IAntiPatternDetector
{
    public string PatternId => "DateTimeNow";
    public DetectorMode Mode => DetectorMode.Syntax;

    public IEnumerable<AntiPatternMatch> Analyze(SyntaxTree tree, string filePath, string projectName)
    {
        var root = tree.GetRoot();
        foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            var name = access.Name.Identifier.Text;
            if (name is not ("Now" or "UtcNow")) continue;
            if (access.Expression.ToString() != "DateTime") continue;

            var line = tree.GetLineSpan(access.Span).StartLinePosition.Line + 1;
            var suggestion = name == "Now"
                ? "Inject TimeProvider and use GetLocalNow() — makes time testable"
                : "Inject TimeProvider and use GetUtcNow() — makes time testable";

            yield return new AntiPatternMatch(
                PatternId,
                $"DateTime.{name} is not testable",
                suggestion,
                filePath, line,
                $"DateTime.{name}",
                projectName);
        }
    }
}

// ── EmptyCatch ────────────────────────────────────────────────────────────────

public sealed class EmptyCatchDetector : IAntiPatternDetector
{
    public string PatternId => "EmptyCatch";
    public DetectorMode Mode => DetectorMode.Syntax;

    public IEnumerable<AntiPatternMatch> Analyze(SyntaxTree tree, string filePath, string projectName)
    {
        var root = tree.GetRoot();
        foreach (var catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            var block = catchClause.Block;
            var hasStatements = block.Statements.Count > 0;
            if (hasStatements) continue;

            var line = tree.GetLineSpan(catchClause.Span).StartLinePosition.Line + 1;
            yield return new AntiPatternMatch(
                PatternId,
                "Empty catch block silently swallows exceptions",
                "Log the exception at minimum, or rethrow if you cannot handle it",
                filePath, line,
                "catch { }",
                projectName, "Error");
        }
    }
}

// ── ThreadSleep ───────────────────────────────────────────────────────────────

public sealed class ThreadSleepDetector : IAntiPatternDetector
{
    public string PatternId => "ThreadSleep";
    public DetectorMode Mode => DetectorMode.Syntax;

    public IEnumerable<AntiPatternMatch> Analyze(SyntaxTree tree, string filePath, string projectName)
    {
        var root = tree.GetRoot();
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;
            if (access.Expression.ToString() != "Thread" || access.Name.Identifier.Text != "Sleep") continue;

            var line = tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;
            yield return new AntiPatternMatch(
                PatternId,
                "Thread.Sleep blocks the thread",
                "Use await Task.Delay() instead to keep the thread pool free",
                filePath, line,
                invocation.ToString(),
                projectName);
        }
    }
}

// ── ConsoleWriteLine ──────────────────────────────────────────────────────────

public sealed class ConsoleWriteLineDetector : IAntiPatternDetector
{
    public string PatternId => "ConsoleWriteLine";
    public DetectorMode Mode => DetectorMode.Syntax;

    public IEnumerable<AntiPatternMatch> Analyze(SyntaxTree tree, string filePath, string projectName)
    {
        // Skip test projects
        if (filePath.Contains("Tests", StringComparison.OrdinalIgnoreCase)) yield break;

        var root = tree.GetRoot();
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;
            if (access.Expression.ToString() != "Console") continue;
            if (access.Name.Identifier.Text is not ("Write" or "WriteLine")) continue;

            var line = tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1;
            yield return new AntiPatternMatch(
                PatternId,
                "Console.WriteLine in production code",
                "Use ILogger<T> with structured properties instead",
                filePath, line,
                invocation.ToString()[..Math.Min(80, invocation.ToString().Length)],
                projectName);
        }
    }
}

// ── HardcodedSecret ───────────────────────────────────────────────────────────

public sealed class HardcodedSecretDetector : IAntiPatternDetector
{
    public string PatternId => "HardcodedSecret";
    public DetectorMode Mode => DetectorMode.Syntax;

    private static readonly string[] SecretKeywords =
        ["password", "secret", "apikey", "api_key", "token", "connectionstring", "pwd"];

    public IEnumerable<AntiPatternMatch> Analyze(SyntaxTree tree, string filePath, string projectName)
    {
        // Skip test helpers and migrations
        if (filePath.Contains("Migration", StringComparison.OrdinalIgnoreCase)) yield break;

        var root = tree.GetRoot();
        foreach (var assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (assignment.Right is not LiteralExpressionSyntax literal) continue;
            if (!literal.IsKind(SyntaxKind.StringLiteralExpression)) continue;

            var value = literal.Token.ValueText;
            if (value.Length < 6) continue; // too short to be a real secret

            var left = assignment.Left.ToString().ToLowerInvariant();
            if (!SecretKeywords.Any(k => left.Contains(k))) continue;

            var line = tree.GetLineSpan(assignment.Span).StartLinePosition.Line + 1;
            yield return new AntiPatternMatch(
                PatternId,
                "Possible hardcoded secret detected",
                "Move to user secrets, environment variables, or Azure Key Vault",
                filePath, line,
                $"{assignment.Left} = \"***\"",
                projectName, "Error");
        }
    }
}

// ── PragmaWithoutRestore ──────────────────────────────────────────────────────

public sealed class PragmaWithoutRestoreDetector : IAntiPatternDetector
{
    public string PatternId => "PragmaWithoutRestore";
    public DetectorMode Mode => DetectorMode.Syntax;

    public IEnumerable<AntiPatternMatch> Analyze(SyntaxTree tree, string filePath, string projectName)
    {
        var root = tree.GetRoot();
        var pragmas = root.DescendantTrivia()
            .Where(t => t.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia))
            .ToList();

        var disabledWarnings = new HashSet<string>();
        foreach (var pragma in pragmas)
        {
            var directive = (PragmaWarningDirectiveTriviaSyntax)pragma.GetStructure()!;
            var isDisable = directive.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword);
            var warnings = directive.ErrorCodes.Select(e => e.ToString()).ToList();

            if (isDisable)
            {
                foreach (var w in warnings) disabledWarnings.Add(w);
            }
            else
            {
                foreach (var w in warnings) disabledWarnings.Remove(w);
            }
        }

        foreach (var warning in disabledWarnings)
        {
            // Find the disable pragma for this warning to get its line
            var disablePragma = pragmas.FirstOrDefault(p =>
            {
                var d = (PragmaWarningDirectiveTriviaSyntax)p.GetStructure()!;
                return d.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword) &&
                       d.ErrorCodes.Any(e => e.ToString() == warning);
            });

            if (disablePragma == default) continue;
            var line = tree.GetLineSpan(disablePragma.Span).StartLinePosition.Line + 1;

            yield return new AntiPatternMatch(
                PatternId,
                $"#pragma warning disable {warning} has no matching restore",
                "Add #pragma warning restore after the suppressed region, or fix the underlying warning",
                filePath, line,
                $"#pragma warning disable {warning}",
                projectName);
        }
    }
}
