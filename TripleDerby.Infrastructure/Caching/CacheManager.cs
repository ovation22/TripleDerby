using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TripleDerby.Core.Abstractions.Caching;
using TripleDerby.Core.Cache;

namespace TripleDerby.Infrastructure.Caching;

public class CacheManager(
    IDistributedCacheAdapter cache,
    IOptions<CacheConfig> cacheOptions)
    : ICacheManager
{
    private readonly int _cacheExpirationMinutes = cacheOptions.Value.DefaultExpirationMinutes;

    public async Task<IEnumerable<T>> GetOrCreate<T>(string key, Func<Task<IEnumerable<T>>> createItem) where T : class
    {
        IEnumerable<T> results;
        var cacheEntry = await cache.GetStringAsync(key);

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
        await cache.RemoveAsync(key);
    }

    private async Task SetCache<T>(string cacheKey, IEnumerable<T> results) where T : class
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheExpirationMinutes)
        };

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(results), options);
    }
}
