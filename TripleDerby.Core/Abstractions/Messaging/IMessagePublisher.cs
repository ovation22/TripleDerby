namespace TripleDerby.Core.Abstractions.Messaging;

/// <summary>
/// Defines a message publisher that can send messages to a message broker.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message with provider-specific routing options.
    /// </summary>
    /// <param name="message">The message to publish</param>
    /// <param name="options">Optional routing and delivery options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<T>(T message, MessagePublishOptions? options = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Extension methods for IMessagePublisher providing backward compatibility.
/// </summary>
public static class MessagePublisherExtensions
{
    /// <summary>
    /// Legacy method for backward compatibility using explicit destination and subject.
    /// Prefer using PublishAsync with MessagePublishOptions for better provider abstraction.
    /// </summary>
    public static Task PublishAsync<T>(
        this IMessagePublisher publisher,
        T message,
        string? destination,
        string? subject,
        CancellationToken cancellationToken = default)
    {
        var options = new MessagePublishOptions
        {
            Destination = destination,
            Subject = subject
        };
        return publisher.PublishAsync(message, options, cancellationToken);
    }
}