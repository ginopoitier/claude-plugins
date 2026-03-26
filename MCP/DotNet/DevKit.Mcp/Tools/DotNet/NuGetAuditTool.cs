using System.ComponentModel;
using DevKit.Mcp.Models;
using ModelContextProtocol.Server;

namespace DevKit.Mcp.Tools.DotNet;

[McpServerToolType]
public sealed class NuGetAuditTool
{
    [McpServerTool, Description(
        "Audits NuGet packages for vulnerabilities and outdated versions using 'dotnet list package'. " +
        "Returns structured results grouped by severity. " +
        "Requires .NET SDK and an internet connection for vulnerability data.")]
    public async Task<IReadOnlyList<PackageAuditResult>> AuditPackages(
        [Description("Path to the solution or project directory. Defaults to current directory.")] string? rootPath = null,
        [Description("Include transitive (indirect) dependencies. Default true.")] bool includeTransitive = true,
        CancellationToken ct = default)
    {
        var root = rootPath ?? Directory.GetCurrentDirectory();
        var results = new List<PackageAuditResult>();

        // Find .sln or .csproj
        var slnFiles = Directory.GetFiles(root, "*.sln", SearchOption.TopDirectoryOnly);
        var csprojFiles = Directory.GetFiles(root, "*.csproj", SearchOption.TopDirectoryOnly);
        var target = slnFiles.FirstOrDefault() ?? csprojFiles.FirstOrDefault() ?? root;

        // Run vulnerable audit
        var transitiveFlag = includeTransitive ? "--include-transitive" : "";
        var vulnerableOutput = await RunDotnetCommandAsync(
            $"list \"{target}\" package --vulnerable {transitiveFlag}", ct);

        ParseVulnerableOutput(vulnerableOutput, results);

        // Run outdated audit
        var outdatedOutput = await RunDotnetCommandAsync(
            $"list \"{target}\" package --outdated {transitiveFlag}", ct);

        ParseOutdatedOutput(outdatedOutput, results);

        return results
            .OrderBy(r => r.Severity == "critical" ? 0
                : r.Severity == "high" ? 1
                : r.Severity == "moderate" ? 2
                : r.Severity == "low" ? 3
                : r.AuditType == "vulnerable" ? 4 : 5)
            .ThenBy(r => r.PackageName)
            .ToList();
    }

    private static async Task<string> RunDotnetCommandAsync(string args, CancellationToken ct)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        try
        {
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            return output;
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    private static void ParseVulnerableOutput(string output, List<PackageAuditResult> results)
    {
        // Parse lines like: "   > Microsoft.AspNetCore.Mvc 2.1.0  Critical  https://..."
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart('>', ' ');
            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                var severity = parts.FirstOrDefault(p =>
                    p.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("High", StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("Moderate", StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("Low", StringComparison.OrdinalIgnoreCase));

                if (severity is not null)
                {
                    var sevIdx = Array.IndexOf(parts, severity);
                    var packageName = parts[0];
                    var currentVersion = sevIdx > 1 ? parts[sevIdx - 1] : "unknown";
                    var advisoryUrl = parts.LastOrDefault(p => p.StartsWith("http")) ?? "";

                    results.Add(new PackageAuditResult(
                        packageName,
                        currentVersion,
                        null,
                        "vulnerable",
                        severity.ToLowerInvariant(),
                        advisoryUrl));
                }
            }
        }
    }

    private static void ParseOutdatedOutput(string output, List<PackageAuditResult> results)
    {
        // Parse lines like: "   > Serilog.AspNetCore  7.0.0  7.0.0  8.0.3"
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart('>', ' ');
            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 4)
            {
                var packageName = parts[0];
                // Skip headers and non-package lines
                if (packageName.StartsWith("Project") || packageName.StartsWith("Top") || packageName.StartsWith("[")) continue;

                // Format: Name RequestedVersion ResolvedVersion LatestVersion
                if (Version.TryParse(parts[1].TrimStart('[').TrimEnd(']'), out _) ||
                    parts[1].Contains('.'))
                {
                    var current = parts[2]; // resolved
                    var latest = parts[3];  // latest

                    // Determine if major version bump (higher risk)
                    var severity = IsMajorVersionBump(current, latest) ? "review" : "patch";

                    // Don't duplicate if already flagged as vulnerable
                    if (!results.Any(r => r.PackageName == packageName && r.AuditType == "vulnerable"))
                    {
                        results.Add(new PackageAuditResult(
                            packageName,
                            current,
                            latest,
                            "outdated",
                            severity,
                            null));
                    }
                }
            }
        }
    }

    private static bool IsMajorVersionBump(string current, string latest)
    {
        if (!Version.TryParse(current, out var cv) || !Version.TryParse(latest, out var lv))
            return false;
        return lv.Major > cv.Major;
    }
}
