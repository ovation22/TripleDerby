using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/races/{raceId}/runs")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class RaceRunsController(
    IRaceService raceService,
    ITripleDerbyRepository repository,
    IRaceRunService raceRunService,
    ILogger<RaceRunsController> logger) : ControllerBase
{
    /// <summary>
    /// Submits an asynchronous race request for processing by the Race microservice.
    /// </summary>
    /// <param name="raceId">Identifier of the race to run.</param>
    /// <param name="horseId">Identifier of the horse participating in the race.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>202 Accepted with request ID and status URL.</returns>
    /// <response code="202">Race request accepted and queued for processing.</response>
    /// <response code="400">Invalid request parameters.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RaceRequestResponse>> CreateRun(
        [FromRoute] byte raceId,
        [FromQuery] Guid horseId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Get owner ID from authenticated user context
        var ownerId = Guid.Empty;

        // Delegate to service layer
        var result = await raceService.QueueRaceAsync(raceId, horseId, ownerId, cancellationToken);

        logger.LogInformation(
            "Race request created: CorrelationId={CorrelationId}, RaceId={RaceId}, HorseId={HorseId}",
            result.Id, raceId, horseId);

        // Return 202 Accepted with request details
        var response = new RaceRequestResponse(
            RequestId: result.Id,
            Status: result.Status,
            Message: "Race request accepted and queued for processing",
            StatusUrl: Url.Action(nameof(GetRequestStatus), new { raceId, requestId = result.Id })
        );

        return Accepted(response);
    }

    /// <summary>
    /// Gets the status of a race request.
    /// </summary>
    /// <param name="raceId">Race identifier.</param>
    /// <param name="requestId">Race request identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with request status; 404 if not found.</returns>
    /// <response code="200">Returns request status.</response>
    /// <response code="404">Request not found.</response>
    [HttpGet("requests/{requestId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RaceRequestStatusResponse>> GetRequestStatus(
        [FromRoute] byte raceId,
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await repository.FindAsync<RaceRequest>(requestId, cancellationToken);

        if (request == null || request.RaceId != raceId)
            return NotFound();

        var response = new RaceRequestStatusResponse(
            RequestId: request.Id,
            RaceId: request.RaceId,
            HorseId: request.HorseId,
            Status: request.Status,
            RaceRunId: request.RaceRunId,
            CreatedDate: request.CreatedDate,
            ProcessedDate: request.ProcessedDate,
            FailureReason: request.FailureReason,
            ResultUrl: request.RaceRunId.HasValue
                ? Url.Action(nameof(GetRun), new { raceId, runId = request.RaceRunId })
                : null
        );

        return Ok(response);
    }

    /// <summary>
    /// Gets a paginated list of race runs for a specific race.
    /// </summary>
    /// <param name="raceId">Race identifier.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10, max: 100).</param>
    /// <returns>200 with paginated race run summaries; 404 if race not found.</returns>
    /// <response code="200">Returns paginated race run list.</response>
    /// <response code="404">Race not found.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<RaceRunSummary>>> GetRuns(
        [FromRoute] byte raceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageSize > 100)
            pageSize = 100;

        var result = await raceRunService.GetRaceRuns(raceId, page, pageSize);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Gets detailed results for a specific race run including full play-by-play.
    /// </summary>
    /// <param name="raceId">Race identifier.</param>
    /// <param name="runId">Race run identifier.</param>
    /// <returns>200 with detailed race run result; 404 if not found.</returns>
    /// <response code="200">Returns detailed race run result.</response>
    /// <response code="404">Race run not found.</response>
    [HttpGet("{runId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RaceRunResult>> GetRun(
        [FromRoute] byte raceId,
        [FromRoute] Guid runId)
    {
        var result = await raceRunService.GetRaceRunDetails(raceId, runId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
