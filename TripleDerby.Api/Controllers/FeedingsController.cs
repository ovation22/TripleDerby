using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class FeedingsController(IFeedingService feedingService) : ControllerBase
{
    /// <summary>
    /// Returns a paged list of feeding definitions/results.
    /// </summary>
    /// <returns>200 with collection of <see cref="FeedingsResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns the feeding definitions.</response>
    /// <response code="400">Unable to return feedings.</response>
    [HttpGet]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<FeedingsResult>>> GetAll()
    {
        var result = await feedingService.GetAll();

        return Ok(result);
    }

    /// <summary>
    /// Returns details for a specific feeding definition.
    /// </summary>
    /// <param name="feedingId">Identifier of the feeding definition.</param>
    /// <returns>200 with <see cref="FeedingResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns the feeding details.</response>
    /// <response code="400">Unable to return feeding.</response>
    [HttpGet("{feedingId}")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeedingResult>> Get(byte feedingId)
    {
        var result = await feedingService.Get(feedingId);

        return Ok(result);
    }

    /// <summary>
    /// Queues a feeding session for async processing.
    /// </summary>
    /// <param name="request">The feeding queue request containing horseId, feedingId, and sessionId.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>202 Accepted with sessionId; 404 if horse/feeding not found.</returns>
    /// <response code="202">Request queued for processing.</response>
    /// <response code="404">Horse or feeding not found.</response>
    [HttpPost("queue")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> QueueFeeding([FromBody] FeedingQueueRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        await feedingService.QueueFeedingAsync(
            request.HorseId,
            request.FeedingId,
            request.SessionId,
            userId,
            cancellationToken);

        var requestUrl = Url.Action("GetRequest", new { id = request.SessionId });
        return Accepted(requestUrl, new { sessionId = request.SessionId, status = "queued" });
    }

    /// <summary>
    /// Gets the status of a feeding request.
    /// </summary>
    /// <param name="id">The sessionId of the feeding request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with status; 404 if not found.</returns>
    [HttpGet("request/{id}")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedingRequestStatusResult>> GetRequest(Guid id, CancellationToken cancellationToken)
    {
        var status = await feedingService.GetRequestStatus(id, cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Gets available daily feeding options for a horse (3 random options).
    /// </summary>
    /// <param name="horseId">The ID of the horse.</param>
    /// <param name="sessionId">The session ID for this request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with feeding options; 404 if horse not found.</returns>
    [HttpGet("options")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<FeedingOptionResult>>> GetOptions([FromQuery] Guid horseId, [FromQuery] Guid sessionId, CancellationToken cancellationToken)
    {
        var options = await feedingService.GetFeedingOptions(horseId, sessionId, cancellationToken);
        return Ok(options);
    }

    /// <summary>
    /// Gets paginated feeding history for a horse with optional filtering and sorting.
    /// </summary>
    /// <param name="horseId">The ID of the horse.</param>
    /// <param name="request">Pagination request with page, size, sorting, and filtering options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with paginated feeding history; 404 if horse not found.</returns>
    [HttpGet("history")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedList<FeedingHistoryResult>>> GetHistory(
        [FromQuery] Guid horseId,
        [FromQuery] PaginationRequest request,
        CancellationToken cancellationToken = default)
    {
        var history = await feedingService.GetFeedingHistory(horseId, request, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Gets the details of a completed feeding session.
    /// </summary>
    /// <param name="sessionId">The ID of the feeding session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with session details; 404 if not found.</returns>
    [HttpGet("session/{sessionId}")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedingSessionResult>> GetSession(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var result = await feedingService.GetFeedingSessionResult(sessionId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Replay a feeding request by republishing the message.
    /// TODO: This is an operational/admin action; consider requiring authorization.
    /// </summary>
    /// <param name="id">The ID of the feeding request to replay.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 202 Accepted when the request is accepted for processing; 404 if the request was not found.
    /// </returns>
    /// <response code="202">Replay accepted.</response>
    /// <response code="404">Feeding request not found.</response>
    [HttpPost("requests/{id:guid}/replay")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplayRequest(Guid id, CancellationToken cancellationToken = default)
    {
        var published = await feedingService.ReplayFeedingRequest(id, cancellationToken);

        if (!published)
            return NotFound();

        return Accepted();
    }

    /// <summary>
    /// Replay all non-complete feeding requests.
    /// TODO: This is an admin/operational endpoint and should be protected in production.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Maximum concurrent publish tasks to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>202 Accepted with number of messages published.</returns>
    [HttpPost("requests/replay-all")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<ActionResult> ReplayAll(
        [FromQuery] int maxDegreeOfParallelism = 10,
        CancellationToken cancellationToken = default)
    {
        var published = await feedingService.ReplayAllNonComplete(maxDegreeOfParallelism, cancellationToken);

        return Accepted(new { published });
    }
}

/// <summary>
/// Request model for queuing a feeding session.
/// </summary>
public record FeedingQueueRequest(Guid HorseId, byte FeedingId, Guid SessionId);
