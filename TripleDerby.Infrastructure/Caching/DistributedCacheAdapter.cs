using Microsoft.Extensions.Caching.Distributed;
using TripleDerby.Core.Abstractions.Caching;

namespace TripleDerby.Infrastructure.Caching;

public class DistributedCacheAdapter : IDistributedCacheAdapter
{
    private readonly IDistributedCache _cache;

    public DistributedCacheAdapter(IDistributedCache cache)
    {
        _cache = cache;
    }

    public Task<string?> GetStringAsync(string key)
    {
        return _cache.GetStringAsync(key);
    }

    public Task SetStringAsync(string key, string value, DistributedCacheEntryOptions options)
    {
        return _cache.SetStringAsync(key, value, options);
    }

    public Task RemoveAsync(string key)
    {
        return _cache.RemoveAsync(key);
    }
}
