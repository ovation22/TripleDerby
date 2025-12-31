using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TripleDerby.Core.Abstractions.Messaging;

namespace TripleDerby.Infrastructure.Messaging;

/// <summary>
/// Generic message consumer that works with any broker via IMessageBrokerAdapter.
/// Resolves message processors from DI and delegates message handling to them.
/// </summary>
/// <typeparam name="TMessage">The type of message to consume</typeparam>
/// <typeparam name="TProcessor">The processor type that handles TMessage</typeparam>
public class GenericMessageConsumer<TMessage, TProcessor>(
    IMessageBrokerAdapter adapter,
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<GenericMessageConsumer<TMessage, TProcessor>> logger)
    : IMessageConsumer
    where TProcessor : IMessageProcessor<TMessage>
{
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var config = BuildConfiguration();

        await adapter.ConnectAsync(config, cancellationToken);
        await adapter.SubscribeAsync<TMessage>(ProcessMessageAsync, cancellationToken);

        logger.LogInformation(
            "GenericMessageConsumer<{MessageType}, {ProcessorType}> started (Queue: {Queue}, Concurrency: {Concurrency})",
            typeof(TMessage).Name,
            typeof(TProcessor).Name,
            config.Queue,
            config.Concurrency);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await adapter.DisconnectAsync();

        logger.LogInformation(
            "GenericMessageConsumer<{MessageType}, {ProcessorType}> stopped",
            typeof(TMessage).Name,
            typeof(TProcessor).Name);
    }

    public async ValueTask DisposeAsync()
    {
        await adapter.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    private async Task<MessageProcessingResult> ProcessMessageAsync(
        TMessage message,
        MessageContext context)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<TProcessor>();

            logger.LogDebug(
                "Processing {MessageType} (MessageId: {MessageId}, CorrelationId: {CorrelationId})",
                typeof(TMessage).Name,
                context.MessageId,
                context.CorrelationId);

            var result = await processor.ProcessAsync(message, context);

            if (result.Success)
            {
                logger.LogInformation(
                    "Successfully processed {MessageType} (MessageId: {MessageId})",
                    typeof(TMessage).Name,
                    context.MessageId);
            }
            else
            {
                logger.LogWarning(
                    "Failed to process {MessageType} (MessageId: {MessageId}, Reason: {Reason}, Requeue: {Requeue})",
                    typeof(TMessage).Name,
                    context.MessageId,
                    result.ErrorReason,
                    result.Requeue);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception processing {MessageType} (MessageId: {MessageId})",
                typeof(TMessage).Name,
                context.MessageId);

            return MessageProcessingResult.FailedWithException(ex, requeue: false);
        }
    }

    private MessageBrokerConfig BuildConfiguration()
    {
        // Read connection string from multiple possible locations
        var connectionString =
            configuration["MessageBus:RabbitMq:ConnectionString"]
            ?? configuration["MessageBus:RabbitMq"]
            ?? configuration["MessageBus__RabbitMq__ConnectionString"]
            ?? configuration["MessageBus__RabbitMq"]
            ?? configuration.GetConnectionString("RabbitMq")
            ?? configuration.GetConnectionString("messaging")
            ?? configuration.GetConnectionString("servicebus")
            ?? throw new InvalidOperationException(
                "Message broker connection string not configured. " +
                "Set MessageBus:RabbitMq:ConnectionString or ConnectionStrings:messaging");

        var config = new MessageBrokerConfig
        {
            ConnectionString = connectionString,
            Queue = configuration["MessageBus:Consumer:Queue"]
                ?? configuration["MessageBus:Queue"]
                ?? throw new InvalidOperationException("MessageBus:Consumer:Queue not configured"),
            Concurrency = int.TryParse(configuration["MessageBus:Consumer:Concurrency"], out var concurrency)
                ? concurrency
                : 5,
            MaxRetries = int.TryParse(configuration["MessageBus:Consumer:MaxRetries"], out var maxRetries)
                ? maxRetries
                : 3,
            PrefetchCount = int.TryParse(configuration["MessageBus:Consumer:PrefetchCount"], out var prefetchCount)
                ? prefetchCount
                : 10
        };

        // Add RabbitMQ-specific configuration if present
        var exchange = configuration["MessageBus:RabbitMq:Exchange"]
            ?? configuration["MessageBus:Exchange"];
        if (!string.IsNullOrWhiteSpace(exchange))
        {
            config.ProviderSpecific["Exchange"] = exchange;
        }

        var routingKey = configuration["MessageBus:RabbitMq:RoutingKey"]
            ?? configuration["MessageBus:RoutingKey"];
        if (!string.IsNullOrWhiteSpace(routingKey))
        {
            config.ProviderSpecific["RoutingKey"] = routingKey;
        }

        var exchangeType = configuration["MessageBus:RabbitMq:ExchangeType"]
            ?? configuration["MessageBus:ExchangeType"];
        if (!string.IsNullOrWhiteSpace(exchangeType))
        {
            config.ProviderSpecific["ExchangeType"] = exchangeType;
        }

        // Add Azure Service Bus-specific configuration if present
        var subscriptionName = configuration["MessageBus:ServiceBus:SubscriptionName"];
        if (!string.IsNullOrWhiteSpace(subscriptionName))
        {
            config.ProviderSpecific["SubscriptionName"] = subscriptionName;
        }

        return config;
    }
}
