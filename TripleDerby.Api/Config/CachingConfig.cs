using System.Diagnostics.CodeAnalysis;
using TripleDerby.Core.Abstractions.Caching;
using TripleDerby.Core.Cache;
using TripleDerby.Infrastructure.Caching;

namespace TripleDerby.Api.Config;

[ExcludeFromCodeCoverage]
public static class CachingConfig
{
    public static void AddCaching(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<CacheConfig>(configuration.GetSection("Cache"));

        // Prefer explicit Redis configuration, fallback to memory if missing
        var redisConfiguration = configuration["Cache:Configuration"]
                                    ?? configuration.GetConnectionString("cache");

        if (string.IsNullOrWhiteSpace(redisConfiguration))
        {
            // No redis config provided — fallback to in-memory to keep service running
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConfiguration;
                options.InstanceName = configuration["Cache:InstanceName"];
            });
        }

        services.AddSingleton<IDistributedCacheAdapter, DistributedCacheAdapter>();
    }
}