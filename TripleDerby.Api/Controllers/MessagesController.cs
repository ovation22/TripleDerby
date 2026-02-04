using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Api.Controllers;

/// <summary>
/// Unified API for viewing and managing async request messages across all services.
/// TODO: This is an admin/operational controller and should be protected in production.
/// </summary>
[ApiController]
[Route("api/messages")]
public class MessagesController(
    IMessageService messageService,
    IBreedingService breedingService,
    IFeedingService feedingService,
    IRaceService raceService,
    ITrainingService trainingService) : ControllerBase
{
    /// <summary>
    /// Gets aggregated status counts for all services.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageRequestsSummaryResult>> GetSummary(
        CancellationToken cancellationToken = default)
    {
        var summary = await messageService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Gets paginated list of all requests across services with optional filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedList<MessageRequestSummary>>> GetAll(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] RequestStatus? status = null,
        [FromQuery] RequestServiceType? serviceType = null,
        CancellationToken cancellationToken = default)
    {
        var requests = await messageService.GetAllRequestsAsync(
            pagination, status, serviceType, cancellationToken);
        return Ok(requests);
    }

    /// <summary>
    /// Replays a single request by ID and service type.
    /// </summary>
    [HttpPost("{serviceType}/{id}/replay")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReplayRequest(
        RequestServiceType serviceType,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            bool success = serviceType switch
            {
                RequestServiceType.Breeding => await breedingService.ReplayBreedingRequest(id, cancellationToken),
                RequestServiceType.Feeding => await feedingService.ReplayFeedingRequest(id, cancellationToken),
                RequestServiceType.Racing => await raceService.ReplayRaceRequest(id, cancellationToken),
                RequestServiceType.Training => await ReplayTrainingRequestAsync(id, cancellationToken),
                _ => throw new ArgumentException("Invalid service type", nameof(serviceType))
            };

            return success ? Accepted() : NotFound();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Replays all non-complete requests for a specific service type.
    /// </summary>
    [HttpPost("{serviceType}/replay-all")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ReplayAll(
        RequestServiceType serviceType,
        [FromQuery] int maxDegreeOfParallelism = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            int published = serviceType switch
            {
                RequestServiceType.Breeding => await breedingService.ReplayAllNonComplete(maxDegreeOfParallelism, cancellationToken),
                RequestServiceType.Feeding => await feedingService.ReplayAllNonComplete(maxDegreeOfParallelism, cancellationToken),
                RequestServiceType.Racing => await raceService.ReplayAllNonComplete(maxDegreeOfParallelism, cancellationToken),
                RequestServiceType.Training => await trainingService.ReplayAllNonComplete(maxDegreeOfParallelism, cancellationToken),
                _ => throw new ArgumentException("Invalid service type", nameof(serviceType))
            };

            return Accepted(new { published });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Helper to wrap TrainingService.ReplayTrainingRequest (which returns Task, not Task{bool})
    /// to match the pattern of other services.
    /// </summary>
    private async Task<bool> ReplayTrainingRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await trainingService.ReplayTrainingRequest(id, cancellationToken);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }
}
