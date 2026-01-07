using Microsoft.AspNetCore.Mvc;
using TripleDerby.Api.Conventions;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;

namespace TripleDerby.Api.Controllers;

/// <summary>
/// API endpoints for training catalog and operations.
/// </summary>
[ApiController]
[Route("api/trainings")]
[ApiConventionType(typeof(ApiConventions))]
public class TrainingsController(ITrainingService trainingService) : ControllerBase
{
    /// <summary>
    /// Gets all available training types.
    /// </summary>
    /// <returns>List of training definitions</returns>
    /// <response code="200">Returns all training types</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TrainingsResult>>> GetAll()
    {
        var result = await trainingService.GetAll();
        return Ok(result);
    }

    /// <summary>
    /// Gets details for a specific training type.
    /// </summary>
    /// <param name="id">Training type ID</param>
    /// <returns>Training details</returns>
    /// <response code="200">Returns training details</response>
    /// <response code="404">Training not found</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingResult>> Get(byte id)
    {
        try
        {
            var result = await trainingService.Get(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Gets available training options for a horse.
    /// </summary>
    /// <param name="horseId">Horse ID</param>
    /// <param name="sessionId">Session ID for caching</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of training options for the horse</returns>
    /// <response code="200">Returns training options</response>
    /// <response code="404">Horse not found</response>
    [HttpGet("options")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TrainingOptionResult>>> GetOptions(
        [FromQuery] Guid horseId,
        [FromQuery] Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await trainingService.GetTrainingOptions(horseId, sessionId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Gets training history for a horse.
    /// </summary>
    /// <param name="horseId">Horse ID</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Training history records</returns>
    /// <response code="200">Returns training history</response>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TrainingHistoryResult>>> GetHistory(
        [FromQuery] Guid horseId,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await trainingService.GetTrainingHistory(horseId, limit, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Initiates async training execution via message queue.
    /// </summary>
    /// <param name="request">Training request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>202 Accepted with request tracking information</returns>
    /// <response code="202">Training request accepted</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Horse or training not found</response>
    [HttpPost("requests")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CreateRequest(
        [FromBody] TrainHorseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            await trainingService.QueueTrainingAsync(
                request.HorseId,
                request.TrainingId,
                request.SessionId,
                userId,
                cancellationToken);

            var requestUrl = Url.Action(nameof(GetRequest), new { id = request.SessionId });
            return Accepted(requestUrl, new { sessionId = request.SessionId, status = "queued" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Gets the status of a training request.
    /// </summary>
    /// <param name="id">Session/Request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Training request status</returns>
    /// <response code="200">Returns request status</response>
    /// <response code="404">Request not found</response>
    [HttpGet("requests/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingRequestStatusResult>> GetRequest(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await trainingService.GetRequestStatus(id, cancellationToken);

        if (result == null)
            return NotFound($"Training request with ID {id} not found");

        return Ok(result);
    }

    /// <summary>
    /// Re-publishes a failed training request.
    /// </summary>
    /// <param name="id">Session/Request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>202 Accepted</returns>
    /// <response code="202">Request replayed</response>
    /// <response code="404">Request not found</response>
    /// <response code="400">Request not in failed status</response>
    [HttpPost("requests/{id:guid}/replay")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ReplayRequest(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await trainingService.ReplayTrainingRequest(id, cancellationToken);
            return Accepted();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

/// <summary>
/// Request DTO for training a horse.
/// </summary>
public record TrainHorseRequest(
    Guid HorseId,
    byte TrainingId,
    Guid SessionId
);
