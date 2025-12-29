using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Infrastructure.Messaging;

namespace TripleDerby.Tests.Unit.DependencyInjection;

/// <summary>
/// Tests for Keyed Dependency Injection (Phase 3)
/// </summary>
public class KeyedDITests
{
    [Fact]
    public void ServiceProvider_ResolvesRabbitMQPublisher_ByKey()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateTestConfiguration();

        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddKeyedSingleton<IMessagePublisher, RabbitMqMessagePublisher>("rabbitmq");

        var provider = services.BuildServiceProvider();

        // Act
        var publisher = provider.GetRequiredKeyedService<IMessagePublisher>("rabbitmq");

        // Assert
        Assert.NotNull(publisher);
        Assert.IsType<RabbitMqMessagePublisher>(publisher);
    }

    [Fact]
    public void ServiceProvider_ResolvesServiceBusPublisher_ByKey()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateTestConfiguration();

        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddKeyedSingleton<IMessagePublisher, AzureServiceBusPublisher>("servicebus");

        var provider = services.BuildServiceProvider();

        // Act
        var publisher = provider.GetRequiredKeyedService<IMessagePublisher>("servicebus");

        // Assert
        Assert.NotNull(publisher);
        Assert.IsType<AzureServiceBusPublisher>(publisher);
    }

    [Fact]
    public void ServiceProvider_ResolvesBothPublishers_Independently()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = CreateTestConfiguration();

        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddKeyedSingleton<IMessagePublisher, RabbitMqMessagePublisher>("rabbitmq");
        services.AddKeyedSingleton<IMessagePublisher, AzureServiceBusPublisher>("servicebus");

        var provider = services.BuildServiceProvider();

        // Act
        var rabbitPublisher = provider.GetRequiredKeyedService<IMessagePublisher>("rabbitmq");
        var serviceBusPublisher = provider.GetRequiredKeyedService<IMessagePublisher>("servicebus");

        // Assert
        Assert.NotNull(rabbitPublisher);
        Assert.NotNull(serviceBusPublisher);
        Assert.IsType<RabbitMqMessagePublisher>(rabbitPublisher);
        Assert.IsType<AzureServiceBusPublisher>(serviceBusPublisher);
        Assert.NotSame(rabbitPublisher, serviceBusPublisher);
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = "Host=localhost;Username=guest;Password=guest",
                ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            })
            .Build();
    }
}
