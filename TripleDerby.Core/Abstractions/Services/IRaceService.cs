using TripleDerby.SharedKernel;

namespace TripleDerby.Core.Abstractions.Services;

public interface IRaceService
{
    Task<RaceResult> Get(byte id, CancellationToken cancellationToken = default);
    Task<IEnumerable<RacesResult>> GetAll(CancellationToken cancellationToken = default);
    Task<RaceRunResult> Race(byte raceId, Guid horseId, CancellationToken cancellationToken = default);
}
