using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Racing.Abstractions;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Racing;

/// <summary>
/// Processes race requests by delegating to the RaceExecutor.
/// </summary>
public class RaceRequestProcessor(
    IRaceExecutor raceExecutor,
    ITripleDerbyRepository repository,
    IMessagePublisher messagePublisher,
    ILogger<RaceRequestProcessor> logger)
    : IRaceRequestProcessor
{
    public async Task<MessageProcessingResult> ProcessAsync(
        RaceRequested request,
        MessageContext context)
    {
        var cancellationToken = context.CancellationToken;

        logger.LogInformation(
            "Processing race: RaceId={RaceId}, HorseId={HorseId}, CorrelationId={CorrelationId}",
            request.RaceId, request.HorseId, request.CorrelationId);

        try
        {
            // Update RaceRequest status to InProgress
            var raceRequest = await repository.FindAsync<RaceRequest>(request.CorrelationId, cancellationToken);
            if (raceRequest != null)
            {
                raceRequest.Status = RaceRequestStatus.InProgress;
                raceRequest.UpdatedDate = DateTimeOffset.UtcNow;
                await repository.UpdateAsync(raceRequest, cancellationToken);
            }

            // Delegate to RaceExecutor
            var result = await raceExecutor.Race(
                request.RaceId,
                request.HorseId,
                cancellationToken);

            // Update RaceRequest with successful result
            if (raceRequest != null)
            {
                raceRequest.Status = RaceRequestStatus.Completed;
                raceRequest.RaceRunId = result.RaceRunId;
                raceRequest.ProcessedDate = DateTimeOffset.UtcNow;
                raceRequest.UpdatedDate = DateTimeOffset.UtcNow;
                await repository.UpdateAsync(raceRequest, cancellationToken);
            }

            // Publish completion message
            var completion = new RaceCompleted
            {
                CorrelationId = request.CorrelationId,
                RaceRunId = result.RaceRunId,
                RaceId = request.RaceId,
                RaceName = result.RaceName,
                WinnerHorseId = result.HorseResults.First().HorseId,
                WinnerName = result.HorseResults.First().HorseName,
                WinnerTime = result.HorseResults.First().Time,
                FieldSize = result.HorseResults.Count,
                Result = result
            };

            await messagePublisher.PublishAsync(
                completion,
                new MessagePublishOptions { Destination = "race-completions" },
                cancellationToken);

            logger.LogInformation(
                "Race completed: Winner={Winner}, Time={Time}, CorrelationId={CorrelationId}",
                result.HorseResults.First().HorseName,
                result.HorseResults.First().Time,
                request.CorrelationId);

            return MessageProcessingResult.Succeeded();
        }
        catch (Exception ex)
        {
            // Update RaceRequest with failure
            var raceRequest = await repository.FindAsync<RaceRequest>(request.CorrelationId, cancellationToken);
            if (raceRequest != null)
            {
                raceRequest.Status = RaceRequestStatus.Failed;
                raceRequest.FailureReason = ex.Message;
                raceRequest.ProcessedDate = DateTimeOffset.UtcNow;
                raceRequest.UpdatedDate = DateTimeOffset.UtcNow;
                await repository.UpdateAsync(raceRequest, cancellationToken);
            }

            logger.LogError(ex,
                "Race processing failed: CorrelationId={CorrelationId}",
                request.CorrelationId);

            return MessageProcessingResult.FailedWithException(ex, requeue: false);
        }
    }
}
