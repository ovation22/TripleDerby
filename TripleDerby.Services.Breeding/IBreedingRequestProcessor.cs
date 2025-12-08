namespace TripleDerby.Services.Breeding;

using TripleDerby.SharedKernel.Messages;

public interface IBreedingRequestProcessor
{
    Task ProcessAsync(BreedingRequested request, CancellationToken cancellationToken);
}