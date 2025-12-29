using Microsoft.AspNetCore.Mvc;
using TripleDerby.Api.Conventions;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Messages;
using TripleDerby.SharedKernel.Dtos;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Api.Controllers;

/// <summary>
/// API endpoints to create and list breeding-related resources (sires, dams, and breeding requests).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiConventionType(typeof(ApiConventions))]
public class BreedingController(IBreedingService breedingService) : ControllerBase
{
    /// <summary>
    /// Returns a list of available dams used for breeding.
    /// </summary>
    /// <returns>
    /// 200 with a collection of <see cref="ParentHorse"/> when successful; 400 on failure.
    /// </returns>
    /// <response code="200">Returns the list of dams.</response>
    /// <response code="400">Unable to return dams.</response>
    [HttpGet("dams")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ParentHorse>>> GetDams()
    {
        var result = await breedingService.GetDams();

        return Ok(result);
    }

    /// <summary>
    /// Returns a list of available sires used for breeding.
    /// </summary>
    /// <returns>
    /// 200 with a collection of <see cref="ParentHorse"/> when successful; 400 on failure.
    /// </returns>
    /// <response code="200">Returns the list of sires.</response>
    /// <response code="400">Unable to return sires.</response>
    [HttpGet("sires")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ParentHorse>>> GetSires()
    {
        var result = await breedingService.GetSires();

        return Ok(result);
    }

    /// <summary>
    /// Creates a breeding request for the specified sire and dam and publishes a <see cref="BreedingRequested"/> message.
    /// </summary>
    /// <param name="request">The breeding request containing <see cref="BreedRequest.UserId"/>, <see cref="BreedRequest.SireId"/> and <see cref="BreedRequest.DamId"/>.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// 202 Accepted with the published <see cref="BreedingRequested"/> message when the request is accepted; 400 on invalid input or failure.
    /// </returns>
    /// <response code="202">Breeding request accepted and event published.</response>
    /// <response code="400">Invalid request or unable to create breeding request.</response>
    [HttpPost("requests")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Resource<BreedingRequested>>> CreateRequest([FromBody] BreedRequest request, CancellationToken cancellationToken)
    {
        var result = await breedingService.QueueBreedingAsync(request, cancellationToken);

        // Build canonical request URL for polling
        var requestUrl = Url.Action(nameof(GetRequest), "Breeding", new { id = result.RequestId }, Request.Scheme)
                         ?? $"/api/breeding/requests/{result.RequestId}";

        // HATEOAS links
        var links = new List<Link>
        {
            new("self", requestUrl, "GET"),
            new("replay", Url.Action(nameof(Replay), "Breeding", new { breedingRequestId = result.RequestId }, Request.Scheme) ?? $"/api/breeding/requests/{result.RequestId}/replay", "POST")
        };

        var resource = new Resource<BreedingRequested>(result, links);

        return AcceptedAtAction(nameof(GetRequest), new { id = result.RequestId }, resource);
    }

    /// <summary>
    /// Replay a breeding request by publishing the BreedingRequested message again.
    /// TODO: This is an operational/admin action; consider requiring authorization.
    /// </summary>
    /// <param name="breedingRequestId">The Id of the breeding request to replay.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// 202 Accepted when the request is accepted for processing; 404 if the request was not found.
    /// </returns>
    /// <response code="202">Replay accepted.</response>
    /// <response code="404">Breeding request not found.</response>
    [HttpPost("requests/{breedingRequestId:guid}/replay")]
    public async Task<IActionResult> Replay(Guid breedingRequestId, CancellationToken cancellationToken = default)
    {
        var published = await breedingService.ReplayBreedingRequest(breedingRequestId, cancellationToken);

        if (!published)
            return NotFound();

        return Accepted();
    }

    /// <summary>
    /// Replay all non-complete breeding requests (Pending or Failed) by re-publishing their <see cref="BreedingRequested"/> messages.
    /// TODO: This is an admin/operational endpoint and should be protected in production.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Maximum concurrent publish tasks to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>202 Accepted with number of messages published.</returns>
    [HttpPost("requests/replay")]
    public async Task<IActionResult> ReplayAll([FromQuery] int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default)
    {
        var published = await breedingService.ReplayAllNonComplete(maxDegreeOfParallelism, cancellationToken);

        return Accepted(new { published });
    }

    /// <summary>
    /// Returns persisted status/details for a breeding request resource.
    /// </summary>
    /// <param name="id">Breeding request id.</param>
    /// <returns>200 with status DTO;404 if not found.</returns>
    [HttpGet("requests/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Resource<BreedingRequestStatusResult>>> GetRequest(Guid id)
    {
        var status = await breedingService.GetRequestStatus(id);

        if (status == null) 
            return NotFound();

        var links = new List<Link>
        {
            new("self", Url.Action(nameof(GetRequest), "Breeding", new { id }, Request.Scheme) ?? $"/api/breeding/requests/{id}", "GET")
        };

        if (status.Status == BreedingRequestStatus.Failed)
        {
            links.Add(new Link("replay", Url.Action(nameof(Replay), "Breeding", new { breedingRequestId = id }, Request.Scheme) ?? $"/api/breeding/requests/{id}/replay", "POST"));
        }

        if (status.FoalId.HasValue)
        {
            var foalHref = Url.Action("Get", "Horses", new { id = status.FoalId.Value }, Request.Scheme) ?? $"/api/horses/{status.FoalId.Value}";
            links.Add(new Link("foal", foalHref, "GET"));
        }

        var resource = new Resource<BreedingRequestStatusResult>(status, links);

        return Ok(resource);
    }
}
