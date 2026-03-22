using DevKit.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// MSBuildLocator MUST be called before any MSBuild/Roslyn types are JIT-compiled.
// Using directives are compile-time aliases — they don't load assemblies. Safe to put first.
Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();

var builder = Host.CreateApplicationBuilder(args);

// Redirect all logging to stderr — stdout is reserved for the MCP JSON-RPC protocol
builder.Logging.ClearProviders();
builder.Logging.AddConsole(opts =>
{
    opts.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Resolve solution path: --solution arg > auto-discover in working directory
var solutionPath = ResolveSolutionPath(args);

// Services
builder.Services.AddSingleton(_ => new SolutionOptions(solutionPath));
builder.Services.AddSingleton<RoslynWorkspaceService>();
builder.Services.AddSingleton<Neo4jService>();
builder.Services.AddSingleton<SqlServerService>();

// MCP server — stdio transport, auto-discovers all [McpServerToolType] classes
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly);

await builder.Build().RunAsync();

static string? ResolveSolutionPath(string[] args)
{
    // --solution <path>
    var idx = Array.IndexOf(args, "--solution");
    if (idx >= 0 && idx + 1 < args.Length)
        return args[idx + 1];

    // Auto-discover .sln in current directory
    var slnFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sln", SearchOption.TopDirectoryOnly);
    return slnFiles.Length == 1 ? slnFiles[0] : null;
}
