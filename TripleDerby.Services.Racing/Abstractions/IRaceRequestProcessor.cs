using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Racing.Abstractions;

/// <summary>
/// Processes race requests from the message queue.
/// </summary>
public interface IRaceRequestProcessor : IMessageProcessor<RaceRequested>
{
}
