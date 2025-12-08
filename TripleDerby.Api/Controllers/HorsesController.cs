using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class HorsesController : ControllerBase
{
    private readonly IHorseService _horseService;

    public HorsesController(
        IHorseService horseService
    )
    {
        _horseService = horseService;
    }

    /// <summary>
    /// Returns a paginated and optionally filtered list of horses.
    /// </summary>
    /// <param name="request">Pagination, sorting and filter parameters (from query string).</param>
    /// <returns>200 with <see cref="HorsesResult"/>; 400 on failure.</returns>
    /// <remarks>
    /// Example URLs:
    /// <code>
    /// /api/horses?page=1&size=10&Filters[Horse].Operator=Contains&Filters[Horse].Value=Admiral
    /// /api/horses?page=1&size=10&Filters[Color].Operator=Contains&Filters[Color].Value=Brown
    /// /api/horses?page=1&size=10&Filters[RaceStarts].Operator=Gt&Filters[RaceStarts].Value=3
    /// /api/horses?page=1&size=10&Operator=And&Filters[RaceStarts].Operator=Gt&Filters[RaceStarts].Value=3&Filters[Earnings].Operator=Gt&Filters[Earnings].Value=100000
    /// /api/horses?page=1&size=10&Operator=Or&Filters[RaceStarts].Operator=Gt&Filters[RaceStarts].Value=3&Filters[Earnings].Operator=Gt&Filters[Earnings].Value=100000
    /// </code>
    /// </remarks>
    /// <response code="200">Returns the paged horses result.</response>
    /// <response code="400">Unable to return horses.</response>
    [HttpGet]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<HorseResult>>> Filter([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await _horseService.Filter(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns details for a single horse.
    /// </summary>
    /// <param name="id">Horse identifier (GUID).</param>
    /// <returns>200 with <see cref="HorseResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns the horse details.</response>
    /// <response code="400">Unable to return horse.</response>
    [HttpGet("{id}")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HorseResult>> Get(Guid id)
    {
        var result = await _horseService.Get(id);

        return Ok(result);
    }

    /// <summary>
    /// Applies a JSON Patch (RFC 6902) document to a horse resource.
    /// </summary>
    /// <remarks>
    /// The request must use the media type <c>application/json-patch+json</c>.
    /// Example:
    /// <code>
    /// [ { "op": "replace", "path": "/name", "value": "NewName" } ]
    /// </code>
    /// </remarks>
    /// <param name="id">Identifier of the horse to patch.</param>
    /// <param name="patch">JSON Patch document describing changes.</param>
    /// <returns>204 on success; 400 for invalid patch or failure.</returns>
    /// <response code="204">Patch applied successfully (no content).</response>
    /// <response code="400">Invalid patch document or unable to update horse.</response>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult> Patch(Guid id, [FromBody] JsonPatchDocument<HorsePatch> patch)
    {
        try
        {
            await _horseService.Update(id, patch);

            return new NoContentResult();
        }
        catch (JsonPatchException)
        {
            return BadRequest("Invalid patch");
        }
    }
}
