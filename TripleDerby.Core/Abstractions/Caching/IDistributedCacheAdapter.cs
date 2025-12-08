using Microsoft.Extensions.Caching.Distributed;

namespace TripleDerby.Core.Abstractions.Caching;

public interface IDistributedCacheAdapter
{
    Task<string?> GetStringAsync(string key);

    Task SetStringAsync(string key, string value, DistributedCacheEntryOptions options);

    Task RemoveAsync(string key);
}
