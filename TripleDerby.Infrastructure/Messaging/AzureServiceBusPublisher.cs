using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using TripleDerby.Core.Abstractions.Messaging;

namespace TripleDerby.Infrastructure.Messaging;

/// <summary>
/// Azure Service Bus implementation of IMessagePublisher.
/// Supports both emulator (local dev) and cloud (production).
/// </summary>
public class AzureServiceBusPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<AzureServiceBusPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _defaultQueue;

    public AzureServiceBusPublisher(
        IConfiguration configuration,
        ILogger<AzureServiceBusPublisher> logger)
    {
        _logger = logger;

        // Read connection string (Aspire injects this automatically)
        var connectionString =
            configuration["ConnectionStrings:servicebus"]
            ?? configuration.GetConnectionString("servicebus")
            ?? throw new InvalidOperationException(
                "Azure Service Bus connection string not configured. " +
                "Set ConnectionStrings:servicebus in configuration.");

        _client = new ServiceBusClient(connectionString);

        _defaultQueue = configuration["ServiceBus:DefaultQueue"] ?? "race-requests";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        _logger.LogInformation(
            "AzureServiceBusPublisher configured for queue {Queue}",
            _defaultQueue);
    }

    public async Task PublishAsync<T>(
        T message,
        MessagePublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        var queueName = options?.Destination ?? _defaultQueue;

        // Serialize message
        var payload = JsonSerializer.Serialize(message, _jsonOptions);
        var body = new BinaryData(Encoding.UTF8.GetBytes(payload));

        // Create Service Bus message
        var sbMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Subject = options?.Subject ?? typeof(T).Name
        };

        // Add metadata
        sbMessage.ApplicationProperties["MessageType"] = typeof(T).FullName ?? typeof(T).Name;
        sbMessage.ApplicationProperties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

        // Send message
        await using var sender = _client.CreateSender(queueName);

        try
        {
            await sender.SendMessageAsync(sbMessage, cancellationToken);

            _logger.LogInformation(
                "Published message {MessageType} to queue {Queue} (MessageId: {MessageId})",
                typeof(T).Name,
                queueName,
                sbMessage.MessageId);
        }
        catch (ServiceBusException ex)
        {
            _logger.LogError(ex,
                "Failed to publish message {MessageType} to queue {Queue}",
                typeof(T).Name,
                queueName);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
