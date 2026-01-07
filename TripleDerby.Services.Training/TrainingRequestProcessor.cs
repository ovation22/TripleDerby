using Microsoft.Extensions.Logging;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Training.Abstractions;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Training;

/// <summary>
/// Processes training requests by delegating to the TrainingExecutor.
/// </summary>
public class TrainingRequestProcessor(
    ITrainingExecutor trainingExecutor,
    ILogger<TrainingRequestProcessor> logger,
    ITripleDerbyRepository repository,
    IMessagePublisher messagePublisher,
    ITimeManager timeManager)
    : ITrainingRequestProcessor
{
    public async Task<MessageProcessingResult> ProcessAsync(TrainingRequested request, MessageContext context)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var cancellationToken = context.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Processing training request for horse {HorseId} with training {TrainingId} (SessionId: {SessionId})",
            request.HorseId, request.TrainingId, request.SessionId);

        try
        {
            var stored = await repository.FindAsync<TrainingRequest>(request.SessionId, cancellationToken);
            if (stored is null)
            {
                logger.LogWarning("TrainingRequest {SessionId} not found in DB; skipping", request.SessionId);
                return MessageProcessingResult.Succeeded();
            }

            if (stored.Status == TrainingRequestStatus.Completed)
            {
                logger.LogInformation("Skipping request {SessionId} because status is {Status}", request.SessionId, stored.Status);
                return MessageProcessingResult.Succeeded();
            }

            if (stored.Status == TrainingRequestStatus.Failed)
            {
                logger.LogInformation("Reprocessing failed TrainingRequest {SessionId}. Previous failure: {FailureReason}",
                    request.SessionId, stored.FailureReason);
            }

            if (stored.Status == TrainingRequestStatus.InProgress)
            {
                logger.LogInformation("Skipping request {SessionId} because it is already InProgress", request.SessionId);
                return MessageProcessingResult.Succeeded();
            }

            try
            {
                stored.Status = TrainingRequestStatus.InProgress;
                stored.UpdatedDate = timeManager.OffsetUtcNow();
                await repository.UpdateAsync(stored, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to claim TrainingRequest {SessionId} for processing; another worker may have claimed it",
                    request.SessionId);

                stored = await repository.FindAsync<TrainingRequest>(request.SessionId, cancellationToken);
                if (stored is null)
                {
                    logger.LogWarning("TrainingRequest {SessionId} not found after failed claim; skipping", request.SessionId);
                    return MessageProcessingResult.Succeeded();
                }

                if (stored.Status != TrainingRequestStatus.InProgress)
                {
                    logger.LogInformation("Request {SessionId} is no longer available for processing (status={Status}), skipping",
                        request.SessionId, stored.Status);
                    return MessageProcessingResult.Succeeded();
                }
            }

            await Train(request, stored, cancellationToken);

            logger.LogInformation("Completed processing training request {SessionId}", request.SessionId);
            return MessageProcessingResult.Succeeded();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process training request {SessionId}", request.SessionId);
            return MessageProcessingResult.FailedWithException(ex, requeue: false);
        }
    }

    private async Task Train(TrainingRequested request, TrainingRequest trainingRequestEntity, CancellationToken cancellationToken)
    {
        try
        {
            var result = await repository.ExecuteInTransactionAsync(async () =>
            {
                var trainingResult = await trainingExecutor.ExecuteTrainingAsync(
                    request.HorseId,
                    request.TrainingId,
                    cancellationToken);

                trainingRequestEntity.TrainingSessionId = trainingResult.SessionId;
                trainingRequestEntity.Status = TrainingRequestStatus.Completed;
                trainingRequestEntity.ProcessedDate = timeManager.OffsetUtcNow();
                trainingRequestEntity.UpdatedDate = timeManager.OffsetUtcNow();
                await repository.UpdateAsync(trainingRequestEntity, cancellationToken);

                return trainingResult;
            }, cancellationToken);

            var completedEvent = new TrainingCompleted(
                request.RequestId,
                request.HorseId,
                request.TrainingId,
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
                logger.LogError(pubEx, "Failed to publish TrainingCompleted for SessionId={SessionId}", request.SessionId);
                try
                {
                    var tre = await repository.FindAsync<TrainingRequest>(request.SessionId, cancellationToken);
                    if (tre != null)
                    {
                        tre.FailureReason = $"Publish failed: {pubEx.Message}";
                        tre.ProcessedDate = timeManager.OffsetUtcNow();
                        tre.UpdatedDate = timeManager.OffsetUtcNow();
                        await repository.UpdateAsync(tre, cancellationToken);
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
            logger.LogError(ex, "Training operation failed for SessionId={SessionId}", request.SessionId);

            try
            {
                var tre = await repository.FindAsync<TrainingRequest>(request.SessionId, cancellationToken);
                if (tre != null)
                {
                    tre.Status = TrainingRequestStatus.Failed;
                    tre.FailureReason = ex.Message;
                    tre.ProcessedDate = timeManager.OffsetUtcNow();
                    tre.UpdatedDate = timeManager.OffsetUtcNow();
                    await repository.UpdateAsync(tre, cancellationToken);
                }
            }
            catch (Exception updEx)
            {
                logger.LogWarning(updEx, "Failed to mark TrainingRequest as failed for SessionId={SessionId}", request.SessionId);
            }

            throw;
        }
    }
}
