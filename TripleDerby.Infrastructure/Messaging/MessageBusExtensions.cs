using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripleDerby.Core.Abstractions.Messaging;

namespace TripleDerby.Infrastructure.Messaging;

/// <summary>
/// Extension methods for configuring message bus with routing abstraction.
/// </summary>
public static class MessageBusExtensions
{
    /// <summary>
    /// Adds message bus with automatic provider selection based on configuration.
    /// Registers both IMessagePublisher and IMessageBrokerAdapter for the selected provider.
    /// Provider determined by MessageBus:Routing:Provider setting ("RabbitMq", "ServiceBus", "Auto").
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration root</param>
    /// <returns>The service collection for chaining</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when provider is invalid or required connection strings are missing.
    /// </exception>
    public static IServiceCollection AddMessageBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind routing configuration
        services.Configure<MessageRoutingConfig>(
            configuration.GetSection("MessageBus:Routing"));

        var routingConfig = configuration
            .GetSection("MessageBus:Routing")
            .Get<MessageRoutingConfig>() ?? new MessageRoutingConfig();

        var provider = ResolveProvider(routingConfig.Provider, configuration);

        // Register publisher and consumer adapter based on provider
        if (provider == "RabbitMq")
        {
            RegisterRabbitMq(services);
        }
        else if (provider == "ServiceBus")
        {
            RegisterServiceBus(services);
        }

        return services;
    }

    /// <summary>
    /// Resolves the provider string to a normalized value, handling auto-detection.
    /// </summary>
    internal static string ResolveProvider(string? providerValue, IConfiguration configuration)
    {
        return providerValue?.ToLowerInvariant() switch
        {
            "rabbitmq" => "RabbitMq",
            "servicebus" => "ServiceBus",
            "auto" or null or "" => DetectProvider(configuration),
            _ => throw new InvalidOperationException(
                $"Invalid MessageBus:Routing:Provider value: '{providerValue}'. " +
                $"Valid values: 'RabbitMq', 'ServiceBus', 'Auto'")
        };
    }

    /// <summary>
    /// Registers RabbitMQ implementations for both publisher and consumer.
    /// Registers IMessagePublisher with RoutingMessagePublisher decorator and IMessageBrokerAdapter with RabbitMqBrokerAdapter.
    /// </summary>
    /// <param name="services">The service collection</param>
    private static void RegisterRabbitMq(IServiceCollection services)
    {
        // Register publisher
        services.AddSingleton<RabbitMqMessagePublisher>();
        services.AddSingleton<IMessagePublisher>(sp =>
        {
            var innerPublisher = sp.GetRequiredService<RabbitMqMessagePublisher>();
            var routingOptions = sp.GetRequiredService<IOptions<MessageRoutingConfig>>();
            var logger = sp.GetRequiredService<ILogger<RoutingMessagePublisher>>();

            logger.LogInformation("Message bus publisher configured with provider: RabbitMq");

            return new RoutingMessagePublisher(innerPublisher, routingOptions, logger);
        });

        // Register consumer adapter
        services.AddSingleton<IMessageBrokerAdapter, RabbitMqBrokerAdapter>();
    }

    /// <summary>
    /// Registers Azure Service Bus implementations for both publisher and consumer.
    /// Registers IMessagePublisher with RoutingMessagePublisher decorator and IMessageBrokerAdapter with ServiceBusBrokerAdapter.
    /// </summary>
    /// <param name="services">The service collection</param>
    private static void RegisterServiceBus(IServiceCollection services)
    {
        // Register publisher
        services.AddSingleton<AzureServiceBusPublisher>();
        services.AddSingleton<IMessagePublisher>(sp =>
        {
            var innerPublisher = sp.GetRequiredService<AzureServiceBusPublisher>();
            var routingOptions = sp.GetRequiredService<IOptions<MessageRoutingConfig>>();
            var logger = sp.GetRequiredService<ILogger<RoutingMessagePublisher>>();

            logger.LogInformation("Message bus publisher configured with provider: ServiceBus");

            return new RoutingMessagePublisher(innerPublisher, routingOptions, logger);
        });

        // Register consumer adapter
        services.AddSingleton<IMessageBrokerAdapter, ServiceBusBrokerAdapter>();
    }

    /// <summary>
    /// Detects available provider by checking for connection strings.
    /// </summary>
    internal static string DetectProvider(IConfiguration configuration)
    {
        var hasRabbitMq = HasRabbitMqConnectionString(configuration);
        var hasServiceBus = HasServiceBusConnectionString(configuration);

        if (hasServiceBus && hasRabbitMq)
        {
            // Both available - prefer RabbitMq for local development compatibility
            return "RabbitMq";
        }

        if (hasServiceBus)
            return "ServiceBus";

        if (hasRabbitMq)
            return "RabbitMq";

        throw new InvalidOperationException(
            "No message broker connection string found. " +
            "Set ConnectionStrings:messaging (RabbitMQ) or ConnectionStrings:servicebus (Azure Service Bus), " +
            "or explicitly set MessageBus:Routing:Provider.");
    }

    /// <summary>
    /// Checks if any RabbitMQ connection string is configured.
    /// </summary>
    internal static bool HasRabbitMqConnectionString(IConfiguration configuration)
    {
        return !string.IsNullOrEmpty(configuration["MessageBus:RabbitMq:ConnectionString"])
            || !string.IsNullOrEmpty(configuration["MessageBus:RabbitMq"])
            || !string.IsNullOrEmpty(configuration["MessageBus__RabbitMq__ConnectionString"])
            || !string.IsNullOrEmpty(configuration["MessageBus__RabbitMq"])
            || !string.IsNullOrEmpty(configuration.GetConnectionString("RabbitMq"))
            || !string.IsNullOrEmpty(configuration.GetConnectionString("messaging"));
    }

    /// <summary>
    /// Checks if Azure Service Bus connection string is configured.
    /// </summary>
    internal static bool HasServiceBusConnectionString(IConfiguration configuration)
    {
        return !string.IsNullOrEmpty(configuration["ConnectionStrings:servicebus"])
            || !string.IsNullOrEmpty(configuration.GetConnectionString("servicebus"));
    }
}
