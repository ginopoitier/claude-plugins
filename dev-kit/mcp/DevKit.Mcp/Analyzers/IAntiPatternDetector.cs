using DevKit.Mcp.Models;
using Microsoft.CodeAnalysis;

namespace DevKit.Mcp.Analyzers;

public enum DetectorMode
{
    /// <summary>Operates on SyntaxTree only — no compilation required. Fast.</summary>
    Syntax,
    /// <summary>Requires SemanticModel. Slower but type-aware.</summary>
    Semantic
}

public interface IAntiPatternDetector
{
    string PatternId { get; }
    DetectorMode Mode { get; }

    /// <summary>Called for Syntax-mode detectors on each file's SyntaxTree.</summary>
    IEnumerable<AntiPatternMatch> Analyze(SyntaxTree tree, string filePath, string projectName)
        => [];

    /// <summary>Called for Semantic-mode detectors. Only invoked when a compilation is available.</summary>
    Task<IEnumerable<AntiPatternMatch>> AnalyzeSemanticAsync(
        SemanticModel model, SyntaxTree tree, string filePath, string projectName, CancellationToken ct)
        => Task.FromResult(Enumerable.Empty<AntiPatternMatch>());
}
