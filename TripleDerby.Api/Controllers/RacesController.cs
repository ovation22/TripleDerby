using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class RacesController(IRaceService raceService) : ControllerBase
{
    /// <summary>
    /// Returns a list of race definitions/results.
    /// </summary>
    /// <returns>200 with collection of <see cref="RacesResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns the races list.</response>
    /// <response code="400">Unable to return races.</response>
    [HttpGet]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<RacesResult>>> GetAll()
    {
        var result = await raceService.GetAll();

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

    /// <summary>
    /// Runs a race for a given horse and returns the result.
    /// </summary>
    /// <param name="raceId">Identifier of the race to run.</param>
    /// <param name="horseId">Identifier of the horse participating in the race.</param>
    /// <returns>200 with <see cref="RaceRunResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns race run result.</response>
    /// <response code="400">Unable to run race.</response>
    [HttpPost("{raceId}/run")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RaceRunResult>> Race([FromRoute] byte raceId, [FromQuery] Guid horseId)
    {
        var result = await raceService.Race(raceId, horseId);

        return Ok(result);
    }
}
