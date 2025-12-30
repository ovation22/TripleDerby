using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class RacesController(IRaceService raceService) : ControllerBase
{
    /// <summary>
    /// Returns a paginated and optionally filtered list of races.
    /// </summary>
    /// <param name="request">Pagination, sorting and filter parameters (from query string).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with <see cref="PagedList{RacesResult}"/>; 400 on failure.</returns>
    /// <response code="200">Returns the paged races result.</response>
    /// <response code="400">Unable to return races.</response>
    [HttpGet]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<RacesResult>>> Filter([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await raceService.Filter(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns details for a specific race.
    /// </summary>
    /// <param name="id">Race identifier.</param>
    /// <returns>200 with <see cref="RaceResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns the race details.</response>
    /// <response code="400">Unable to return race.</response>
    [HttpGet("{id}")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RaceResult>> Get(byte id)
    {
        var result = await raceService.Get(id);

        return Ok(result);
    }
}
