using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace DevKit.Mcp.Services;

public enum WorkspaceState { NotStarted, Loading, Ready, Failed }

/// <summary>
/// Manages MSBuildWorkspace with:
/// - LRU compilation cache (30 entries, atomic access counters)
/// - Lazy loading for large solutions (>50 projects pre-warmed, rest on-demand)
/// - 5-second refresh throttle to prevent hammering on rapid file changes
/// - Thread-safe state machine
/// </summary>
public sealed class RoslynWorkspaceService : IAsyncDisposable
{
    private const int MaxCachedCompilations = 30;
    private const int LargeProjectThreshold = 50;
    private static readonly TimeSpan RefreshCooldown = TimeSpan.FromSeconds(5);

    private readonly SolutionOptions _options;
    private readonly ILogger<RoslynWorkspaceService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly CompilationCache _cache = new(MaxCachedCompilations);

    private MSBuildWorkspace? _workspace;
    private Solution? _solution;
    private DateTime _lastRefresh = DateTime.MinValue;

    public WorkspaceState State { get; private set; } = WorkspaceState.NotStarted;

    public RoslynWorkspaceService(SolutionOptions options, ILogger<RoslynWorkspaceService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<Solution> GetSolutionAsync(CancellationToken ct = default)
    {
        if (State == WorkspaceState.Ready && _solution is not null)
            return _solution;

        await _lock.WaitAsync(ct);
        try
        {
            if (State == WorkspaceState.Ready && _solution is not null)
                return _solution;

            _solution = await LoadSolutionCoreAsync(ct);
            return _solution;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets a cached compilation for the given project, computing it if not cached.
    /// </summary>
    public async Task<Compilation?> GetCompilationAsync(Project project, CancellationToken ct = default)
    {
        var cached = _cache.Get(project.Id);
        if (cached is not null) return cached;

        var compilation = await project.GetCompilationAsync(ct);
        if (compilation is not null)
            _cache.Set(project.Id, compilation);

        return compilation;
    }

    public async Task<Solution> ReloadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _cache.Clear();
            _workspace?.Dispose();
            _workspace = null;
            _solution = null;
            State = WorkspaceState.NotStarted;

            _solution = await LoadSolutionCoreAsync(ct);
            return _solution;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Refreshes only if the cooldown has elapsed. Returns true if a refresh occurred.
    /// </summary>
    public async Task<bool> TryRefreshAsync(CancellationToken ct = default)
    {
        if (DateTime.UtcNow - _lastRefresh < RefreshCooldown)
            return false;

        await ReloadAsync(ct);
        return true;
    }

    private async Task<Solution> LoadSolutionCoreAsync(CancellationToken ct)
    {
        State = WorkspaceState.Loading;

        var solutionPath = ResolveSolutionPath();
        if (solutionPath is null)
        {
            State = WorkspaceState.Failed;
            throw new InvalidOperationException(
                "No solution file found. Pass --solution <path> or run from a directory containing a .sln/.slnx file.");
        }

        _logger.LogInformation("Loading solution {Path}", solutionPath);

        _workspace = MSBuildWorkspace.Create();
        _workspace.WorkspaceFailed += (_, e) =>
            _logger.LogWarning("Workspace warning: {Diagnostic}", e.Diagnostic.Message);

        try
        {
            var solution = await _workspace.OpenSolutionAsync(solutionPath, cancellationToken: ct);

            _lastRefresh = DateTime.UtcNow;
            State = WorkspaceState.Ready;

            var projectCount = solution.Projects.Count();
            _logger.LogInformation("Solution loaded: {Count} projects", projectCount);

            if (projectCount <= LargeProjectThreshold)
                await PreWarmAsync(solution, ct);
            else
                _logger.LogInformation("Large solution ({Count} projects) — compilations will load on demand", projectCount);

            return solution;
        }
        catch
        {
            State = WorkspaceState.Failed;
            throw;
        }
    }

    private async Task PreWarmAsync(Solution solution, CancellationToken ct)
    {
        _logger.LogInformation("Pre-warming {Count} compilations", solution.Projects.Count());
        foreach (var project in solution.Projects)
        {
            ct.ThrowIfCancellationRequested();
            var compilation = await project.GetCompilationAsync(ct);
            if (compilation is not null)
                _cache.Set(project.Id, compilation);
        }
    }

    private string? ResolveSolutionPath()
    {
        if (_options.SolutionPath is not null && File.Exists(_options.SolutionPath))
            return _options.SolutionPath;

        return DiscoverSolution(Directory.GetCurrentDirectory());
    }

    private static string? DiscoverSolution(string root)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".git", "bin", "obj", "node_modules", ".vs", "artifacts", ".idea" };

        return SearchDirectory(root, depth: 0, maxDepth: 3, excluded);
    }

    private static string? SearchDirectory(string dir, int depth, int maxDepth, HashSet<string> excluded)
    {
        var slnFiles = Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(dir, "*.slnx", SearchOption.TopDirectoryOnly))
            .OrderBy(f => f)
            .ToList();

        if (slnFiles.Count >= 1) return slnFiles[0];
        if (depth >= maxDepth) return null;

        foreach (var subDir in Directory.GetDirectories(dir)
                     .Where(d => !excluded.Contains(Path.GetFileName(d))))
        {
            var found = SearchDirectory(subDir, depth + 1, maxDepth, excluded);
            if (found is not null) return found;
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        await _lock.WaitAsync();
        try { _workspace?.Dispose(); }
        finally
        {
            _lock.Release();
            _lock.Dispose();
        }
    }

    // ── LRU Compilation Cache ──────────────────────────────────────────────────

    private sealed class CompilationCache(int maxSize)
    {
        private readonly Dictionary<ProjectId, CacheEntry> _entries = new();
        private long _counter;

        public Compilation? Get(ProjectId id)
        {
            lock (_entries)
            {
                if (!_entries.TryGetValue(id, out var entry)) return null;
                entry.LastAccess = Interlocked.Increment(ref _counter);
                return entry.Compilation;
            }
        }

        public void Set(ProjectId id, Compilation compilation)
        {
            lock (_entries)
            {
                if (_entries.Count >= maxSize && !_entries.ContainsKey(id))
                    Evict();

                _entries[id] = new CacheEntry(compilation)
                {
                    LastAccess = Interlocked.Increment(ref _counter)
                };
            }
        }

        public void Clear()
        {
            lock (_entries) { _entries.Clear(); }
        }

        private void Evict()
        {
            if (_entries.Count == 0) return;
            var lruKey = _entries.MinBy(e => e.Value.LastAccess).Key;
            _entries.Remove(lruKey);
        }

        private sealed class CacheEntry(Compilation compilation)
        {
            public Compilation Compilation { get; } = compilation;
            public long LastAccess { get; set; }
        }
    }
}
