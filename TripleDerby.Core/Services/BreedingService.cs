using Ardalis.Specification;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TripleDerby.Core.Abstractions.Caching;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Cache;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Core.Services;

public partial class BreedingService(
    IDistributedCacheAdapter cache,
    ITripleDerbyRepository repository,
    IOptions<CacheConfig> cacheOptions,
    ITimeManager timeManager,
    [FromKeyedServices("rabbitmq")] IMessagePublisher messagePublisher,
    ILogger<BreedingService> logger)
    : IBreedingService
{
    private readonly int _cacheExpirationMinutes = cacheOptions.Value.DefaultExpirationMinutes;

    /// <inheritdoc />
    public async Task<IEnumerable<ParentHorse>> GetDams()
    {
        return await GetParentHorses(CacheKeys.FeaturedDams, false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ParentHorse>> GetSires()
    {
        return await GetParentHorses(CacheKeys.FeaturedSires, true);
    }

    /// <inheritdoc />
    public async Task<BreedingRequested> Breed(BreedRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Creating breeding request (sire={SireId}, dam={DamId}, user={UserId})", request.SireId, request.DamId, request.UserId);

        var dam = await repository.SingleOrDefaultAsync(new ParentHorseSpecification(request.DamId), cancellationToken);
        var sire = await repository.SingleOrDefaultAsync(new ParentHorseSpecification(request.SireId), cancellationToken);

        if (dam is null)
            throw new InvalidOperationException($"Unable to retrieve Dam ({request.DamId})");

        if (sire is null)
            throw new InvalidOperationException($"Unable to retrieve Sire ({request.SireId})");
        
        var breedingRequestEntity = new BreedingRequest
        {
            SireId = request.SireId,
            DamId = request.DamId,
            OwnerId = request.UserId,
            CreatedDate = timeManager.OffsetUtcNow()
        };

        breedingRequestEntity = await repository.CreateAsync(breedingRequestEntity, cancellationToken);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var breedingRequested = new BreedingRequested(
                breedingRequestEntity.Id,
                breedingRequestEntity.SireId,
                breedingRequestEntity.DamId,
                breedingRequestEntity.OwnerId,
                breedingRequestEntity.CreatedDate
            );

            await messagePublisher.PublishAsync(breedingRequested, cancellationToken: cancellationToken);

            logger.LogInformation("Published BreedingRequested event for BreedingId={Id}", breedingRequestEntity.Id);

            return breedingRequested;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Breeding request publishing cancelled for BreedingId={Id}", breedingRequestEntity.Id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish BreedingRequested event for BreedingId={Id}", breedingRequestEntity.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ReplayBreedingRequest(Guid breedingRequestId, CancellationToken cancellationToken = default)
    {
        if (breedingRequestId == Guid.Empty) 
            throw new ArgumentException("Invalid id", nameof(breedingRequestId));

        var entity = await repository.FindAsync<BreedingRequest>(breedingRequestId, cancellationToken);
        
        if (entity is null) 
            return false;

        // If the request already completed, don't replay
        if (entity.Status == BreedingRequestStatus.Completed)
        {
            logger.LogInformation("Not replaying BreedingId={Id} because it is already Completed", entity.Id);
            return false;
        }

        // If previously failed, reset to Pending so processors will pick it up.
        var originalStatus = entity.Status;
        var originalFailureReason = entity.FailureReason;
        try
        {
            if (entity.Status == BreedingRequestStatus.Failed)
            {
                entity.Status = BreedingRequestStatus.Pending;
                entity.FailureReason = null;
                entity.ProcessedDate = null;
                entity.UpdatedDate = timeManager.OffsetUtcNow();

                await repository.UpdateAsync(entity, cancellationToken);

                logger.LogInformation("Marked BreedingId={Id} as Pending for replay", entity.Id);
            }

            var msg = new BreedingRequested(entity.Id, entity.SireId, entity.DamId, entity.OwnerId, entity.CreatedDate);

            await messagePublisher.PublishAsync(msg, cancellationToken: cancellationToken);

            logger.LogInformation("Replayed BreedingRequested event for BreedingId={Id}", entity.Id);

            return true;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Replay publishing cancelled for BreedingId={Id}", entity.Id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to replay BreedingRequested event for BreedingId={Id}", entity.Id);

            // Attempt to restore Failed status and record failure reason so it can be retried later
            try
            {
                var bre = await repository.FindAsync<BreedingRequest>(breedingRequestId, cancellationToken);
                if (bre != null)
                {
                    bre.Status = BreedingRequestStatus.Failed;
                    bre.FailureReason = $"Replay publish failed: {ex.Message}";
                    bre.ProcessedDate = timeManager.OffsetUtcNow();
                    bre.UpdatedDate = timeManager.OffsetUtcNow();
                    await repository.UpdateAsync(bre, cancellationToken);
                }
            }
            catch (Exception updEx)
            {
                logger.LogWarning(updEx, "Failed to persist replay-publish-failure metadata for BreedingId={Id}", entity.Id);
            }

            // Restore original status in-memory (no DB change) for calling code if needed
            entity.Status = originalStatus;
            entity.FailureReason = originalFailureReason;

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> ReplayAllNonComplete(int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default)
    {
        if (maxDegreeOfParallelism <= 0) 
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

        cancellationToken.ThrowIfCancellationRequested();

        // Fetch all BreedingRequests that are not Completed
        var requests = await repository.ListAsync<BreedingRequest>(br => br.Status != BreedingRequestStatus.Completed, cancellationToken);

        if (requests == null || requests.Count == 0)
        {
            logger.LogInformation("No non-complete breeding requests found to replay.");
            
            return 0;
        }

        logger.LogInformation("Replaying {Count} non-complete breeding requests (maxConcurrency={Max})", requests.Count, maxDegreeOfParallelism);

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
                    if (r.Status == BreedingRequestStatus.Failed)
                    {
                        try
                        {
                            r.Status = BreedingRequestStatus.Pending;
                            r.FailureReason = null;
                            r.ProcessedDate = null;
                            r.UpdatedDate = timeManager.OffsetUtcNow();
                            await repository.UpdateAsync(r, cancellationToken);
                        }
                        catch (Exception updateEx)
                        {
                            logger.LogWarning(updateEx, "Failed to mark BreedingId={Id} Pending before replay; skipping", r.Id);
                            return;
                        }
                    }

                    var msg = new BreedingRequested(r.Id, r.SireId, r.DamId, r.OwnerId, r.CreatedDate);
                    await messagePublisher.PublishAsync(msg, cancellationToken: cancellationToken);

                    Interlocked.Increment(ref publishedCount);
                    logger.LogInformation("Replayed BreedingRequested for BreedingId={Id}", r.Id);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to replay BreedingRequested for BreedingId={Id}", r.Id);
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

    /// <inheritdoc />
    public async Task<BreedingRequestStatusResult?> GetRequestStatus(Guid breedingRequestId, CancellationToken cancellationToken = default)
    {
        if (breedingRequestId == Guid.Empty) 
            throw new ArgumentException("Invalid id", nameof(breedingRequestId));

        var entity = await repository.FindAsync<BreedingRequest>(breedingRequestId, cancellationToken);

        if (entity == null) 
            return null;

        return new BreedingRequestStatusResult(
            entity.Id,
            entity.Status,
            entity.FoalId,
            entity.OwnerId,
            entity.CreatedDate,
            entity.ProcessedDate,
            entity.UpdatedDate,
            entity.FailureReason
        );
    }

    private async Task<IEnumerable<ParentHorse>> GetParentHorses(string cacheKey, bool isMale)
    {
        IEnumerable<ParentHorse> results;
        var cacheValue = await cache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(cacheValue))
        {
            results = (await GetRandomHorses(new HorseRandomRetiredSpecification(isMale))).ToList();

            await SetCache(cacheKey, results);
        }
        else
        {
            results = JsonSerializer.Deserialize<List<ParentHorse>>(cacheValue)!;
        }

        return results;
    }

    private async Task SetCache(string cacheKey, IEnumerable<ParentHorse> results)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheExpirationMinutes)
        };

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(results), options);
    }

    private async Task<IEnumerable<ParentHorse>> GetRandomHorses(ISpecification<Horse> spec)
    {
        var horses = await repository.ListAsync(spec);

        var results = horses.Select(x => new ParentHorse
        {
            Id = x.Id,
            Name = x.Name,
            Color = x.Color.Name,
            Earnings = x.Earnings,
            RacePlace = x.RacePlace,
            RaceShow = x.RaceShow,
            RaceStarts = x.RaceStarts,
            RaceWins = x.RaceWins
        });

        return results;
    }
}
