using Microsoft.Extensions.Configuration;
using TripleDerby.Core.Abstractions.Messaging;

namespace TripleDerby.Tests.Unit.Messaging;

/// <summary>
/// Tests for MessageRoutingConfig configuration binding.
/// </summary>
public class MessageRoutingConfigTests
{
    [Fact]
    public void Bind_WithFullConfiguration_BindsAllProperties()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MessageBus:Routing:Provider"] = "RabbitMq",
                ["MessageBus:Routing:DefaultDestination"] = "triplederby.events",
                ["MessageBus:Routing:DefaultRoutingKey"] = "default",
                ["MessageBus:Routing:Routes:RaceRequested:Destination"] = "race-requests",
                ["MessageBus:Routing:Routes:RaceRequested:RoutingKey"] = "RaceRequested",
                ["MessageBus:Routing:Routes:RaceRequested:Subject"] = "race-subject"
            })
            .Build();

        // Act
        var routingConfig = config.GetSection("MessageBus:Routing").Get<MessageRoutingConfig>();

        // Assert
        Assert.NotNull(routingConfig);
        Assert.Equal("RabbitMq", routingConfig.Provider);
        Assert.Equal("triplederby.events", routingConfig.DefaultDestination);
        Assert.Equal("default", routingConfig.DefaultRoutingKey);
        Assert.Single(routingConfig.Routes);
        Assert.True(routingConfig.Routes.ContainsKey("RaceRequested"));

        var raceRoute = routingConfig.Routes["RaceRequested"];
        Assert.Equal("race-requests", raceRoute.Destination);
        Assert.Equal("RaceRequested", raceRoute.RoutingKey);
        Assert.Equal("race-subject", raceRoute.Subject);
    }

    [Fact]
    public void Bind_WithMultipleRoutes_BindsAllRoutes()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MessageBus:Routing:Provider"] = "ServiceBus",
                ["MessageBus:Routing:Routes:RaceRequested:Destination"] = "race-requests",
                ["MessageBus:Routing:Routes:BreedingRequested:Destination"] = "breeding-requests",
                ["MessageBus:Routing:Routes:TrainingRequested:Destination"] = "training-requests"
            })
            .Build();

        // Act
        var routingConfig = config.GetSection("MessageBus:Routing").Get<MessageRoutingConfig>();

        // Assert
        Assert.NotNull(routingConfig);
        Assert.Equal(3, routingConfig.Routes.Count);
        Assert.True(routingConfig.Routes.ContainsKey("RaceRequested"));
        Assert.True(routingConfig.Routes.ContainsKey("BreedingRequested"));
        Assert.True(routingConfig.Routes.ContainsKey("TrainingRequested"));
        Assert.Equal("race-requests", routingConfig.Routes["RaceRequested"].Destination);
        Assert.Equal("breeding-requests", routingConfig.Routes["BreedingRequested"].Destination);
        Assert.Equal("training-requests", routingConfig.Routes["TrainingRequested"].Destination);
    }

    [Fact]
    public void Bind_WithEmptyConfiguration_ProducesValidDefaults()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var routingConfig = config.GetSection("MessageBus:Routing").Get<MessageRoutingConfig>()
            ?? new MessageRoutingConfig();

        // Assert
        Assert.NotNull(routingConfig);
        Assert.Equal("Auto", routingConfig.Provider);
        Assert.Null(routingConfig.DefaultDestination);
        Assert.Null(routingConfig.DefaultRoutingKey);
        Assert.Empty(routingConfig.Routes);
    }

    [Fact]
    public void Bind_WithMetadata_BindsMetadataDictionary()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MessageBus:Routing:Routes:RaceRequested:Destination"] = "race-requests",
                ["MessageBus:Routing:Routes:RaceRequested:Metadata:priority"] = "high",
                ["MessageBus:Routing:Routes:RaceRequested:Metadata:retryCount"] = "3"
            })
            .Build();

        // Act
        var routingConfig = config.GetSection("MessageBus:Routing").Get<MessageRoutingConfig>();

        // Assert
        Assert.NotNull(routingConfig);
        Assert.Single(routingConfig.Routes);

        var raceRoute = routingConfig.Routes["RaceRequested"];
        Assert.NotNull(raceRoute.Metadata);
        Assert.Equal(2, raceRoute.Metadata.Count);
        Assert.Equal("high", raceRoute.Metadata["priority"]);
        Assert.Equal("3", raceRoute.Metadata["retryCount"]);
    }

    [Fact]
    public void Bind_WithProviderOnly_UsesDefaultsForOtherProperties()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MessageBus:Routing:Provider"] = "ServiceBus"
            })
            .Build();

        // Act
        var routingConfig = config.GetSection("MessageBus:Routing").Get<MessageRoutingConfig>();

        // Assert
        Assert.NotNull(routingConfig);
        Assert.Equal("ServiceBus", routingConfig.Provider);
        Assert.Null(routingConfig.DefaultDestination);
        Assert.Null(routingConfig.DefaultRoutingKey);
        Assert.Empty(routingConfig.Routes);
    }

    [Fact]
    public void MessageRoute_DefaultValues_AreNull()
    {
        // Arrange & Act
        var route = new MessageRoute();

        // Assert
        Assert.Null(route.Destination);
        Assert.Null(route.RoutingKey);
        Assert.Null(route.Subject);
        Assert.Null(route.Metadata);
    }

    [Fact]
    public void MessageRoutingConfig_DefaultProvider_IsAuto()
    {
        // Arrange & Act
        var config = new MessageRoutingConfig();

        // Assert
        Assert.Equal("Auto", config.Provider);
    }

    [Fact]
    public void MessageRoutingConfig_DefaultRoutes_IsEmptyDictionary()
    {
        // Arrange & Act
        var config = new MessageRoutingConfig();

        // Assert
        Assert.NotNull(config.Routes);
        Assert.Empty(config.Routes);
    }
}
