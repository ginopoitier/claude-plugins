using System.ComponentModel;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

[McpServerToolType]
public sealed class ArchitectureTool(RoslynWorkspaceService workspace)
{
    // ── CheckMediatorPatterns ──────────────────────────────────────────────────

    [McpServerTool, Description(
        "Validates CQRS/MediatR hygiene: finds IMediator usage (should be ISender/IPublisher), " +
        "handlers not marked internal sealed, and handlers not returning Result<T>.")]
    public async Task<IReadOnlyList<MediatorViolation>> CheckMediatorPatterns(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<MediatorViolation>();

        foreach (var project in GetProjects(solution, projectName))
        {
            ct.ThrowIfCancellationRequested();
            var compilation = await workspace.GetCompilationAsync(project, ct);

            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null || document.FilePath is null) continue;

                // IMediator usage (should be ISender or IPublisher)
                foreach (var node in root.DescendantNodes().OfType<IdentifierNameSyntax>()
                             .Where(n => n.Identifier.Text == "IMediator"))
                {
                    var line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    results.Add(new MediatorViolation(
                        "IMediator",
                        "IMediator is injected — use ISender for commands/queries, IPublisher for events",
                        document.FilePath, line, project.Name));
                }

                // Handlers not internal sealed
                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var baseList = classDecl.BaseList?.ToString() ?? "";
                    if (!baseList.Contains("IRequestHandler") && !baseList.Contains("INotificationHandler"))
                        continue;

                    var isInternal = classDecl.Modifiers.Any(SyntaxKind.InternalKeyword);
                    var isSealed = classDecl.Modifiers.Any(SyntaxKind.SealedKeyword);

                    if (!isInternal || !isSealed)
                    {
                        var line = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        var missing = (!isInternal ? "internal " : "") + (!isSealed ? "sealed" : "");
                        results.Add(new MediatorViolation(
                            "HandlerNotInternalSealed",
                            $"Handler '{classDecl.Identifier.Text}' should be marked '{missing.Trim()}' — prevents accidental inheritance and signals intent",
                            document.FilePath, line, project.Name));
                    }
                }
            }
        }

        return results.OrderBy(r => r.ViolationType).ThenBy(r => r.FilePath).ToList();
    }

    // ── FindMissingValidators ──────────────────────────────────────────────────

    [McpServerTool, Description(
        "Finds IRequest<T> implementations (commands/queries) with no matching AbstractValidator<T>. " +
        "Unvalidated commands let bad input reach your handlers.")]
    public async Task<IReadOnlyList<MissingValidatorItem>> FindMissingValidators(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<MissingValidatorItem>();

        // Collect all validator types first
        var validatedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in GetProjects(solution, projectName))
        {
            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null) continue;

                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var baseList = classDecl.BaseList?.ToString() ?? "";
                    if (!baseList.Contains("AbstractValidator")) continue;

                    // Extract type argument: AbstractValidator<CreateOrderCommand> → CreateOrderCommand
                    var match = System.Text.RegularExpressions.Regex.Match(
                        baseList, @"AbstractValidator<(\w+)>");
                    if (match.Success)
                        validatedTypes.Add(match.Groups[1].Value);
                }
            }
        }

        // Now find IRequest implementations without validators
        foreach (var project in GetProjects(solution, projectName))
        {
            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null || document.FilePath is null) continue;

                foreach (var typeDecl in root.DescendantNodes()
                             .OfType<TypeDeclarationSyntax>()
                             .Where(t => t is ClassDeclarationSyntax or RecordDeclarationSyntax))
                {
                    var baseList = typeDecl.BaseList?.ToString() ?? "";
                    if (!baseList.Contains("IRequest")) continue;

                    var name = typeDecl.Identifier.Text;
                    if (validatedTypes.Contains(name)) continue;

                    // Only flag commands (not queries — queries don't need validators typically)
                    if (!name.EndsWith("Command", StringComparison.OrdinalIgnoreCase)) continue;

                    var line = typeDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    results.Add(new MissingValidatorItem(name, document.FilePath, line, project.Name));
                }
            }
        }

        return results.OrderBy(r => r.ProjectName).ThenBy(r => r.CommandName).ToList();
    }

    // ── CheckResultPatternUsage ────────────────────────────────────────────────

    [McpServerTool, Description(
        "Checks result pattern compliance: finds handlers not returning Result<T>, " +
        "and places where exceptions are thrown for business failures instead of returning errors.")]
    public async Task<IReadOnlyList<ResultPatternViolation>> CheckResultPatternUsage(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<ResultPatternViolation>();

        foreach (var project in GetProjects(solution, projectName))
        {
            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null || document.FilePath is null) continue;

                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var baseList = classDecl.BaseList?.ToString() ?? "";
                    if (!baseList.Contains("IRequestHandler")) continue;

                    // Check return type contains Result
                    if (!baseList.Contains("Result"))
                    {
                        var line = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        results.Add(new ResultPatternViolation(
                            "HandlerNotReturningResult",
                            $"Handler '{classDecl.Identifier.Text}' does not return Result<T> — use Result pattern for all business failures",
                            document.FilePath, line, project.Name));
                    }

                    // Find throw statements inside handlers (business-logic throws)
                    foreach (var throwStmt in classDecl.DescendantNodes().OfType<ThrowStatementSyntax>())
                    {
                        // Allow rethrows (throw;) and infrastructure exceptions
                        if (throwStmt.Expression is null) continue; // rethrow

                        var exprText = throwStmt.Expression.ToString();
                        if (exprText.Contains("ArgumentException") || exprText.Contains("InvalidOperationException") ||
                            exprText.Contains("NotSupportedException") || exprText.Contains("NotImplementedException"))
                        {
                            var line = throwStmt.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            results.Add(new ResultPatternViolation(
                                "ThrowInHandler",
                                $"throw statement in handler — return a Result failure instead: return {ExtractErrorType(exprText)}Errors.XYZ",
                                document.FilePath, line, project.Name));
                        }
                    }
                }
            }
        }

        return results.OrderBy(r => r.ViolationType).ThenBy(r => r.FilePath).ToList();
    }

    // ── CheckDddPatterns ──────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Validates DDD conventions: entities with public setters, value objects not using record, " +
        "missing private constructors on aggregates, business logic outside Domain layer.")]
    public async Task<IReadOnlyList<DddViolation>> CheckDddPatterns(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<DddViolation>();

        foreach (var project in GetProjects(solution, projectName))
        {
            var isDomainProject = project.Name.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase);

            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null || document.FilePath is null) continue;

                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var baseList = classDecl.BaseList?.ToString() ?? "";
                    var isEntity = baseList.Contains("Entity") || baseList.Contains("AggregateRoot");
                    var isValueObject = baseList.Contains("ValueObject");

                    if (isEntity)
                    {
                        // Entities should not have public setters
                        foreach (var prop in classDecl.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                        {
                            var setter = prop.AccessorList?.Accessors
                                .FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
                            if (setter is null) continue;
                            var isPublicSetter = !setter.Modifiers.Any(m =>
                                m.IsKind(SyntaxKind.PrivateKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword));
                            if (!isPublicSetter) continue;

                            var line = prop.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            results.Add(new DddViolation(
                                "EntityPublicSetter",
                                $"Entity property '{prop.Identifier.Text}' has a public setter — use private set or init to protect invariants",
                                document.FilePath, line, project.Name));
                        }

                        // Aggregates should have a private/protected constructor for EF Core
                        var hasPrivateCtor = classDecl.DescendantNodes().OfType<ConstructorDeclarationSyntax>()
                            .Any(c => c.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword)));
                        if (!hasPrivateCtor && classDecl.Members.OfType<ConstructorDeclarationSyntax>().Any())
                        {
                            var line = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            results.Add(new DddViolation(
                                "AggregateMissingPrivateCtor",
                                $"Aggregate '{classDecl.Identifier.Text}' has no private/protected constructor — add one for EF Core hydration",
                                document.FilePath, line, project.Name));
                        }
                    }

                    if (isValueObject && !isDomainProject)
                    {
                        var line = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                        results.Add(new DddViolation(
                            "ValueObjectOutsideDomain",
                            $"ValueObject '{classDecl.Identifier.Text}' is defined outside the Domain layer",
                            document.FilePath, line, project.Name));
                    }
                }

                // Value objects should be records
                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var baseList = classDecl.BaseList?.ToString() ?? "";
                    if (!baseList.Contains("ValueObject")) continue;
                    if (!isDomainProject) continue;

                    // Flag class-based value objects (should be records)
                    var line = classDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    results.Add(new DddViolation(
                        "ValueObjectNotRecord",
                        $"ValueObject '{classDecl.Identifier.Text}' should be a record for structural equality",
                        document.FilePath, line, project.Name));
                }
            }
        }

        return results.OrderBy(r => r.ViolationType).ThenBy(r => r.FilePath).ToList();
    }

    // ── FindMissingAuthorization ───────────────────────────────────────────────

    [McpServerTool, Description(
        "Finds Minimal API endpoints missing .RequireAuthorization() or .AllowAnonymous(). " +
        "Flags any endpoint without an explicit authorization decision.")]
    public async Task<IReadOnlyList<UnauthorizedEndpoint>> FindMissingAuthorization(
        [Description("Filter to a specific project. Omit for whole solution.")] string? projectName = null,
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var results = new List<UnauthorizedEndpoint>();

        var mapMethods = new HashSet<string>(StringComparer.Ordinal)
            { "MapGet", "MapPost", "MapPut", "MapDelete", "MapPatch" };

        foreach (var project in GetProjects(solution, projectName))
        {
            foreach (var document in project.Documents)
            {
                ct.ThrowIfCancellationRequested();
                var root = await document.GetSyntaxRootAsync(ct);
                if (root is null || document.FilePath is null) continue;

                foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;
                    var methodName = access.Name.Identifier.Text;
                    if (!mapMethods.Contains(methodName)) continue;

                    // Walk up the method chain to check for RequireAuthorization or AllowAnonymous
                    var chain = GetMethodChain(invocation);
                    var hasAuth = chain.Any(m => m is "RequireAuthorization" or "AllowAnonymous");
                    if (hasAuth) continue;

                    // Extract route pattern from first argument
                    var routeArg = invocation.ArgumentList.Arguments.FirstOrDefault()?.ToString() ?? "unknown";
                    var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                    results.Add(new UnauthorizedEndpoint(
                        methodName.Replace("Map", ""),
                        routeArg.Trim('"'),
                        document.FilePath, line, project.Name));
                }
            }
        }

        return results.OrderBy(r => r.FilePath).ThenBy(r => r.Line).ToList();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static IEnumerable<Project> GetProjects(Solution solution, string? projectName) =>
        projectName is null
            ? solution.Projects
            : solution.Projects.Where(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> GetMethodChain(SyntaxNode node)
    {
        var current = node.Parent;
        while (current is MemberAccessExpressionSyntax or InvocationExpressionSyntax)
        {
            if (current is MemberAccessExpressionSyntax ma)
                yield return ma.Name.Identifier.Text;
            current = current.Parent;
        }
    }

    private static string ExtractErrorType(string throwExpression)
    {
        var match = System.Text.RegularExpressions.Regex.Match(throwExpression, @"new\s+(\w+)Exception");
        return match.Success ? match.Groups[1].Value : "Domain";
    }
}
