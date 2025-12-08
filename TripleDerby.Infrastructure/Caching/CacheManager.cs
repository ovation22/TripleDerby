using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TripleDerby.Core.Abstractions.Caching;
using TripleDerby.Core.Cache;

namespace TripleDerby.Infrastructure.Caching;

public class CacheManager : ICacheManager
{
    private readonly int _cacheExpirationMinutes;
    private readonly IDistributedCacheAdapter _cache;

    public CacheManager(
        IDistributedCacheAdapter cache,
        IOptions<CacheConfig> cacheOptions
    )
    {
        _cache = cache;
        _cacheExpirationMinutes = cacheOptions.Value.DefaultExpirationMinutes;
    }

    public async Task<IEnumerable<T>> GetOrCreate<T>(string key, Func<Task<IEnumerable<T>>> createItem) where T : class
    {
        IEnumerable<T> results;
        var cacheEntry = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(cacheEntry))
        {
            results = (await createItem()).ToList();

            await SetCache(key, results);
        }
        else
        {
            results = JsonSerializer.Deserialize<List<T>>(cacheEntry)!;
        }

        return results;
    }

    public async Task Remove(string key)
    {
        await _cache.RemoveAsync(key);
    }

    private async Task SetCache<T>(string cacheKey, IEnumerable<T> results) where T : class
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheExpirationMinutes)
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(results), options);
    }
}
