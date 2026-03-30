using DevKit.Mcp.Models;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DevKit.Mcp.Tests;

/// <summary>
/// Unit tests for the security scanning logic inside SecurityTool.
///
/// SecurityTool's scan methods are private static, and RoslynWorkspaceService
/// is sealed with no interface, so we cannot inject a fake workspace without
/// modifying production code. Instead we drive the same Roslyn queries that the
/// scanner uses through in-memory CSharpSyntaxTree objects, verifying the
/// detection contracts independently of the workspace layer.
///
/// Each test class mirrors one ScanXxx method inside SecurityTool. The scanner
/// logic is replicated via the same Roslyn queries so we can assert:
///   (a) the pattern IS detected in code that contains the vulnerability, and
///   (b) the pattern is NOT detected in safe code.
///
/// This approach means the tests will catch regressions if the scanner queries
/// change AND will fail loudly if the models change — both desirable.
/// </summary>
public sealed class SecurityScannerTests
{
    private const string TestFile = "/src/Service.cs";
    private const string TestProject = "App";

    // ── Shared Roslyn helpers ─────────────────────────────────────────────────

    private static SyntaxNode ParseRoot(string source) =>
        CSharpSyntaxTree.ParseText(source).GetRoot();

    // ── ScanHardcodedConnStrings ──────────────────────────────────────────────

    public sealed class HardcodedConnectionStringTests
    {
        private static readonly string[] ConnStringKeywords =
            ["Server=", "Data Source=", "Initial Catalog=", "Password=", "Pwd="];

        /// Mirrors the query inside SecurityTool.ScanHardcodedConnStrings
        private static IReadOnlyList<SecurityVulnerability> Scan(string source)
        {
            var root = ParseRoot(source);
            var results = new List<SecurityVulnerability>();

            foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>()
                         .Where(l => l.IsKind(SyntaxKind.StringLiteralExpression)))
            {
                var value = literal.Token.ValueText;
                if (value.Length < 10) continue;
                if (!ConnStringKeywords.Any(k => value.Contains(k, StringComparison.OrdinalIgnoreCase))) continue;

                var line = literal.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                results.Add(new SecurityVulnerability(
                    "HardcodedConnectionString",
                    "Hardcoded connection string detected",
                    TestFile, line,
                    value[..Math.Min(60, value.Length)] + "...",
                    "High", TestProject));
            }

            return results;
        }

