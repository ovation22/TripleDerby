using Microsoft.AspNetCore.Mvc;
using TripleDerby.Core.Abstractions.Services;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(
        IUserService userService
    )
    {
        _userService = userService;
    }

    /// <summary>
    /// Returns a paginated and optionally filtered list of users.
    /// </summary>
    /// <param name="request">Pagination, sorting and filter parameters (from query string).</param>
    /// <returns>200 with <see cref="UsersResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns the paged users result.</response>
    /// <response code="400">Unable to return users.</response>
    [HttpGet]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<UserResult>>> Filter([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.Filter(request, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns details for a single user.
    /// </summary>
    /// <param name="id">User identifier (GUID).</param>
    /// <returns>200 with <see cref="UserResult"/>; 400 on failure.</returns>
    /// <response code="200">Returns the user details.</response>
    /// <response code="400">Unable to return user.</response>
    [HttpGet("{id}")]
    [ProducesDefaultResponseType]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResult>> Get(Guid id)
    {
        var result = await _userService.Get(id);

        return Ok(result);
    }
}
