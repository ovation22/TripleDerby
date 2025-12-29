using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;

namespace TripleDerby.Core.Abstractions.Services;

public interface IRaceService
{
    Task<RaceResult> Get(byte id, CancellationToken cancellationToken = default);
    Task<IEnumerable<RacesResult>> GetAll(CancellationToken cancellationToken = default);
    Task<RaceRequestStatusResult> QueueRaceAsync(byte raceId, Guid horseId, Guid ownerId, CancellationToken cancellationToken = default);
    Task<RaceRequestStatusResult?> GetRequestStatusAsync(byte raceId, Guid requestId, CancellationToken cancellationToken = default);
    Task<bool> ReplayRaceRequest(Guid raceRequestId, CancellationToken cancellationToken = default);
    Task<int> ReplayAllNonComplete(int maxDegreeOfParallelism = 10, CancellationToken cancellationToken = default);
}
