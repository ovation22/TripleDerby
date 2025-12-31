using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class TracksController(ITrackService trackService) : ControllerBase
{
    /// <summary>
    /// Returns a paginated and optionally filtered list of tracks.
    /// </summary>
    /// <param name="request">Pagination, sorting and filter parameters (from query string).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 with <see cref="PagedList{TracksResult}"/>; 400 on failure.</returns>
    /// <response code="200">Returns the paged tracks result.</response>
    /// <response code="400">Unable to return tracks.</response>
    [HttpGet]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<TracksResult>>> Filter([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await trackService.Filter(request, cancellationToken);

        return Ok(result);
    }
}
