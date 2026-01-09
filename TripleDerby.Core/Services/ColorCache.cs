using Microsoft.Extensions.Logging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Entities;

namespace TripleDerby.Core.Services;

/// <summary>
/// Singleton cache for Color reference data.
/// Colors are static game data that rarely change, making them ideal for in-memory caching.
/// This cache eliminates database queries on every breeding operation, significantly improving performance.
/// </summary>
public class ColorCache(ILogger<ColorCache> logger)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<Color>? _colors;
    private readonly ILogger<ColorCache> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Gets all colors, loading from repository on first call and caching for subsequent calls.
    /// Thread-safe using double-checked locking pattern.
    /// </summary>
    /// <param name="repository">Repository to load colors from if cache is empty</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all colors</returns>
    public virtual async Task<List<Color>> GetColorsAsync(
        ITripleDerbyRepository repository,
        CancellationToken cancellationToken = default)
    {
        // Fast path: cache hit (no locking required)
        if (_colors != null)
            return _colors;

        // Slow path: cache miss (only happens once per service lifetime)
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check: another thread may have loaded while we waited
            if (_colors != null)
                return _colors;

            _logger.LogInformation("Loading colors from repository for cache initialization");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _colors = (await repository.GetAllAsync<Color>(cancellationToken)).ToList();

            stopwatch.Stop();
            _logger.LogInformation(
                "Color cache initialized with {ColorCount} colors in {ElapsedMs}ms",
                _colors.Count,
                stopwatch.ElapsedMilliseconds);

            return _colors;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Invalidates the cache, forcing a reload on next GetColorsAsync call.
    /// Use this when colors are modified (rare in production).
    /// </summary>
    public void Invalidate()
    {
        _colors = null;
        _logger.LogInformation("Color cache invalidated");
    }
}
