namespace TripleDerby.Core.Abstractions.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string? exchange = null, string? routingKey = null, CancellationToken cancellationToken = default);
}