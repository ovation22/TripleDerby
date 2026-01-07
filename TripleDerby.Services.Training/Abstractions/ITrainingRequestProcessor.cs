using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Training.Abstractions;

/// <summary>
/// Message processor for TrainingRequested messages.
/// </summary>
public interface ITrainingRequestProcessor : IMessageProcessor<TrainingRequested>
{
}
