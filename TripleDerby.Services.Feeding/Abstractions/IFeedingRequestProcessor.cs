using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Feeding.Abstractions;

/// <summary>
/// Message processor for FeedingRequested messages.
/// </summary>
public interface IFeedingRequestProcessor : IMessageProcessor<FeedingRequested>
{
}
