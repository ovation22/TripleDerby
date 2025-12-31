using Microsoft.Extensions.Logging.Abstractions;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Infrastructure.Messaging;

namespace TripleDerby.Tests.Unit.Messaging;

/// <summary>
/// Tests for RabbitMqBrokerAdapter (Phase 2)
///
/// Note: These are structural unit tests. Full integration testing with actual
/// RabbitMQ connections would require Testcontainers or a live RabbitMQ instance,
/// which is beyond the scope of unit tests.
///
/// These tests verify:
/// - Constructor behavior
/// - Interface implementation
/// - Configuration parsing logic
/// - Disposal patterns
/// </summary>
public class RabbitMqBrokerAdapterTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var adapter = new RabbitMqBrokerAdapter(NullLogger<RabbitMqBrokerAdapter>.Instance);

        // Assert
        Assert.NotNull(adapter);
    }

    [Fact]
    public void Constructor_ImplementsIMessageBrokerAdapter()
    {
        // Act
        var adapter = new RabbitMqBrokerAdapter(NullLogger<RabbitMqBrokerAdapter>.Instance);

        // Assert
        Assert.IsAssignableFrom<IMessageBrokerAdapter>(adapter);
    }

    [Fact]
    public void Constructor_ImplementsIAsyncDisposable()
    {
        // Act
        var adapter = new RabbitMqBrokerAdapter(NullLogger<RabbitMqBrokerAdapter>.Instance);

        // Assert
        Assert.IsAssignableFrom<IAsyncDisposable>(adapter);
    }

    [Fact]
    public async Task ConnectAsync_RequiresConnectionString()
    {
        // Arrange
        var adapter = new RabbitMqBrokerAdapter(NullLogger<RabbitMqBrokerAdapter>.Instance);

        var config = new MessageBrokerConfig
        {
            ConnectionString = "", // Empty connection string
            Queue = "test-queue"
        };

        // Act & Assert
        // This will fail to connect, but we're testing that it attempts to parse
        // In a real scenario, we'd need a mock or actual RabbitMQ
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await adapter.ConnectAsync(config, CancellationToken.None);
        });
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledWithoutConnect()
    {
        // Arrange
        var adapter = new RabbitMqBrokerAdapter(NullLogger<RabbitMqBrokerAdapter>.Instance);

        // Act & Assert - should not throw
        await adapter.DisposeAsync();
    }

    [Fact]
    public async Task DisconnectAsync_CanBeCalledWithoutConnect()
    {
        // Arrange
        var adapter = new RabbitMqBrokerAdapter(NullLogger<RabbitMqBrokerAdapter>.Instance);

        // Act & Assert - should not throw
        await adapter.DisconnectAsync();
    }

    [Fact]
    public async Task SubscribeAsync_WithoutConnect_ThrowsInvalidOperationException()
    {
        // Arrange
        var adapter = new RabbitMqBrokerAdapter(NullLogger<RabbitMqBrokerAdapter>.Instance);

        Task<MessageProcessingResult> Handler(TestMessage msg, MessageContext ctx)
        {
            return Task.FromResult(MessageProcessingResult.Succeeded());
        }

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await adapter.SubscribeAsync<TestMessage>(Handler, CancellationToken.None);
        });
    }

    [Fact]
    public void MessageBrokerConfig_SupportsProviderSpecificSettings()
    {
        // Arrange
        var config = new MessageBrokerConfig
        {
            ConnectionString = "amqp://localhost",
            Queue = "test-queue",
            ProviderSpecific = new Dictionary<string, string>
            {
                ["Exchange"] = "custom-exchange",
                ["RoutingKey"] = "custom-key",
                ["ExchangeType"] = "fanout"
            }
        };

        // Assert
        Assert.Equal("custom-exchange", config.ProviderSpecific["Exchange"]);
        Assert.Equal("custom-key", config.ProviderSpecific["RoutingKey"]);
        Assert.Equal("fanout", config.ProviderSpecific["ExchangeType"]);
    }

    // Test message type
    private class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
