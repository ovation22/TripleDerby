using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Feeding.Abstractions;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Feeding;

/// <summary>
/// Processes feeding requests by delegating to the FeedingExecutor.
/// </summary>
public class FeedingRequestProcessor(
    IFeedingExecutor feedingExecutor,
    ILogger<FeedingRequestProcessor> logger,
    ITripleDerbyRepository repository,
    IMessagePublisher messagePublisher,
    ITimeManager timeManager)
    : IFeedingRequestProcessor
{
    public async Task<MessageProcessingResult> ProcessAsync(FeedingRequested request, MessageContext context)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var cancellationToken = context.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Processing feeding request for horse {HorseId} with feeding {FeedingId} (SessionId: {SessionId})",
            request.HorseId, request.FeedingId, request.SessionId);

        try
        {
            var stored = await repository.FindAsync<FeedingRequest>(request.SessionId, cancellationToken);
            if (stored is null)
            {
                logger.LogWarning("FeedingRequest {SessionId} not found in DB; skipping", request.SessionId);
                return MessageProcessingResult.Succeeded();
            }

            if (stored.Status == FeedingRequestStatus.Completed)
            {
                logger.LogInformation("Skipping request {SessionId} because status is {Status}", request.SessionId, stored.Status);
                return MessageProcessingResult.Succeeded();
            }

            if (stored.Status == FeedingRequestStatus.Failed)
            {
                logger.LogInformation("Reprocessing failed FeedingRequest {SessionId}. Previous failure: {FailureReason}",
                    request.SessionId, stored.FailureReason);
            }

            if (stored.Status == FeedingRequestStatus.InProgress)
            {
                logger.LogInformation("Skipping request {SessionId} because it is already InProgress", request.SessionId);
                return MessageProcessingResult.Succeeded();
            }

            try
            {
                stored.Status = FeedingRequestStatus.InProgress;
                stored.UpdatedDate = timeManager.OffsetUtcNow();
                await repository.UpdateAsync(stored, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to claim FeedingRequest {SessionId} for processing; another worker may have claimed it",
                    request.SessionId);

                stored = await repository.FindAsync<FeedingRequest>(request.SessionId, cancellationToken);
                if (stored is null)
                {
                    logger.LogWarning("FeedingRequest {SessionId} not found after failed claim; skipping", request.SessionId);
                    return MessageProcessingResult.Succeeded();
                }

                if (stored.Status != FeedingRequestStatus.InProgress)
                {
                    logger.LogInformation("Request {SessionId} is no longer available for processing (status={Status}), skipping",
                        request.SessionId, stored.Status);
                    return MessageProcessingResult.Succeeded();
                }
            }

            await Feed(request, stored, cancellationToken);

            logger.LogInformation("Completed processing feeding request {SessionId}", request.SessionId);
            return MessageProcessingResult.Succeeded();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process feeding request {SessionId}", request.SessionId);
            return MessageProcessingResult.FailedWithException(ex, requeue: false);
        }
    }

    private async Task Feed(FeedingRequested request, FeedingRequest feedingRequestEntity, CancellationToken cancellationToken)
    {
        try
        {
            var result = await repository.ExecuteInTransactionAsync(async () =>
            {
                var feedingResult = await feedingExecutor.ExecuteFeedingAsync(
                    request.HorseId,
                    request.FeedingId,
                    cancellationToken);

                feedingRequestEntity.FeedingSessionId = feedingResult.SessionId;
                feedingRequestEntity.Status = FeedingRequestStatus.Completed;
                feedingRequestEntity.ProcessedDate = timeManager.OffsetUtcNow();
                feedingRequestEntity.UpdatedDate = timeManager.OffsetUtcNow();
                await repository.UpdateAsync(feedingRequestEntity, cancellationToken);

                return feedingResult;
            }, cancellationToken);

            var completedEvent = new FeedingCompleted(
                request.RequestId,
                request.HorseId,
                request.FeedingId,
                request.SessionId,
                request.OwnerId,
                result.SessionId,
                timeManager.OffsetUtcNow()
            );

            try
            {
                await messagePublisher.PublishAsync(completedEvent, cancellationToken: cancellationToken);
            }
            catch (Exception pubEx)
            {
                logger.LogError(pubEx, "Failed to publish FeedingCompleted for SessionId={SessionId}", request.SessionId);
                try
                {
                    var fre = await repository.FindAsync<FeedingRequest>(request.SessionId, cancellationToken);
                    if (fre != null)
                    {
                        fre.FailureReason = $"Publish failed: {pubEx.Message}";
                        fre.ProcessedDate = timeManager.OffsetUtcNow();
                        fre.UpdatedDate = timeManager.OffsetUtcNow();
                        await repository.UpdateAsync(fre, cancellationToken);
                    }
                }
                catch (Exception updEx)
                {
                    logger.LogWarning(updEx, "Failed to persist publish-failure metadata for SessionId={SessionId}", request.SessionId);
                }

                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Feeding operation failed for SessionId={SessionId}", request.SessionId);

            try
            {
                var fre = await repository.FindAsync<FeedingRequest>(request.SessionId, cancellationToken);
                if (fre != null)
                {
                    fre.Status = FeedingRequestStatus.Failed;
                    fre.FailureReason = ex.Message;
                    fre.ProcessedDate = timeManager.OffsetUtcNow();
                    fre.UpdatedDate = timeManager.OffsetUtcNow();
                    await repository.UpdateAsync(fre, cancellationToken);
                }
            }
            catch (Exception updEx)
            {
                logger.LogWarning(updEx, "Failed to mark FeedingRequest as failed for SessionId={SessionId}", request.SessionId);
            }

            throw;
        }
    }
}
