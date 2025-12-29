using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Racing;

/// <summary>
/// Processes race requests by delegating to the RaceService.
/// </summary>
public class RaceRequestProcessor : IRaceRequestProcessor
{
    private readonly IRaceService _raceService;
    private readonly ITripleDerbyRepository _repository;
    private readonly ILogger<RaceRequestProcessor> _logger;

    public RaceRequestProcessor(
        IRaceService raceService,
        ITripleDerbyRepository repository,
        ILogger<RaceRequestProcessor> logger)
    {
        _raceService = raceService;
        _repository = repository;
        _logger = logger;
    }

    public async Task<RaceRunResult> ProcessAsync(
        RaceRequested request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing race: RaceId={RaceId}, HorseId={HorseId}, CorrelationId={CorrelationId}",
            request.RaceId, request.HorseId, request.CorrelationId);

        // Update RaceRequest status to InProgress
        var raceRequest = await _repository.FindAsync<RaceRequest>(request.CorrelationId, cancellationToken);
        if (raceRequest != null)
        {
            raceRequest.Status = RaceRequestStatus.InProgress;
            raceRequest.UpdatedDate = DateTimeOffset.UtcNow;
            await _repository.UpdateAsync(raceRequest, cancellationToken);
        }

        try
        {
            // Delegate to existing RaceService.Race() method
            var result = await _raceService.Race(
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
                await _repository.UpdateAsync(raceRequest, cancellationToken);
            }

            _logger.LogInformation(
                "Race completed: Winner={Winner}, Time={Time}, CorrelationId={CorrelationId}",
                result.HorseResults.First().HorseName,
                result.HorseResults.First().Time,
                request.CorrelationId);

            return result;
        }
        catch (Exception ex)
        {
            // Update RaceRequest with failure
            if (raceRequest != null)
            {
                raceRequest.Status = RaceRequestStatus.Failed;
                raceRequest.FailureReason = ex.Message;
                raceRequest.ProcessedDate = DateTimeOffset.UtcNow;
                raceRequest.UpdatedDate = DateTimeOffset.UtcNow;
                await _repository.UpdateAsync(raceRequest, cancellationToken);
            }

            _logger.LogError(ex,
                "Race processing failed: CorrelationId={CorrelationId}",
                request.CorrelationId);

            throw;
        }
    }
}
