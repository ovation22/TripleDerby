using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Infrastructure.Messaging;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Services.Feeding;

public sealed class Worker(IMessageConsumer consumer, ILogger<Worker> logger)
    : MessageConsumerWorker<FeedingRequested, FeedingRequestProcessor>(consumer, logger);