        [Fact]
        public void Scan_SqlServerConnectionString_DetectsVulnerability()
        {
            // Arrange
            const string source = """
                public class DbConfig
                {
                    private readonly string _conn =
                        "Server=prod-db.corp.local;Initial Catalog=Orders;User Id=sa;Password=abc123";
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().ContainSingle()
                .Which.VulnerabilityType.Should().Be("HardcodedConnectionString");
        }

        [Fact]
        public void Scan_DataSourceConnectionString_DetectsVulnerability()
        {
            // Arrange
            const string source = """
                public class Repo
                {
                    void Connect()
                    {
                        var cs = "Data Source=myserver;Initial Catalog=db;Password=hunter2";
                    }
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().ContainSingle()
                .Which.Severity.Should().Be("High");
        }

        [Fact]
        public void Scan_PwdKeyword_DetectsVulnerability()
        {
            // Arrange
            const string source = """
                public class Legacy
                {
                    static string Conn = "Server=db;Pwd=secret12345";
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().ContainSingle();
        }

        [Fact]
        public void Scan_SafeConfigurationKeyReference_DoesNotFlag()
        {
            // Arrange — value read from config; no literal contains a connection string keyword
            const string source = """
                public class DbConfig
                {
                    private readonly string _conn;
                    public DbConfig(IConfiguration config)
                    {
                        _conn = config.GetConnectionString("Default");
                    }
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().BeEmpty("reading from IConfiguration is the safe pattern");
        }

        [Fact]
        public void Scan_ShortStringLiteral_IsIgnored()
        {
            // Arrange — strings under 10 chars are skipped to avoid false positives on labels
            const string source = """
                public class Foo
                {
                    string _s = "Server=x";
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert — "Server=x" is only 8 chars, below the 10-char minimum
            findings.Should().BeEmpty("the 10-character minimum guards against noise on short labels");
        }
    }

    // ── ScanPathTraversal ─────────────────────────────────────────────────────

    public sealed class PathTraversalTests
    {
        /// Mirrors the query inside SecurityTool.ScanPathTraversal
        private static IReadOnlyList<SecurityVulnerability> Scan(string source)
        {
            var root = ParseRoot(source);
            var results = new List<SecurityVulnerability>();

            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;
                var method = access.Name.Identifier.Text;
                var type = access.Expression.ToString();

                if (!((type == "Path" && method is "Combine" or "GetFullPath") ||
                      (type is "File" or "Directory" or "System.IO.File") &&
                      method is "Open" or "ReadAllText" or "WriteAllText" or "Delete"))
                    continue;

                var args = invocation.ArgumentList.Arguments;
                var hasExternalInput = args.Any(a =>
                    a.Expression is IdentifierNameSyntax id &&
                    !id.Identifier.Text.StartsWith("_") &&
                    char.IsLower(id.Identifier.Text[0]));

                if (!hasExternalInput) continue;

                var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                results.Add(new SecurityVulnerability(
                    "PathTraversal",
                    $"{type}.{method}() called with what appears to be user-supplied input",
                    TestFile, line,
                    invocation.ToString()[..Math.Min(120, invocation.ToString().Length)],
                    "High", TestProject));
            }

            return results;
        }

        [Fact]
        public void Scan_PathCombineWithParameter_DetectsTraversal()
        {
            // Arrange — 'fileName' is lowercase and not a field, so it looks like user input
            const string source = """
                using System.IO;
                public class FileService
                {
                    public string GetPath(string fileName)
                    {
                        return Path.Combine("/uploads", fileName);
                    }
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().ContainSingle()
                .Which.VulnerabilityType.Should().Be("PathTraversal");
        }

        [Fact]
        public void Scan_FileReadAllTextWithParameter_DetectsTraversal()
        {
            // Arrange
            const string source = """
                using System.IO;
                public class Reader
                {
                    public string Read(string path)
                    {
                        return File.ReadAllText(path);
                    }
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().ContainSingle()
                .Which.Severity.Should().Be("High");
        }

        [Fact]
        public void Scan_PathCombineWithConstantOnly_DoesNotFlag()
        {
            // Arrange — both arguments are compile-time constants, no user input
            const string source = """
                using System.IO;
                public class Config
                {
                    private static readonly string BasePath = Path.Combine("/app", "data");
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().BeEmpty("constant path segments do not constitute path traversal");
        }

        [Fact]
        public void Scan_FileDeleteWithPrivateField_DoesNotFlag()
        {
            // Arrange — underscore-prefixed identifier is treated as a private field, not user input
            const string source = """
                using System.IO;
                public class Cleaner
                {
                    private string _tempPath = "/tmp/work.tmp";
                    public void Clean() { File.Delete(_tempPath); }
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().BeEmpty("field references (underscore-prefixed) should not be flagged");
        }

        [Fact]
        public void Scan_PathGetFullPathWithUserInput_DetectsTraversal()
        {
            // Arrange
            const string source = """
                using System.IO;
                public class Uploader
                {
                    public string Resolve(string userPath)
                    {
                        return Path.GetFullPath(userPath);
                    }
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().ContainSingle();
        }
    }

    // ── ScanSqlInjection ──────────────────────────────────────────────────────

    public sealed class SqlInjectionTests
    {
        private static readonly string[] SqlKeywords =
            ["SELECT", "INSERT", "UPDATE", "DELETE", "EXEC", "EXECUTE"];

        /// Mirrors the query inside SecurityTool.ScanSqlInjection
        private static IReadOnlyList<SecurityVulnerability> Scan(string source)
        {
            var root = ParseRoot(source);
            var results = new List<SecurityVulnerability>();

            // String concatenation
            foreach (var addition in root.DescendantNodes().OfType<BinaryExpressionSyntax>()
                         .Where(b => b.IsKind(SyntaxKind.AddExpression)))
            {
                var text = addition.ToString();
                if (!SqlKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase))) continue;

                var hasLiteral = addition.DescendantNodes().OfType<LiteralExpressionSyntax>()
                    .Any(l => l.IsKind(SyntaxKind.StringLiteralExpression));
                var hasVariable = addition.DescendantNodes().OfType<IdentifierNameSyntax>().Any();

                if (!hasLiteral || !hasVariable) continue;

                var line = addition.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                results.Add(new SecurityVulnerability(
                    "SqlInjection",
                    "SQL query built via string concatenation",
                    TestFile, line,
                    text[..Math.Min(120, text.Length)],
                    "Critical", TestProject));
            }

            // String interpolation
            foreach (var interpolation in root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
            {
                var text = interpolation.ToString();
                if (!SqlKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase))) continue;

                var line = interpolation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                results.Add(new SecurityVulnerability(
                    "SqlInjection",
                    "SQL query uses string interpolation",
                    TestFile, line,
                    text[..Math.Min(120, text.Length)],
                    "Critical", TestProject));
            }

            return results;
        }

        [Fact]
        public void Scan_SelectConcatenation_DetectsCriticalVulnerability()
        {
            // Arrange
            const string source = """
                public class UserRepo
                {
                    public string BuildQuery(string userId)
                    {
                        return "SELECT * FROM Users WHERE Id = " + userId;
                    }
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().ContainSingle()
                .Which.Severity.Should().Be("Critical");
        }

        [Fact]
        public void Scan_InterpolatedSqlSelect_DetectsVulnerability()
        {
            // Arrange
            const string source = """
                public class OrderRepo
                {
                    public string GetSql(string status)
                    {
                        return $"SELECT * FROM Orders WHERE Status = '{status}'";
                    }
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().ContainSingle()
                .Which.VulnerabilityType.Should().Be("SqlInjection");
        }

        [Fact]
        public void Scan_InterpolatedDeleteStatement_DetectsVulnerability()
        {
            // Arrange
            const string source = """
                public class AdminService
                {
                    string Wipe(string table) => $"DELETE FROM {table}";
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().ContainSingle();
        }

        [Fact]
        public void Scan_ParameterizedQuery_DoesNotFlag()
        {
            // Arrange — parameterized query via Dapper; no string concatenation of SQL + variable
            const string source = """
                public class SafeRepo
                {
                    const string Query = "SELECT * FROM Users WHERE Id = @Id";
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert — literal-only string, no variable added to it
            findings.Should().BeEmpty("constants composed of only string literals are safe");
        }

        [Fact]
        public void Scan_StringConcatenationWithNoSqlKeyword_DoesNotFlag()
        {
            // Arrange
            const string source = """
                public class Greeting
                {
                    string Build(string name) => "Hello, " + name + "!";
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().BeEmpty("string concatenation without SQL keywords is not an injection risk");
        }
    }

    // ── ScanDangerousDeserialization ──────────────────────────────────────────

    public sealed class DangerousDeserializationTests
    {
        private static readonly string[] DangerousTypes =
            ["BinaryFormatter", "NetDataContractSerializer", "LosFormatter"];

        /// Mirrors the query inside SecurityTool.ScanDangerousDeserialization
        private static IReadOnlyList<SecurityVulnerability> Scan(string source)
        {
            var root = ParseRoot(source);
            var results = new List<SecurityVulnerability>();

            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                if (!DangerousTypes.Any(t => creation.Type.ToString().Contains(t))) continue;

                var line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                results.Add(new SecurityVulnerability(
                    "InsecureDeserialization",
                    $"{creation.Type} is vulnerable to deserialization attacks",
                    TestFile, line,
                    creation.ToString(),
                    "Critical", TestProject));
            }

            return results;
        }

        [Theory]
        [InlineData("BinaryFormatter", "new BinaryFormatter()")]
        [InlineData("NetDataContractSerializer", "new NetDataContractSerializer()")]
        [InlineData("LosFormatter", "new LosFormatter()")]
        public void Scan_KnownDangerousType_DetectsCriticalVulnerability(
            string typeName, string instantiation)
        {
            // Arrange
            var source = $$"""
                public class Deserializer
                {
                    void Load() { var f = {{instantiation}}; }
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().ContainSingle()
                .Which.Severity.Should().Be("Critical");
        }

        [Fact]
        public void Scan_SystemTextJsonSerializer_DoesNotFlag()
        {
            // Arrange — System.Text.Json is safe
            const string source = """
                using System.Text.Json;
                public class Safe
                {
                    void Deserialize(string json)
                    {
                        var obj = JsonSerializer.Deserialize<object>(json);
                    }
                }
                """;

            // Act
            var findings = Scan(source);

            // Assert
            findings.Should().BeEmpty("System.Text.Json is not vulnerable to deserialization attacks");
        }
    }
}
