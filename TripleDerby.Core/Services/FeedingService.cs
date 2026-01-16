using Microsoft.Extensions.Logging;
using TripleDerby.Core.Abstractions.Caching;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Cache;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Services;

/// <summary>
/// Core feeding service for request orchestration.
/// Follows TrainingService pattern.
/// </summary>
public class FeedingService(
    ICacheManager cache,
    ITripleDerbyRepository repository,
    IMessagePublisher messagePublisher,
    ITimeManager timeManager,
    IRandomGenerator randomGenerator,
    ILogger<FeedingService> logger)
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
            Description = x.Description,
            Category = x.CategoryId.ToString()
        });
    }

    /// <summary>
    /// Queues a feeding session for async processing.
    /// </summary>
    public async Task<FeedingRequested> QueueFeedingAsync(
        Guid horseId,
        byte feedingId,
        Guid sessionId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Queueing feeding request: Horse={HorseId}, Feeding={FeedingId}, Session={SessionId}",
            horseId, feedingId, sessionId);

        var existingRequest = await repository.FindAsync<FeedingRequest>(sessionId, cancellationToken);
        if (existingRequest != null)
        {
            logger.LogInformation("Feeding request {SessionId} already exists with status {Status}, returning existing",
                sessionId, existingRequest.Status);

            return new FeedingRequested(
                existingRequest.Id,
                existingRequest.HorseId,
                existingRequest.FeedingId,
                existingRequest.SessionId,
                existingRequest.OwnerId,
                existingRequest.CreatedDate
            );
        }

        var horse = await repository.FindAsync<Horse>(horseId, cancellationToken);
        if (horse == null)
            throw new KeyNotFoundException($"Horse with ID {horseId} not found");

        var feeding = await repository.FindAsync<Feeding>(feedingId, cancellationToken);
        if (feeding == null)
            throw new KeyNotFoundException($"Feeding with ID {feedingId} not found");

        var feedingRequest = new FeedingRequest
        {
            Id = sessionId,
            HorseId = horseId,
            FeedingId = feedingId,
            SessionId = sessionId,
            OwnerId = ownerId,
            Status = FeedingRequestStatus.Pending,
            CreatedDate = timeManager.OffsetUtcNow(),
            CreatedBy = ownerId
        };

        feedingRequest = await repository.CreateAsync(feedingRequest, cancellationToken);

        var message = new FeedingRequested(
            feedingRequest.Id,
            feedingRequest.HorseId,
            feedingRequest.FeedingId,
            feedingRequest.SessionId,
            feedingRequest.OwnerId,
            feedingRequest.CreatedDate
        );

        await messagePublisher.PublishAsync(message, cancellationToken: cancellationToken);

        logger.LogInformation("Feeding request queued successfully: RequestId={RequestId}", feedingRequest.Id);

        return message;
    }

    /// <summary>
    /// Gets the status of a feeding request.
    /// </summary>
    public async Task<FeedingRequestStatusResult?> GetRequestStatus(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var request = await repository.FindAsync<FeedingRequest>(sessionId, cancellationToken);

        if (request == null)
            return null;

        return new FeedingRequestStatusResult
        {
            Id = request.Id,
            HorseId = request.HorseId,
            FeedingId = request.FeedingId,
            SessionId = request.SessionId,
            FeedingSessionId = request.FeedingSessionId,
            Status = request.Status,
            FailureReason = request.FailureReason,
            CreatedDate = request.CreatedDate,
            ProcessedDate = request.ProcessedDate
        };
    }

    /// <summary>
    /// Re-publishes a failed feeding request.
    /// </summary>
    public async Task ReplayFeedingRequest(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var request = await repository.FindAsync<FeedingRequest>(sessionId, cancellationToken);

        if (request == null)
            throw new KeyNotFoundException($"Feeding request {sessionId} not found");

        if (request.Status != FeedingRequestStatus.Failed)
            throw new InvalidOperationException($"Feeding request {sessionId} is not in Failed status");

        var message = new FeedingRequested(
            request.Id,
            request.HorseId,
            request.FeedingId,
            request.SessionId,
            request.OwnerId,
            request.CreatedDate
        );

        await messagePublisher.PublishAsync(message, cancellationToken: cancellationToken);

        logger.LogInformation("Replayed feeding request: RequestId={RequestId}", request.Id);
    }

    /// <summary>
    /// Gets available feeding options for a horse (3 random daily options).
    /// Options are cached per horse per day to ensure consistency within the same day.
    /// </summary>
    public async Task<List<FeedingOptionResult>> GetFeedingOptions(Guid horseId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var horse = await repository.FindAsync<Horse>(horseId, cancellationToken);
        if (horse == null)
            throw new KeyNotFoundException($"Horse with ID {horseId} not found");

        var today = timeManager.UtcNow().Date;
        var cacheKey = CacheKeys.FeedingOptions(horseId, today);

        // Get or create cached daily options (cache ensures consistency for the day)
        var cachedResult = await cache.GetOrCreate<FeedingOptionResult>(cacheKey, async () =>
        {
            var allFeedings = await repository.GetAllAsync<Feeding>(cancellationToken);

            // Shuffle and take 3 random options
            var shuffled = allFeedings.OrderBy(_ => randomGenerator.Next()).Take(3);

            return shuffled.Select(f => new FeedingOptionResult
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                CategoryId = f.CategoryId,
                HappinessMin = f.HappinessMin,
                HappinessMax = f.HappinessMax,
                SpeedMin = f.SpeedMin,
                SpeedMax = f.SpeedMax,
                StaminaMin = f.StaminaMin,
                StaminaMax = f.StaminaMax,
                AgilityMin = f.AgilityMin,
                AgilityMax = f.AgilityMax,
                DurabilityMin = f.DurabilityMin,
                DurabilityMax = f.DurabilityMax
            });
        });

        return cachedResult.ToList();
    }

    /// <summary>
    /// Gets feeding history for a horse.
    /// </summary>
    public async Task<PagedList<FeedingHistoryResult>> GetFeedingHistory(Guid horseId, PaginationRequest request, CancellationToken cancellationToken = default)
    {
        // Verify horse exists
        var horse = await repository.FindAsync<Horse>(horseId, cancellationToken);
        if (horse == null)
        {
            throw new KeyNotFoundException($"Horse with ID '{horseId}' was not found.");
        }

        // Specification handles filtering, sorting, pagination, and projection
        var spec = new FeedingSessionHistorySpecification(horseId, request);
        return await repository.ListAsync(spec, cancellationToken);
    }

    /// <summary>
    /// Gets the details of a completed feeding session by ID.
    /// Returns null if not found.
    /// </summary>
    public async Task<FeedingSessionResult?> GetFeedingSessionResult(Guid feedingSessionId, CancellationToken cancellationToken = default)
    {
        var session = await repository.FindAsync<FeedingSession>(feedingSessionId, cancellationToken);

        if (session == null)
            return null;

        // Load the feeding details if not already loaded
        if (session.Feeding == null)
        {
            var feeding = await repository.FindAsync<Feeding>(session.FeedingId, cancellationToken);
            if (feeding != null)
                session.Feeding = feeding;
        }

        // Load horse if not already loaded (for name in discovery message)
        if (session.Horse == null)
        {
            var horse = await repository.FindAsync<Horse>(session.HorseId, cancellationToken);
            if (horse != null)
                session.Horse = horse;
        }

        // Build discovery message if preference was discovered
        string? discoveryMessage = null;
        if (session.PreferenceDiscovered)
        {
            var horseName = session.Horse?.Name ?? "Horse";
            var verb = session.Result switch
            {
                FeedResponse.Favorite => "LOVES",
                FeedResponse.Liked => "likes",
                FeedResponse.Neutral => "is okay with",
                FeedResponse.Disliked => "dislikes",
                FeedResponse.Hated => "HATES",
                FeedResponse.Rejected => "refused to eat",
                _ => "tried"
            };
            discoveryMessage = $"{horseName} {verb} {session.Feeding?.Name ?? "this food"}!";
        }

        return new FeedingSessionResult
        {
            SessionId = session.Id,
            FeedingName = session.Feeding?.Name ?? "Unknown",
            Result = session.Result,
            HappinessGain = session.HappinessGain,
            SpeedGain = session.SpeedGain,
            StaminaGain = session.StaminaGain,
            AgilityGain = session.AgilityGain,
            DurabilityGain = session.DurabilityGain,
            PreferenceDiscovered = session.PreferenceDiscovered,
            DiscoveryMessage = discoveryMessage,
            UpsetStomachOccurred = session.UpsetStomachOccurred
        };
    }
}

