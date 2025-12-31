using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Dtos;

namespace TripleDerby.Web.ApiClients.Abstractions;

public interface IBreedingApiClient
{
    Task<IEnumerable<HorseResult>?> GetDamsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<HorseResult>?> GetSiresAsync(CancellationToken cancellationToken = default);

    Task<BreedingRequestStatusResult?> SubmitBreedingAsync(
        Guid sireId,
        Guid damId,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}