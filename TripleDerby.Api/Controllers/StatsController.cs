using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel.Horses;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;

    public StatsController(
        IStatsService statsService
    )
    {
        _statsService = statsService;
    }

    /// <summary>
    /// Gets statistics for horses grouped by color.
    /// </summary>
    /// <returns>
    /// 200 with a read-only list of <see cref="HorseColorStats"/> entries (color id, optional name, count and percentage);
    /// 400 on failure.
    /// </returns>
    /// <response code="200">Returns the color statistics.</response>
    /// <response code="400">Unable to return stats.</response>
    [HttpGet("horses/colors")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(typeof(IReadOnlyList<HorseColorStats>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<HorseColorStats>>> Get(CancellationToken cancellationToken)
    {
        var result = await _statsService.GetHorseColorStats(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets statistics for horses grouped by leg type.
    /// </summary>
    /// <returns>
    /// 200 with a read-only list of <see cref="HorseLegTypeStats"/> entries (leg type id, optional name, count and percentage);
    /// 400 on failure.
    /// </returns>
    /// <response code="200">Returns the leg type statistics.</response>
    /// <response code="400">Unable to return stats.</response>
    [HttpGet("horses/legtypes")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(typeof(IReadOnlyList<HorseLegTypeStats>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<HorseLegTypeStats>>> GetLegTypeStats(CancellationToken cancellationToken)
    {
        var result = await _statsService.GetHorseLegTypeStats(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets statistics for horses grouped by gender.
    /// </summary>
    /// <returns>
    /// 200 with a read-only list of <see cref="HorseGenderStats"/> entries (gender flag, optional name, count and percentage);
    /// 400 on failure.
    /// </returns>
    /// <response code="200">Returns the gender statistics.</response>
    /// <response code="400">Unable to return stats.</response>
    [HttpGet("horses/genders")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(typeof(IReadOnlyList<HorseGenderStats>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<HorseGenderStats>>> GetGenderStats(CancellationToken cancellationToken)
    {
        var result = await _statsService.GetHorseGenderStats(cancellationToken);
        return Ok(result);
    }
}
