using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Breeding;

/// <summary>
/// Processes breeding requests by delegating to the BreedingExecutor.
/// </summary>
public class BreedingRequestProcessor(
    IBreedingExecutor breedingExecutor,
    ILogger<BreedingRequestProcessor> logger,
    ITripleDerbyRepository repository,
    IMessagePublisher messagePublisher,
    ITimeManager timeManager)
    : IBreedingRequestProcessor
{
    public async Task<MessageProcessingResult> ProcessAsync(BreedingRequested request, MessageContext context)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var cancellationToken = context.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Processing breeding request {RequestId} (sire={SireId}, dam={DamId})", request.RequestId, request.SireId, request.DamId);

        try
        {
            // idempotency: load persisted request
            var stored = await repository.FindAsync<BreedingRequest>(request.RequestId, cancellationToken);
            if (stored is null)
            {
                logger.LogWarning("BreedingRequest {RequestId} not found in DB; skipping", request.RequestId);
                return MessageProcessingResult.Succeeded();
            }

            // If already completed, skip
            if (stored.Status == BreedingRequestStatus.Completed)
            {
                logger.LogInformation("Skipping request {RequestId} because status is {Status}", request.RequestId, stored.Status);
                return MessageProcessingResult.Succeeded();
            }

            // If previously failed, allow replay – log and proceed to claim
            if (stored.Status == BreedingRequestStatus.Failed)
            {
                logger.LogInformation("Reprocessing failed BreedingRequest {RequestId}. Previous failure: {FailureReason}", request.RequestId, stored.FailureReason);
            }

            // If already in progress, skip to avoid concurrent processing
            if (stored.Status == BreedingRequestStatus.InProgress)
            {
                logger.LogInformation("Skipping request {RequestId} because it is already InProgress", request.RequestId);
                return MessageProcessingResult.Succeeded();
            }

            // Claim the request so other workers won't process it concurrently.
            try
            {
                stored.Status = BreedingRequestStatus.InProgress;
                stored.UpdatedDate = timeManager.OffsetUtcNow();
                await repository.UpdateAsync(stored, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to claim BreedingRequest {RequestId} for processing; another worker may have claimed it", request.RequestId);
                // reload and re-check status
                stored = await repository.FindAsync<BreedingRequest>(request.RequestId, cancellationToken);
                if (stored is null)
                {
                    logger.LogWarning("BreedingRequest {RequestId} not found after failed claim; skipping", request.RequestId);
                    return MessageProcessingResult.Succeeded();
                }

                if (stored.Status != BreedingRequestStatus.InProgress)
                {
                    logger.LogInformation("Request {RequestId} is no longer available for processing (status={Status}), skipping", request.RequestId, stored.Status);
                    return MessageProcessingResult.Succeeded();
                }

                // if we couldn't claim, and it's still not in progress, proceed – a later duplicate check in DB will avoid double side effects in most cases.
            }

            await Breed(request, stored, cancellationToken);

            logger.LogInformation("Completed processing breeding request {RequestId}", request.RequestId);
            return MessageProcessingResult.Succeeded();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process breeding request {RequestId}", request.RequestId);
            return MessageProcessingResult.FailedWithException(ex, requeue: false);
        }
    }

    private async Task Breed(BreedingRequested request, BreedingRequest breedingRequestEntity, CancellationToken cancellationToken)
    {
        try
        {
            // Execute breeding logic and create foal, update parent counters, and update BreedingRequest atomically
            var result = await repository.ExecuteInTransactionAsync(async () =>
            {
                // Delegate to BreedingExecutor for core breeding logic
                var breedingResult = await breedingExecutor.Breed(
                    request.SireId,
                    request.DamId,
                    request.OwnerId,
                    cancellationToken);

                // Update parent counters
                await repository.UpdateParentedAsync(request.SireId, request.DamId, cancellationToken);

                // Update the persisted BreedingRequest with FoalId, status and processed date as part of the same transaction
                // Use the entity passed from ProcessAsync to avoid redundant DB query
                breedingRequestEntity.FoalId = breedingResult.FoalId;
                breedingRequestEntity.Status = BreedingRequestStatus.Completed;
                breedingRequestEntity.ProcessedDate = timeManager.OffsetUtcNow();
                breedingRequestEntity.UpdatedDate = timeManager.OffsetUtcNow();
                await repository.UpdateAsync(breedingRequestEntity, cancellationToken);

                return breedingResult;
            }, cancellationToken);

            // publish BreedingCompleted AFTER transaction commit
            var completedEvent = new BreedingCompleted(
                request.RequestId,
                request.SireId,
                request.DamId,
                result.FoalId,
                request.OwnerId,
                timeManager.OffsetUtcNow()
            );

            try
            {
                await messagePublisher.PublishAsync(completedEvent, cancellationToken: cancellationToken);
            }
            catch (Exception pubEx)
            {
                // Log and persist failure info, but keep Status = Completed so DB reflects foal existence.
                logger.LogError(pubEx, "Failed to publish BreedingCompleted for BreedingId={RequestId}", request.RequestId);
                try
                {
                    var bre = await repository.FindAsync<BreedingRequest>(request.RequestId, cancellationToken);
                    if (bre != null)
                    {
                        // Keep Status = Completed (foal created). Record publish failure details for reconciliation.
                        bre.FailureReason = $"Publish failed: {pubEx.Message}";
                        bre.ProcessedDate = timeManager.OffsetUtcNow();
                        bre.UpdatedDate = timeManager.OffsetUtcNow();
                        await repository.UpdateAsync(bre, cancellationToken);
                    }
                }
                catch (Exception updEx)
                {
                    logger.LogWarning(updEx, "Failed to persist publish-failure metadata for BreedingId={RequestId}", request.RequestId);
                }

                // decide whether to rethrow or swallow – here we rethrow so caller / monitoring sees the failure
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Breeding operation failed for RequestId={RequestId}", request.RequestId);

            try
            {
                var bre = await repository.FindAsync<BreedingRequest>(request.RequestId, cancellationToken);
                if (bre != null)
                {
                    bre.Status = BreedingRequestStatus.Failed;
                    bre.FailureReason = ex.Message;
                    bre.ProcessedDate = timeManager.OffsetUtcNow();
                    bre.UpdatedDate = timeManager.OffsetUtcNow();
                    await repository.UpdateAsync(bre, cancellationToken);
                }
            }
            catch (Exception updEx)
            {
                logger.LogWarning(updEx, "Failed to mark BreedingRequest as failed for RequestId={RequestId}", request.RequestId);
            }

            throw;
        }
    }
}
