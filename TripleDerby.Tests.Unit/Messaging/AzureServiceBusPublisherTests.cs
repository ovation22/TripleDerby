using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Infrastructure.Messaging;

namespace TripleDerby.Tests.Unit.Messaging;

/// <summary>
/// Tests for AzureServiceBusPublisher (Phase 2)
/// </summary>
public class AzureServiceBusPublisherTests
{
    [Fact]
    public void Constructor_WithValidConfiguration_Succeeds()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var publisher = new AzureServiceBusPublisher(config, NullLogger<AzureServiceBusPublisher>.Instance);

        // Assert
        Assert.NotNull(publisher);
    }

    [Fact]
    public void Constructor_WithMissingConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new AzureServiceBusPublisher(config, NullLogger<AzureServiceBusPublisher>.Instance));
    }

    [Fact]
    public async Task PublishAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var publisher = new AzureServiceBusPublisher(config, NullLogger<AzureServiceBusPublisher>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.PublishAsync<TestMessage>(null!));
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            })
            .Build();
    }

    private record TestMessage
    {
        public Guid Id { get; init; }
        public string Content { get; init; } = string.Empty;
    }
}
