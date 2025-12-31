using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Breeding;

public interface IBreedingRequestProcessor : IMessageProcessor<BreedingRequested>
{
}