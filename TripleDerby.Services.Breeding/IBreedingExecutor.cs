using TripleDerby.SharedKernel;

namespace TripleDerby.Services.Breeding;

public interface IBreedingExecutor
{
    Task<BreedingResult> Breed(Guid sireId, Guid damId, Guid ownerId, CancellationToken cancellationToken = default);
}
