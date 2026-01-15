using TripleDerby.Core.Abstractions.Caching;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Cache;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Services;

public class FeedingService(
    ICacheManager cache,
    IRandomGenerator randomGenerator,
    ITripleDerbyRepository repository)
    : IFeedingService
{
    public async Task<FeedingResult> Get(byte id)
    {
        var feeding = await repository.SingleOrDefaultAsync(new FeedingSpecification(id));

        if (feeding is null)
        {
            throw new KeyNotFoundException($"Feeding with ID '{id}' was not found.");
        }

        return new FeedingResult
        {
            Id = feeding.Id,
            Name = feeding.Name,
            Description = feeding.Description
        };
    }

    public async Task<IEnumerable<FeedingsResult>> GetAll()
    {
        return await cache.GetOrCreate(CacheKeys.Feedings, async () => await GetFeedings());
    }

    private async Task<IEnumerable<FeedingsResult>> GetFeedings()
    {
        var feedings = await repository.GetAllAsync<Feeding>();

        return feedings.Select(x => new FeedingsResult
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description
        });
    }

    public async Task<FeedingSessionResult> Feed(byte feedingId, Guid horseId)
    {
        const FeedResponse result = FeedResponse.Liked;

        // Get Horse, with Stats, and with prior feedings of type
        // Save horse, with updated stats (Actual)
        // [] check feeding type past results to determine result, etc.
        var horse = await repository.SingleOrDefaultAsync(new HorseWithHappinessAndPriorFeedingsSpecification(horseId, feedingId));

        if (horse is null)
        {
            throw new KeyNotFoundException($"Horse with ID '{horseId}' was not found.");
        }

        var horseHappiness = horse.Statistics.Single(x => x.StatisticId == StatisticId.Happiness);

        horseHappiness.Actual = AffectHorseStatistic(horseHappiness, 0, 1, 0);

        var feedingSession = new FeedingSession
        {
            FeedingId = feedingId,
            Result = result
        };
        
        horse.FeedingSessions.Add(feedingSession);

        await repository.UpdateAsync(horse);

        return new FeedingSessionResult { Result = result };
    }

    private byte AffectHorseStatistic(
        HorseStatistic stat,
        int min,
        int max,
        int actualMin
    )
    {
        return (byte) Math.Clamp(stat.Actual + randomGenerator.Next(min, max),
            actualMin,
            stat.DominantPotential);
    }
}
