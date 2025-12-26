using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Racing;

/// <summary>
/// Processes race requests by delegating to the RaceService.
/// </summary>
public class RaceRequestProcessor : IRaceRequestProcessor
{
    private readonly IRaceService _raceService;
    private readonly ILogger<RaceRequestProcessor> _logger;

    public RaceRequestProcessor(
        IRaceService raceService,
        ILogger<RaceRequestProcessor> logger)
    {
        _raceService = raceService;
        _logger = logger;
    }

    public async Task<RaceRunResult> ProcessAsync(
        RaceRequested request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing race: RaceId={RaceId}, HorseId={HorseId}, CorrelationId={CorrelationId}",
            request.RaceId, request.HorseId, request.CorrelationId);

        // Delegate to existing RaceService.Race() method
        var result = await _raceService.Race(
            request.RaceId,
            request.HorseId,
            cancellationToken);

        _logger.LogInformation(
            "Race completed: Winner={Winner}, Time={Time}, CorrelationId={CorrelationId}",
            result.HorseResults.First().HorseName,
            result.HorseResults.First().Time,
            request.CorrelationId);

        return result;
    }
}
