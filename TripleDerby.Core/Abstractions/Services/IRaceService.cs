using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;
using TripleDerby.SharedKernel.Pagination;

namespace TripleDerby.Core.Abstractions.Services;

public interface IRaceService
{
    Task<RaceResult> Get(byte id, CancellationToken cancellationToken = default);
    Task<PagedList<RacesResult>> Filter(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<RaceRequestStatusResult> QueueRaceAsync(byte raceId, Guid horseId, Guid ownerId, CancellationToken cancellationToken = default);
    Task<RaceRequestStatusResult?> GetRequestStatusAsync(byte raceId, Guid requestId, CancellationToken cancellationToken = default);
    Task<bool> ReplayRaceRequest(Guid raceRequestId, CancellationToken cancellationToken = default);
    Task<int> ReplayAllNonComplete(int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default);
}
