namespace TripleDerby.Core.Cache;

public static class CacheKeys
{
    public const string FeaturedDams = "FeaturedDams";
    public const string FeaturedSires = "FeaturedSires";
    public const string Feedings = "Feedings";

    /// <summary>
    /// Cache key for daily feeding options per horse.
    /// Format: FeedingOptions:{horseId}:{yyyy-MM-dd}
    /// </summary>
    public static string FeedingOptions(Guid horseId, DateTime date)
        => $"FeedingOptions:{horseId}:{date:yyyy-MM-dd}";
}
