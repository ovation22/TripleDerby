using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class TrainingsController : ControllerBase
{
    private readonly ITrainingService _trainingService;

    public TrainingsController(
        ITrainingService trainingService
    )
    {
        _trainingService = trainingService;
    }

    /// <summary>
    /// Returns all training definitions or results.
    /// </summary>
    /// <returns>200 with collection of <see cref="TrainingsResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns the training definitions.</response>
    /// <response code="400">Unable to return trainings.</response>
    [HttpGet]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<TrainingsResult>>> GetAll()
    {
        var result = await _trainingService.GetAll();
        
        return Ok(result);
    }

    /// <summary>
    /// Returns details for a specific training.
    /// </summary>
    /// <param name="trainingId">Identifier of the training.</param>
    /// <returns>200 with <see cref="TrainingResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns the training details.</response>
    /// <response code="400">Unable to return training.</response>
    [HttpGet("{trainingId}")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TrainingResult>> Get(byte trainingId)
    {
        var result = await _trainingService.Get(trainingId);
        
        return Ok(result);
    }

    /// <summary>
    /// Starts a training session for the specified horse using a training definition.
    /// </summary>
    /// <param name="trainingId">Identifier of the training definition to run.</param>
    /// <param name="horseId">Identifier of the horse to train.</param>
    /// <returns>200 with <see cref="TrainingSessionResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns the training session result.</response>
    /// <response code="400">Unable to create training session.</response>
    [HttpPost("{trainingId}/{horseId}")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TrainingSessionResult>> Train(byte trainingId, Guid horseId)
    {
        var result = await _trainingService.Train(trainingId, horseId);
        
        return Ok(result);
    }
}
