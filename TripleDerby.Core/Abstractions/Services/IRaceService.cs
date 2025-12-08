using TripleDerby.SharedKernel;

namespace TripleDerby.Core.Abstractions.Services;

public interface IRaceService
{
    Task<RaceResult> Get(byte id);
    Task<IEnumerable<RacesResult>> GetAll();
    Task<RaceRunResult> Race(byte raceId, Guid horseId);
}
