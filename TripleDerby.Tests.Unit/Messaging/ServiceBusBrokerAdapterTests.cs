using Microsoft.Extensions.Logging.Abstractions;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Infrastructure.Messaging;

namespace TripleDerby.Tests.Unit.Messaging;

/// <summary>
/// Tests for ServiceBusBrokerAdapter (Phase 5)
///
/// Note: These are structural unit tests. Full integration testing with actual
/// Azure Service Bus would require a live Service Bus instance or emulator,
/// which is beyond the scope of unit tests.
///
/// These tests verify:
/// - Constructor behavior
/// - Interface implementation
/// - Configuration handling
/// - Disposal patterns
/// </summary>
public class ServiceBusBrokerAdapterTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var adapter = new ServiceBusBrokerAdapter(NullLogger<ServiceBusBrokerAdapter>.Instance);

        // Assert
        Assert.NotNull(adapter);
    }

    [Fact]
    public void Constructor_ImplementsIMessageBrokerAdapter()
    {
        // Act
        var adapter = new ServiceBusBrokerAdapter(NullLogger<ServiceBusBrokerAdapter>.Instance);

        // Assert
        Assert.IsAssignableFrom<IMessageBrokerAdapter>(adapter);
    }

    [Fact]
    public void Constructor_ImplementsIAsyncDisposable()
    {
        // Act
        var adapter = new ServiceBusBrokerAdapter(NullLogger<ServiceBusBrokerAdapter>.Instance);

        // Assert
        Assert.IsAssignableFrom<IAsyncDisposable>(adapter);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledWithoutConnect()
    {
        // Arrange
        var adapter = new ServiceBusBrokerAdapter(NullLogger<ServiceBusBrokerAdapter>.Instance);

        // Act & Assert - should not throw
        await adapter.DisposeAsync();
    }

    [Fact]
    public async Task DisconnectAsync_CanBeCalledWithoutConnect()
    {
        // Arrange
        var adapter = new ServiceBusBrokerAdapter(NullLogger<ServiceBusBrokerAdapter>.Instance);

        // Act & Assert - should not throw
        await adapter.DisconnectAsync();
    }

    [Fact]
    public async Task SubscribeAsync_WithoutConnect_ThrowsInvalidOperationException()
    {
        // Arrange
        var adapter = new ServiceBusBrokerAdapter(NullLogger<ServiceBusBrokerAdapter>.Instance);

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
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
            Queue = "test-queue",
            ProviderSpecific = new Dictionary<string, string>
            {
                ["SubscriptionName"] = "test-subscription"
            }
        };

        // Assert
        Assert.Equal("test-subscription", config.ProviderSpecific["SubscriptionName"]);
    }

    // Test message type
    private class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
