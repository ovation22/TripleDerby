using Microsoft.Extensions.Logging;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Services;

public class RaceService(
    ITripleDerbyRepository repository,
    IMessagePublisher messagePublisher,
    ITimeManager timeManager,
    ILogger<RaceService> logger) : IRaceService
{
    public async Task<RaceResult> Get(byte id, CancellationToken cancellationToken = default)
    {
        var spec = new RaceByIdSpecification(id);
        var result = await repository.SingleOrDefaultAsync(spec, cancellationToken);

        if (result == null)
            throw new KeyNotFoundException($"Race with ID {id} not found.");

        return result;
    }

    public async Task<PagedList<RacesResult>> Filter(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var spec = new RaceFilterSpecificationToDto(request);

        return await repository.ListAsync(spec, cancellationToken);
    }

    public async Task<RaceRequestStatusResult> QueueRaceAsync(byte raceId, Guid horseId, Guid ownerId, CancellationToken cancellationToken = default)
    {
        // Generate correlation ID for this request
        var correlationId = Guid.NewGuid();

        // Create RaceRequest entity for tracking
        var raceRequest = new RaceRequest
        {
            Id = correlationId,
            RaceId = raceId,
            HorseId = horseId,
            OwnerId = ownerId,
            Status = RaceRequestStatus.Pending,
            CreatedDate = DateTimeOffset.UtcNow,
            CreatedBy = ownerId
        };

        await repository.CreateAsync(raceRequest, cancellationToken);

        // Publish message to Service Bus
        var message = new RaceRequested
        {
            CorrelationId = correlationId,
            RaceId = raceId,
            HorseId = horseId,
            RequestedBy = ownerId,
            RequestedAt = DateTime.UtcNow
        };

        await messagePublisher.PublishAsync(message, cancellationToken: cancellationToken);

        return new RaceRequestStatusResult(
            Id: correlationId,
            RaceId: raceId,
            HorseId: horseId,
            Status: RaceRequestStatus.Pending,
            RaceRunId: null,
            OwnerId: ownerId,
            CreatedDate: raceRequest.CreatedDate,
            ProcessedDate: null,
            UpdatedDate: null,
            FailureReason: null
        );
    }

    public async Task<RaceRequestStatusResult?> GetRequestStatusAsync(byte raceId, Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await repository.FindAsync<RaceRequest>(requestId, cancellationToken);

        if (request == null || request.RaceId != raceId)
            return null;

        return new RaceRequestStatusResult(
            Id: request.Id,
            RaceId: request.RaceId,
            HorseId: request.HorseId,
            Status: request.Status,
            RaceRunId: request.RaceRunId,
            OwnerId: request.OwnerId,
            CreatedDate: request.CreatedDate,
            ProcessedDate: request.ProcessedDate,
            UpdatedDate: request.UpdatedDate,
            FailureReason: request.FailureReason
        );
    }

    public async Task<bool> ReplayRaceRequest(Guid raceRequestId, CancellationToken cancellationToken = default)
    {
        if (raceRequestId == Guid.Empty)
            throw new ArgumentException("Invalid id", nameof(raceRequestId));

        var entity = await repository.FindAsync<RaceRequest>(raceRequestId, cancellationToken);

        if (entity is null)
            return false;

        // If the request already completed, don't replay
        if (entity.Status == RaceRequestStatus.Completed)
        {
            logger.LogInformation("Not replaying RaceRequestId={Id} because it is already Completed", entity.Id);
            return false;
        }

        // If previously failed, reset to Pending so processors will pick it up.
        var originalStatus = entity.Status;
        var originalFailureReason = entity.FailureReason;
        try
        {
            if (entity.Status == RaceRequestStatus.Failed)
            {
                entity.Status = RaceRequestStatus.Pending;
                entity.FailureReason = null;
                entity.ProcessedDate = null;
                entity.UpdatedDate = timeManager.OffsetUtcNow();

                await repository.UpdateAsync(entity, cancellationToken);

                logger.LogInformation("Marked RaceRequestId={Id} as Pending for replay", entity.Id);
            }

            var msg = new RaceRequested
            {
                CorrelationId = entity.Id,
                RaceId = entity.RaceId,
                HorseId = entity.HorseId,
                RequestedBy = entity.OwnerId,
                RequestedAt = entity.CreatedDate.DateTime
            };

            await messagePublisher.PublishAsync(msg, cancellationToken: cancellationToken);

            logger.LogInformation("Replayed RaceRequested event for RaceRequestId={Id}", entity.Id);

            return true;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Replay publishing cancelled for RaceRequestId={Id}", entity.Id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to replay RaceRequested event for RaceRequestId={Id}", entity.Id);

            // Attempt to restore Failed status and record failure reason so it can be retried later
            try
            {
                var rr = await repository.FindAsync<RaceRequest>(raceRequestId, cancellationToken);
                if (rr != null)
                {
                    rr.Status = RaceRequestStatus.Failed;
                    rr.FailureReason = $"Replay publish failed: {ex.Message}";
                    rr.ProcessedDate = timeManager.OffsetUtcNow();
                    rr.UpdatedDate = timeManager.OffsetUtcNow();
                    await repository.UpdateAsync(rr, cancellationToken);
                }
            }
            catch (Exception updEx)
            {
                logger.LogWarning(updEx, "Failed to persist replay-publish-failure metadata for RaceRequestId={Id}", entity.Id);
            }

            // Restore original status in-memory (no DB change) for calling code if needed
            entity.Status = originalStatus;
            entity.FailureReason = originalFailureReason;

            throw;
        }
    }

    public async Task<int> ReplayAllNonComplete(int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default)
    {
        if (maxDegreeOfParallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

        cancellationToken.ThrowIfCancellationRequested();

        // Fetch all RaceRequests that are not Completed
        var requests = await repository.ListAsync<RaceRequest>(rr => rr.Status != RaceRequestStatus.Completed, cancellationToken);

        if (requests == null || requests.Count == 0)
        {
            logger.LogInformation("No non-complete race requests found to replay.");
            return 0;
        }

        logger.LogInformation("Replaying {Count} non-complete race requests (maxConcurrency={Max})", requests.Count, maxDegreeOfParallelism);

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
                    if (r.Status == RaceRequestStatus.Failed)
                    {
                        try
                        {
                            r.Status = RaceRequestStatus.Pending;
                            r.FailureReason = null;
                            r.ProcessedDate = null;
                            r.UpdatedDate = timeManager.OffsetUtcNow();
                            await repository.UpdateAsync(r, cancellationToken);
                        }
                        catch (Exception updateEx)
                        {
                            logger.LogWarning(updateEx, "Failed to mark RaceRequestId={Id} Pending before replay; skipping", r.Id);
                            return;
                        }
                    }

                    var msg = new RaceRequested
                    {
                        CorrelationId = r.Id,
                        RaceId = r.RaceId,
                        HorseId = r.HorseId,
                        RequestedBy = r.OwnerId,
                        RequestedAt = r.CreatedDate.DateTime
                    };

                    await messagePublisher.PublishAsync(msg, cancellationToken: cancellationToken);

                    Interlocked.Increment(ref publishedCount);
                    logger.LogInformation("Replayed RaceRequested for RaceRequestId={Id}", r.Id);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to replay RaceRequested for RaceRequestId={Id}", r.Id);
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
}
