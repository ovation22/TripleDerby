using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Infrastructure.Messaging;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Training;

public sealed class Worker(IMessageConsumer consumer, ILogger<Worker> logger)
    : MessageConsumerWorker<TrainingRequested, TrainingRequestProcessor>(consumer, logger);
