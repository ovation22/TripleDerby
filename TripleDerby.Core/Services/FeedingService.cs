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
    public async Task<FeedingRequestStatusResult> GetRequestStatus(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var request = await repository.FindAsync<FeedingRequest>(sessionId, cancellationToken);

        if (request == null)
            throw new KeyNotFoundException($"Feeding request with ID '{sessionId}' was not found.");

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
    public async Task<bool> ReplayFeedingRequest(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Invalid id", nameof(sessionId));

        var entity = await repository.FindAsync<FeedingRequest>(sessionId, cancellationToken);

        if (entity is null)
            return false;

        // If the request already completed, don't replay
        if (entity.Status == FeedingRequestStatus.Completed)
        {
            logger.LogInformation("Not replaying FeedingId={Id} because it is already Completed", entity.Id);
            return false;
        }

        // If previously failed, reset to Pending so processors will pick it up.
        var originalStatus = entity.Status;
        var originalFailureReason = entity.FailureReason;
        try
        {
            if (entity.Status == FeedingRequestStatus.Failed)
            {
                entity.Status = FeedingRequestStatus.Pending;
                entity.FailureReason = null;
                entity.ProcessedDate = null;
                entity.UpdatedDate = timeManager.OffsetUtcNow();

                await repository.UpdateAsync(entity, cancellationToken);

                logger.LogInformation("Marked FeedingId={Id} as Pending for replay", entity.Id);
            }

            var msg = new FeedingRequested(
                entity.Id,
                entity.HorseId,
                entity.FeedingId,
                entity.SessionId,
                entity.OwnerId,
                entity.CreatedDate
            );

            await messagePublisher.PublishAsync(msg, cancellationToken: cancellationToken);

            logger.LogInformation("Replayed FeedingRequested event for FeedingId={Id}", entity.Id);

            return true;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Replay publishing cancelled for FeedingId={Id}", entity.Id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to replay FeedingRequested event for FeedingId={Id}", entity.Id);

            // Attempt to restore Failed status and record failure reason so it can be retried later
            try
            {
                var fr = await repository.FindAsync<FeedingRequest>(sessionId, cancellationToken);
                if (fr != null)
                {
                    fr.Status = FeedingRequestStatus.Failed;
                    fr.FailureReason = $"Replay publish failed: {ex.Message}";
                    fr.ProcessedDate = timeManager.OffsetUtcNow();
                    fr.UpdatedDate = timeManager.OffsetUtcNow();
                    await repository.UpdateAsync(fr, cancellationToken);
                }
            }
            catch (Exception updEx)
            {
                logger.LogWarning(updEx, "Failed to persist replay-publish-failure metadata for FeedingId={Id}", entity.Id);
            }

            // Restore original status in-memory (no DB change) for calling code if needed
            entity.Status = originalStatus;
            entity.FailureReason = originalFailureReason;

            throw;
        }
    }

    /// <summary>
    /// Replays all non-complete feeding requests.
    /// </summary>
    public async Task<int> ReplayAllNonComplete(int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default)
    {
        if (maxDegreeOfParallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

        cancellationToken.ThrowIfCancellationRequested();

        // Fetch all FeedingRequests that are not Completed
        var requests = await repository.ListAsync<FeedingRequest>(fr => fr.Status != FeedingRequestStatus.Completed, cancellationToken);

        if (requests == null || requests.Count == 0)
        {
            logger.LogInformation("No non-complete feeding requests found to replay.");
            return 0;
        }

        logger.LogInformation("Replaying {Count} non-complete feeding requests (maxConcurrency={Max})", requests.Count, maxDegreeOfParallelism);

        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
        var tasks = new List<Task>();
        var publishedCount = 0;

        foreach (var r in requests)
        {
            await semaphore.WaitAsync(cancellationToken);
            var task = Task.Run(async () =>
            {
                try
                {
                    // If previously failed, mark Pending before publishing so processors will pick it up
                    if (r.Status == FeedingRequestStatus.Failed)
                    {
                        try
                        {
                            r.Status = FeedingRequestStatus.Pending;
                            r.FailureReason = null;
                            r.ProcessedDate = null;
                            r.UpdatedDate = timeManager.OffsetUtcNow();
                            await repository.UpdateAsync(r, cancellationToken);
                        }
                        catch (Exception updateEx)
                        {
                            logger.LogWarning(updateEx, "Failed to mark FeedingId={Id} Pending before replay; skipping", r.Id);
                            return;
                        }
                    }

                    var msg = new FeedingRequested(r.Id, r.HorseId, r.FeedingId, r.SessionId, r.OwnerId, r.CreatedDate);
                    await messagePublisher.PublishAsync(msg, cancellationToken: cancellationToken);

                    Interlocked.Increment(ref publishedCount);
                    logger.LogInformation("Replayed FeedingRequested for FeedingId={Id}", r.Id);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to replay FeedingRequested for FeedingId={Id}", r.Id);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        logger.LogInformation("ReplayAllNonComplete finished. Published {Published} of {Total}", publishedCount, requests.Count);

        return publishedCount;
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
    public async Task<FeedingSessionResult> GetFeedingSessionResult(Guid feedingSessionId, CancellationToken cancellationToken = default)
    {
        var session = await repository.FindAsync<FeedingSession>(feedingSessionId, cancellationToken);

        if (session == null)
            throw new KeyNotFoundException($"Feeding session with ID '{feedingSessionId}' was not found.");

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

