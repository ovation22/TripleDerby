using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class FeedingsController : ControllerBase
{
    private readonly IFeedingService _feedingService;

    public FeedingsController(
        IFeedingService feedingService
    )
    {
        _feedingService = feedingService;
    }

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
        var result = await _feedingService.GetAll();

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
        var result = await _feedingService.Get(feedingId);

        return Ok(result);
    }

    /// <summary>
    /// Creates a feeding session for a horse using the specified feeding definition.
    /// </summary>
    /// <param name="feedingId">Identifier of the feeding definition to apply.</param>
    /// <param name="horseId">Identifier of the horse to feed.</param>
    /// <returns>200 with <see cref="FeedingSessionResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns the feeding session result.</response>
    /// <response code="400">Unable to create feeding session.</response>
    [HttpPost("{feedingId}/{horseId}")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeedingSessionResult>> Feed(byte feedingId, Guid horseId)
    {
        var result = await _feedingService.Feed(feedingId, horseId);

        return Ok(result);
    }
}
