using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/races/{raceId}/runs")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class RaceRunsController(IRaceService raceService, IRaceRunService raceRunService) : ControllerBase
{
    /// <summary>
    /// Runs a race for a given horse and returns the result.
    /// </summary>
    /// <param name="raceId">Identifier of the race to run.</param>
    /// <param name="horseId">Identifier of the horse participating in the race.</param>
    /// <returns>200 with <see cref="RaceRunResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns race run result.</response>
    /// <response code="400">Unable to run race.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RaceRunResult>> CreateRun([FromRoute] byte raceId, [FromQuery] Guid horseId)
    {
        var result = await raceService.Race(raceId, horseId);

        return Ok(result);
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
