using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Messages;
using TripleDerby.SharedKernel.Dtos;

namespace TripleDerby.Core.Abstractions.Services;

public interface IBreedingService
{
    /// <summary>
    /// Returns a list of featured dams (female parent horses) suitable for breeding.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an enumerable of <see cref="ParentHorse"/>.
    /// </returns>
    Task<IEnumerable<ParentHorse>> GetDams();

    /// <summary>
    /// Returns a list of featured sires (male parent horses) suitable for breeding.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an enumerable of <see cref="ParentHorse"/>.
    /// </returns>
    Task<IEnumerable<ParentHorse>> GetSires();

    /// <summary>
    /// Create a single breeding request and publish a corresponding <see cref="BreedingRequested"/> message.
    /// </summary>
    /// <param name="request">The breed request containing sire, dam and requesting user information.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the published <see cref="BreedingRequested"/> message.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the specified sire or dam cannot be retrieved.</exception>
    Task<BreedingRequested> Breed(BreedRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Replay a persisted BreedingRequest by id by re-publishing the <see cref="BreedingRequested"/> message.
    /// Returns true when the BreedingRequest was found and the message published; false when the request was not found.
    /// </summary>
    Task<bool> ReplayBreedingRequest(Guid breedingRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replay all non-complete breeding requests (Pending or Failed). Returns the number of requests that were published.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Maximum concurrent publish tasks to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int> ReplayAllNonComplete(int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get status/details for a persisted BreedingRequest by id.
    /// </summary>
    Task<BreedingRequestStatusResult?> GetRequestStatus(Guid breedingRequestId, CancellationToken cancellationToken = default);
}
