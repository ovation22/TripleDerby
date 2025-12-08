using TripleDerby.SharedKernel;

namespace TripleDerby.Web.ApiClients.Abstractions;

public interface IBreedingApiClient
{
    Task<IEnumerable<HorseResult>?> GetDamsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<HorseResult>?> GetSiresAsync(CancellationToken cancellationToken = default);
}