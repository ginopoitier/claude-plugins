using DevKit.Mcp.Analyzers;
using DevKit.Mcp.Models;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DevKit.Mcp.Tests;

/// <summary>
/// Unit tests for the individual IAntiPatternDetector implementations.
/// Each detector operates on a parsed SyntaxTree, so no workspace, no disk I/O,
/// and no mocking framework is needed. Tests use AdhocWorkspace-free in-memory
/// trees created via CSharpSyntaxTree.ParseText.
/// </summary>
public sealed class AntipatternDetectorTests
{
    private const string TestFile = "/test/Sample.cs";
    private const string TestProject = "TestProject";

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static IReadOnlyList<AntiPatternMatch> Analyze(IAntiPatternDetector detector, string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        return detector.Analyze(tree, TestFile, TestProject).ToList();
    }

    // ── AsyncVoidDetector ─────────────────────────────────────────────────────

    public sealed class AsyncVoidDetectorTests
    {
        private readonly AsyncVoidDetector _sut = new();

        [Fact]
        public void Analyze_AsyncVoidMethod_ReturnsOneMatch()
        {
            // Arrange
            const string source = """
                public class Foo
                {
                    public async void DoWork() { }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().ContainSingle()
                .Which.PatternId.Should().Be("AsyncVoid");
        }

        [Fact]
        public void Analyze_AsyncVoidEventHandler_ReturnsNoMatch()
        {
            // Arrange — event handler pattern (object sender, EventArgs e) is exempted
            const string source = """
                using System;
                public class Foo
                {
                    public async void OnClick(object sender, EventArgs e) { }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().BeEmpty("event handlers are a legitimate async void use case");
        }

        [Fact]
        public void Analyze_AsyncTaskMethod_ReturnsNoMatch()
        {
            // Arrange
            const string source = """
                using System.Threading.Tasks;
                public class Foo
                {
                    public async Task DoWork() { }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().BeEmpty();
        }

        [Theory]
        [InlineData("public async void Method1() { }", 1)]
        [InlineData("public async void Method1() { } public async void Method2() { }", 2)]
        public void Analyze_MultipleAsyncVoidMethods_ReturnsMatchForEach(string methodBlock, int expectedCount)
        {
            // Arrange
            var source = $"public class Foo {{ {methodBlock} }}";

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().HaveCount(expectedCount);
        }
    }

    // ── SyncOverAsyncDetector ─────────────────────────────────────────────────

    public sealed class SyncOverAsyncDetectorTests
    {
        private readonly SyncOverAsyncDetector _sut = new();

        [Fact]
        public void Analyze_DotResultAccess_ReturnsMatch()
        {
            // Arrange
            const string source = """
                using System.Threading.Tasks;
                public class Foo
                {
                    public string GetData()
                    {
                        var result = Task.FromResult("hello").Result;
                        return result;
                    }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().ContainSingle()
                .Which.Severity.Should().Be("Error", "blocking on async is a hard error");
        }

        [Fact]
        public void Analyze_DotWait_ReturnsMatch()
        {
            // Arrange
            const string source = """
                using System.Threading.Tasks;
                public class Foo
                {
                    public void Run()
                    {
                        Task.Delay(100).Wait();
                    }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().ContainSingle()
                .Which.PatternId.Should().Be("SyncOverAsync");
        }

        [Fact]
        public void Analyze_GetAwaiterGetResult_ReturnsMatch()
        {
            // Arrange
            const string source = """
                using System.Threading.Tasks;
                public class Foo
                {
                    public string Get()
                    {
                        return Task.FromResult("x").GetAwaiter().GetResult();
                    }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().ContainSingle(
                "GetAwaiter().GetResult() is the same deadlock risk as .Result");
        }

        [Fact]
        public void Analyze_AwaitedTask_ReturnsNoMatch()
        {
            // Arrange
            const string source = """
                using System.Threading.Tasks;
                public class Foo
                {
                    public async Task<string> GetAsync()
                    {
                        return await Task.FromResult("hello");
                    }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().BeEmpty("proper await usage should not be flagged");
        }
    }

    // ── HardcodedSecretDetector ───────────────────────────────────────────────

    public sealed class HardcodedSecretDetectorTests
    {
        private readonly HardcodedSecretDetector _sut = new();

        [Theory]
        [InlineData("password", "supersecret123")]
        [InlineData("apiKey", "sk-prod-abc123xyz")]
        [InlineData("connectionString", "Server=localhost;Database=db")]
        [InlineData("token", "eyJhbGciOiJIUzI1NiJ9.payload")]
        public void Analyze_KnownSecretAssignment_ReturnsMatch(string variableName, string value)
        {
            // Arrange
            var source = $$"""
                public class Config
                {
                    public string {{variableName}};
                    public void Setup()
                    {
                        {{variableName}} = "{{value}}";
                    }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().ContainSingle(
                $"assignment to '{variableName}' with a string literal should be flagged")
                .Which.Severity.Should().Be("Error");
        }

        [Fact]
        public void Analyze_ShortStringValue_ReturnsNoMatch()
        {
            // Arrange — value shorter than 6 chars is skipped (too short to be a real secret)
            const string source = """
                public class Foo
                {
                    public void Setup() { var password = "abc"; }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().BeEmpty("values under 6 characters cannot be meaningful secrets");
        }

        [Fact]
        public void Analyze_InMigrationFile_ReturnsNoMatch()
        {
            // Arrange — detector skips files with "Migration" in path
            const string source = """
                public class Setup
                {
                    void Go() { var password = "verylongsecretpassword"; }
                }
                """;
            var tree = CSharpSyntaxTree.ParseText(source);

            // Act
            var matches = _sut.Analyze(tree, "/Migrations/20240101_Init.cs", TestProject).ToList();

            // Assert
            matches.Should().BeEmpty("migration files are explicitly excluded from secret detection");
        }

        [Fact]
        public void Analyze_NonSecretVariable_ReturnsNoMatch()
        {
            // Arrange
            const string source = """
                public class Foo
                {
                    public void Setup() { var displayName = "Hello World"; }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().BeEmpty();
        }
    }

    // ── EmptyCatchDetector ────────────────────────────────────────────────────

    public sealed class EmptyCatchDetectorTests
    {
        private readonly EmptyCatchDetector _sut = new();

        [Fact]
        public void Analyze_EmptyCatchBlock_ReturnsMatch()
        {
            // Arrange
            const string source = """
                public class Foo
                {
                    public void Run()
                    {
                        try { int x = 1; }
                        catch { }
                    }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().ContainSingle()
                .Which.Severity.Should().Be("Error");
        }

        [Fact]
        public void Analyze_CatchWithStatement_ReturnsNoMatch()
        {
            // Arrange
            const string source = """
                using System;
                public class Foo
                {
                    public void Run()
                    {
                        try { int x = 1; }
                        catch (Exception ex) { Console.WriteLine(ex.Message); }
                    }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().BeEmpty("a catch block with a statement is not empty");
        }
    }

    // ── ThreadSleepDetector ───────────────────────────────────────────────────

    public sealed class ThreadSleepDetectorTests
    {
        private readonly ThreadSleepDetector _sut = new();

        [Fact]
        public void Analyze_ThreadSleepCall_ReturnsMatch()
        {
            // Arrange
            const string source = """
                using System.Threading;
                public class Foo
                {
                    public void Poll() { Thread.Sleep(1000); }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().ContainSingle()
                .Which.PatternId.Should().Be("ThreadSleep");
        }

        [Fact]
        public void Analyze_TaskDelay_ReturnsNoMatch()
        {
            // Arrange — Task.Delay is the correct async alternative
            const string source = """
                using System.Threading.Tasks;
                public class Foo
                {
                    public async Task Poll() { await Task.Delay(1000); }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().BeEmpty();
        }
    }

    // ── DateTimeNowDetector ───────────────────────────────────────────────────

    public sealed class DateTimeNowDetectorTests
    {
        private readonly DateTimeNowDetector _sut = new();

        [Theory]
        [InlineData("DateTime.Now", "Now")]
        [InlineData("DateTime.UtcNow", "UtcNow")]
        public void Analyze_DateTimeNowOrUtcNow_ReturnsMatch(string expression, string property)
        {
            // Arrange
            var source = $$"""
                using System;
                public class Foo
                {
                    public void Log() { var t = {{expression}}; }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().ContainSingle()
                .Which.CodeSnippet.Should().Contain(property);
        }

        [Fact]
        public void Analyze_TimeProviderUsage_ReturnsNoMatch()
        {
            // Arrange
            const string source = """
                using System;
                public class Foo
                {
                    private readonly TimeProvider _time;
                    public Foo(TimeProvider time) { _time = time; }
                    public void Log() { var t = _time.GetUtcNow(); }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().BeEmpty("TimeProvider.GetUtcNow() is the correct injectable pattern");
        }
    }

    // ── InlineHttpClientDetector ──────────────────────────────────────────────

    public sealed class InlineHttpClientDetectorTests
    {
        private readonly InlineHttpClientDetector _sut = new();

        [Fact]
        public void Analyze_NewHttpClient_ReturnsMatch()
        {
            // Arrange
            const string source = """
                using System.Net.Http;
                public class ApiClient
                {
                    public void Fetch()
                    {
                        var client = new HttpClient();
                    }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().ContainSingle()
                .Which.PatternId.Should().Be("InlineHttpClient");
        }

        [Fact]
        public void Analyze_InjectedHttpClient_ReturnsNoMatch()
        {
            // Arrange
            const string source = """
                using System.Net.Http;
                public class ApiClient
                {
                    private readonly HttpClient _client;
                    public ApiClient(HttpClient client) { _client = client; }
                }
                """;

            // Act
            var matches = Analyze(_sut, source);

            // Assert
            matches.Should().BeEmpty();
        }
    }
}
