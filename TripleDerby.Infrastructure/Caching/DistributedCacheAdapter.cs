using Microsoft.Extensions.Caching.Distributed;
using TripleDerby.Core.Abstractions.Caching;

namespace TripleDerby.Infrastructure.Caching;

public class DistributedCacheAdapter(IDistributedCache cache) : IDistributedCacheAdapter
{
    public Task<string?> GetStringAsync(string key)
    {
        return cache.GetStringAsync(key);
    }

    public Task SetStringAsync(string key, string value, DistributedCacheEntryOptions options)
    {
        return cache.SetStringAsync(key, value, options);
    }

    public Task RemoveAsync(string key)
    {
        return cache.RemoveAsync(key);
    }
}
