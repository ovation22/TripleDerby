using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Infrastructure.Messaging;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Breeding;

public sealed class Worker(IMessageConsumer consumer, ILogger<Worker> logger)
    : MessageConsumerWorker<BreedingRequested, BreedingRequestProcessor>(consumer, logger);
