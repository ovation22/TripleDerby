using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;

namespace TripleDerby.Web.ApiClients.Abstractions;

public interface IRaceRunApiClient
{
    Task<RaceRequestStatusResult?> SubmitRaceRunAsync(byte raceId, Guid horseId, CancellationToken cancellationToken = default);
    Task<RaceRequestStatusResult?> GetRequestStatusAsync(byte raceId, Guid requestId, CancellationToken cancellationToken = default);
    Task<RaceRunResult?> GetRaceRunResultAsync(byte raceId, Guid runId, CancellationToken cancellationToken = default);
}
