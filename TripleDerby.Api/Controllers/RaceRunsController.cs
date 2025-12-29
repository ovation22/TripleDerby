using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/races/{raceId}/runs")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class RaceRunsController(
    IRaceService raceService,
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
    public async Task<ActionResult<Resource<RaceRequestStatusResult>>> CreateRun(
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

        // Build canonical request URL for polling
        var requestUrl = Url.Action(nameof(GetRequestStatus), "RaceRuns", new { raceId, requestId = result.Id }, Request.Scheme)
                         ?? $"/api/races/{raceId}/runs/requests/{result.Id}";

        // HATEOAS links
        var links = new List<Link>
        {
            new("self", requestUrl, "GET"),
            new("replay", Url.Action(nameof(Replay), "RaceRuns", new { raceId, raceRequestId = result.Id }, Request.Scheme) ?? $"/api/races/{raceId}/runs/requests/{result.Id}/replay", "POST")
        };

        var resource = new Resource<RaceRequestStatusResult>(result, links);

        return AcceptedAtAction(nameof(GetRequestStatus), new { raceId, requestId = result.Id }, resource);
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
    public async Task<ActionResult<Resource<RaceRequestStatusResult>>> GetRequestStatus(
        [FromRoute] byte raceId,
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var result = await raceService.GetRequestStatusAsync(raceId, requestId, cancellationToken);

        if (result == null)
            return NotFound();

        var links = new List<Link>
        {
            new("self", Url.Action(nameof(GetRequestStatus), "RaceRuns", new { raceId, requestId }, Request.Scheme) ?? $"/api/races/{raceId}/runs/requests/{requestId}", "GET")
        };

        if (result.Status == RaceRequestStatus.Failed)
        {
            links.Add(new Link("replay", Url.Action(nameof(Replay), "RaceRuns", new { raceId, raceRequestId = requestId }, Request.Scheme) ?? $"/api/races/{raceId}/runs/requests/{requestId}/replay", "POST"));
        }

        if (result.RaceRunId.HasValue)
        {
            var raceRunHref = Url.Action(nameof(GetRun), "RaceRuns", new { raceId, runId = result.RaceRunId.Value }, Request.Scheme) ?? $"/api/races/{raceId}/runs/{result.RaceRunId.Value}";
            links.Add(new Link("result", raceRunHref, "GET"));
        }

        var resource = new Resource<RaceRequestStatusResult>(result, links);

        return Ok(resource);
    }

    /// <summary>
    /// Replay a race request by publishing the RaceRequested message again.
    /// TODO: This is an operational/admin action; consider requiring authorization.
    /// </summary>
    /// <param name="raceId">Race identifier.</param>
    /// <param name="raceRequestId">The Id of the race request to replay.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// 202 Accepted when the request is accepted for processing; 404 if the request was not found.
    /// </returns>
    /// <response code="202">Replay accepted.</response>
    /// <response code="404">Race request not found.</response>
    [HttpPost("requests/{raceRequestId:guid}/replay")]
    public async Task<IActionResult> Replay(
        [FromRoute] byte raceId,
        [FromRoute] Guid raceRequestId,
        CancellationToken cancellationToken = default)
    {
        var published = await raceService.ReplayRaceRequest(raceRequestId, cancellationToken);

        if (!published)
            return NotFound();

        return Accepted();
    }

    /// <summary>
    /// Replay all non-complete race requests (Pending or Failed) by re-publishing their RaceRequested messages.
    /// TODO: This is an admin/operational endpoint and should be protected in production.
    /// </summary>
    /// <param name="raceId">Race identifier.</param>
    /// <param name="maxDegreeOfParallelism">Maximum concurrent publish tasks to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>202 Accepted with number of messages published.</returns>
    [HttpPost("requests/replay")]
    public async Task<IActionResult> ReplayAll(
        [FromRoute] byte raceId,
        [FromQuery] int maxDegreeOfParallelism = 10,
        CancellationToken cancellationToken = default)
    {
        var published = await raceService.ReplayAllNonComplete(maxDegreeOfParallelism, cancellationToken);

        return Accepted(new { published });
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
