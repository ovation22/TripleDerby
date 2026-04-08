using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Infrastructure.Messaging;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Racing;

public sealed class Worker(IMessageConsumer consumer, ILogger<Worker> logger)
    : MessageConsumerWorker<RaceRequested, RaceRequestProcessor>(consumer, logger);
