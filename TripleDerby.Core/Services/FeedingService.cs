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

public class FeedingService : IFeedingService
{
    private readonly ICacheManager _cache;
    private readonly IRandomGenerator _randomGenerator;
    private readonly ITripleDerbyRepository _repository;

    public FeedingService(
        ICacheManager cache,
        IRandomGenerator randomGenerator,
        ITripleDerbyRepository repository
    )
    {
        _cache = cache;
        _repository = repository;
        _randomGenerator = randomGenerator;
    }

    public async Task<FeedingResult> Get(byte id)
    {
        var feeding = await _repository.SingleOrDefaultAsync(new FeedingSpecification(id));

        return new FeedingResult
        {
            Id = feeding.Id,
            Name = feeding.Name,
            Description = feeding.Description
        };
    }

    public async Task<IEnumerable<FeedingsResult>> GetAll()
    {
        return await _cache.GetOrCreate(CacheKeys.Feedings, async () => await GetFeedings());
    }

    private async Task<IEnumerable<FeedingsResult>> GetFeedings()
    {
        var feedings = await _repository.GetAllAsync<Feeding>();

        return feedings.Select(x => new FeedingsResult
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description
        });
    }

    public async Task<FeedingSessionResult> Feed(byte feedingId, Guid horseId)
    {
        const SharedKernel.Enums.FeedResponse result = SharedKernel.Enums.FeedResponse.Accepted;

        // Get Horse, with Stats, and with prior feedings of type
        // Save horse, with updated stats (Actual)
        // [] check feeding type past results to determine result, etc.
        var horse = await _repository.SingleOrDefaultAsync(new HorseWithHappinessAndPriorFeedingsSpecification(horseId, feedingId));
        var horseHappiness = horse.Statistics.Single(x => x.StatisticId == StatisticId.Happiness);

        horseHappiness.Actual = AffectHorseStatistic(horseHappiness, 0, 1, 0);

        var feedingSession = new FeedingSession
        {
            FeedingId = feedingId,
            Result = result
        };
        
        horse.FeedingSessions.Add(feedingSession);

        await _repository.UpdateAsync(horse);

        return new FeedingSessionResult { Result = result };
    }

    private byte AffectHorseStatistic(
        HorseStatistic stat,
        int min,
        int max,
        int actualMin
    )
    {
        return (byte) Math.Clamp(stat.Actual + _randomGenerator.Next(min, max),
            actualMin,
            stat.DominantPotential);
    }
}
