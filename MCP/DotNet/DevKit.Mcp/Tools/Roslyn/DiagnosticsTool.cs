using System.ComponentModel;
using DevKit.Mcp.Models;
using DevKit.Mcp.Services;
using Microsoft.CodeAnalysis;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.Roslyn;

/// <summary>
/// Provides MCP tools for retrieving Roslyn compiler diagnostics and managing the workspace lifecycle.
/// </summary>
[McpServerToolType]
public sealed class DiagnosticsTool(RoslynWorkspaceService workspace)
{
    [McpServerTool, Description(
        "Returns compiler errors and warnings for the solution or a specific project. " +
        "Use this instead of running 'dotnet build' and parsing output — returns structured results directly.")]
    /// <summary>
    /// Returns structured compiler errors and warnings for the solution or a specific project.
    /// </summary>
    public async Task<IReadOnlyList<DiagnosticItem>> GetDiagnostics(
        [Description("Filter to a specific project. Omit to get diagnostics for the whole solution.")] string? projectName = null,
        [Description("Minimum severity to include: Error, Warning, Info. Defaults to Warning.")] string minSeverity = "Warning",
        CancellationToken ct = default)
    {
        var solution = await workspace.GetSolutionAsync(ct);
        var minLevel = ParseSeverity(minSeverity);

        var projects = projectName is null
            ? solution.Projects
            : solution.Projects.Where(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));

        var results = new List<DiagnosticItem>();

        foreach (var project in projects)
        {
            ct.ThrowIfCancellationRequested();
            var compilation = await workspace.GetCompilationAsync(project, ct);
            if (compilation is null) continue;

            var diagnostics = compilation.GetDiagnostics(ct)
                .Where(d => d.Severity >= minLevel && !d.IsSuppressed)
                .Where(d => d.Location.IsInSource);

            foreach (var diag in diagnostics)
            {
                var lineSpan = diag.Location.GetLineSpan();
                results.Add(new DiagnosticItem(
                    diag.Severity.ToString(),
                    diag.Id,
                    diag.GetMessage(),
                    lineSpan.Path ?? "unknown",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    project.Name));
            }
        }

        return results
            .OrderByDescending(d => ParseSeverity(d.Severity))
            .ThenBy(d => d.ProjectName)
            .ThenBy(d => d.FilePath)
            .ThenBy(d => d.Line)
            .ToList();
    }

    /// <summary>
    /// Reloads the Roslyn solution from disk and returns a summary of loaded projects.
    /// </summary>
    [McpServerTool, Description("Reloads the Roslyn workspace from disk. Call after adding files or changing project references.")]
    public async Task<string> ReloadSolution(CancellationToken ct = default)
    {
        var solution = await workspace.ReloadAsync(ct);
        return $"Solution reloaded: {solution.Projects.Count()} projects.";
    }

    /// <summary>
    /// Returns the current Roslyn workspace loading state.
    /// </summary>
    [McpServerTool, Description("Returns the current workspace state: NotStarted, Loading, Ready, or Failed.")]
    public string GetWorkspaceStatus() =>
        $"State: {workspace.State}";

    private static DiagnosticSeverity ParseSeverity(string s) => s.ToLowerInvariant() switch
    {
        "error" => DiagnosticSeverity.Error,
        "info" or "information" => DiagnosticSeverity.Info,
        _ => DiagnosticSeverity.Warning
    };
}
