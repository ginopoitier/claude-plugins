namespace DevKit.Mcp.Models;

// ── Symbol Navigation ──────────────────────────────────────────────────────────

public record SymbolLocation(
    string Name,
    string Kind,
    string FilePath,
    int Line,
    int Column,
    string Signature,
    string Namespace,
    string ProjectName);

public record SymbolDetail(
    string Name,
    string Kind,
    string FullSignature,
    string Namespace,
    string ProjectName,
    string FilePath,
    int Line,
    string? ReturnType,
    IReadOnlyList<ParameterDetail> Parameters,
    string? Documentation,
    bool IsStatic,
    bool IsAbstract,
    bool IsVirtual,
    bool IsOverride,
    string Accessibility);

public record ParameterDetail(
    string Name,
    string Type,
    bool HasDefault,
    string? DefaultValue);

public record ImplementationInfo(
    string TypeName,
    string Kind,
    string FilePath,
    int Line,
    string Namespace,
    string ProjectName);

public record TypeHierarchyResult(
    string TypeName,
    string Namespace,
    string ProjectName,
    string FilePath,
    string? BaseType,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<ImplementationInfo> DerivedTypes,
    IReadOnlyList<ImplementationInfo> Implementations);

public record CallerInfo(
    string CallerMethod,
    string ContainingType,
    string FilePath,
    int Line,
    string Snippet,
    string ProjectName);

public record OverrideInfo(
    string MethodName,
    string ContainingType,
    string FilePath,
    int Line,
    bool IsSealed,
    string ProjectName);

// ── Public API ─────────────────────────────────────────────────────────────────

public record PublicApiResult(
    string TypeName,
    string Namespace,
    string ProjectName,
    string FilePath,
    IReadOnlyList<PublicMember> Members);

public record PublicMember(
    string Name,
    string Kind,
    string Signature,
    string? Documentation,
    bool IsStatic,
    bool IsAbstract);

// ── Diagnostics ────────────────────────────────────────────────────────────────

public record DiagnosticItem(
    string Severity,
    string Id,
    string Message,
    string FilePath,
    int Line,
    int Column,
    string ProjectName);

// ── Anti-patterns ──────────────────────────────────────────────────────────────

public record AntiPatternMatch(
    string PatternId,
    string Description,
    string Suggestion,
    string FilePath,
    int Line,
    string CodeSnippet,
    string ProjectName,
    string Severity = "Warning");

// ── Project Graph ──────────────────────────────────────────────────────────────

public record ProjectNode(
    string Name,
    string FilePath,
    IReadOnlyList<string> References);

public record LayerViolation(
    string SourceProject,
    string TargetProject,
    string ViolationType,
    string Message);

// ── Dead Code ──────────────────────────────────────────────────────────────────

public record DeadCodeItem(
    string Name,
    string Kind,
    string ContainingType,
    string FilePath,
    int Line,
    string ProjectName);

// ── Complexity ─────────────────────────────────────────────────────────────────

public record ComplexityItem(
    string MethodName,
    string TypeName,
    string FilePath,
    int Line,
    int CyclomaticComplexity,
    int ParameterCount,
    int LineCount,
    string ProjectName);

public record LongMethodItem(
    string MethodName,
    string TypeName,
    string FilePath,
    int Line,
    int LineCount,
    int ParameterCount,
    string ProjectName);

// ── Coupling ───────────────────────────────────────────────────────────────────

public record CouplingMetric(
    string TypeName,
    string Namespace,
    int AfferentCoupling,
    int EfferentCoupling,
    string FilePath,
    string ProjectName);

public record InterfaceSegregationViolation(
    string InterfaceName,
    string ImplementorName,
    int TotalMembers,
    int UsedMembers,
    IReadOnlyList<string> UnusedMembers,
    string FilePath,
    string ProjectName);

public record DuplicateCodePair(
    string File1,
    int StartLine1,
    int EndLine1,
    string File2,
    int StartLine2,
    int EndLine2,
    string NormalizedHash);

public record ExtractionCandidate(
    string MethodName,
    string TypeName,
    string FilePath,
    int StartLine,
    int EndLine,
    int LineCount,
    string Reason,
    string ProjectName);

// ── Architecture ───────────────────────────────────────────────────────────────

public record MediatorViolation(
    string ViolationType,
    string Description,
    string FilePath,
    int Line,
    string ProjectName);

public record MissingValidatorItem(
    string CommandName,
    string FilePath,
    int Line,
    string ProjectName);

public record ResultPatternViolation(
    string ViolationType,
    string Description,
    string FilePath,
    int Line,
    string ProjectName);

public record DddViolation(
    string ViolationType,
    string Description,
    string FilePath,
    int Line,
    string ProjectName);

public record UnauthorizedEndpoint(
    string HttpMethod,
    string Pattern,
    string FilePath,
    int Line,
    string ProjectName);

// ── Security ───────────────────────────────────────────────────────────────────

public record SecurityVulnerability(
    string VulnerabilityType,
    string Description,
    string FilePath,
    int Line,
    string Snippet,
    string Severity,
    string ProjectName);

// ── Testing ────────────────────────────────────────────────────────────────────

public record TestGapItem(
    string HandlerName,
    string HandlerFile,
    bool HasTests,
    string? TestFile,
    string ProjectName);

public record TestSmellItem(
    string SmellType,
    string Description,
    string FilePath,
    int Line,
    string ProjectName);

// ── Performance ────────────────────────────────────────────────────────────────

public record N1PatternItem(
    string Description,
    string FilePath,
    int Line,
    string Snippet,
    string ProjectName);

public record MissingNoTrackingItem(
    string QueryName,
    string FilePath,
    int Line,
    string Snippet,
    string ProjectName);

// ── Documentation ──────────────────────────────────────────────────────────────

public record DocumentationGap(
    string MemberName,
    string Kind,
    string FilePath,
    int Line,
    string GapType,
    string ProjectName);

// ── Vue Analysis ───────────────────────────────────────────────────────────────

public record VueAnalysisResult(
    string FileName,
    string FilePath,
    IReadOnlyList<string> Issues);

public record StoreValidationResult(
    string FileName,
    string FilePath,
    IReadOnlyList<string> Issues);

public record MissingApiTypeItem(
    string FilePath,
    int Line,
    string IssueType,
    string Description);

// ── NuGet Audit ────────────────────────────────────────────────────────────────

public record PackageAuditResult(
    string PackageName,
    string CurrentVersion,
    string? LatestVersion,
    string AuditType,      // "vulnerable" | "outdated"
    string Severity,       // "critical" | "high" | "moderate" | "low" | "review" | "patch"
    string? AdvisoryUrl);
