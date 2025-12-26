using System.Text.Json;
using TripleDerby.SharedKernel.Messages;

namespace TripleDerby.Tests.Unit.Messages;

/// <summary>
/// Tests for Race message contracts (Phase 5)
/// </summary>
public class RaceMessageTests
{
    [Fact]
    public void RaceRequested_DefaultValues_AreSet()
    {
        // Arrange & Act
        var message = new RaceRequested
        {
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid()
        };

        // Assert
        Assert.NotEqual(Guid.Empty, message.CorrelationId);
        Assert.True(message.RequestedAt > DateTime.UtcNow.AddSeconds(-5));
        Assert.True(message.RequestedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void RaceRequested_SerializesAndDeserializes_Correctly()
    {
        // Arrange
        var original = new RaceRequested
        {
            CorrelationId = Guid.NewGuid(),
            RaceId = 5,
            HorseId = Guid.NewGuid(),
            RequestedBy = Guid.NewGuid(),
            RequestedAt = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<RaceRequested>(json, options);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.CorrelationId, deserialized.CorrelationId);
        Assert.Equal(original.RaceId, deserialized.RaceId);
        Assert.Equal(original.HorseId, deserialized.HorseId);
        Assert.Equal(original.RequestedBy, deserialized.RequestedBy);
    }

    [Fact]
    public void RaceCompleted_SerializesAndDeserializes_Correctly()
    {
        // Arrange
        var original = new RaceCompleted
        {
            CorrelationId = Guid.NewGuid(),
            RaceRunId = Guid.NewGuid(),
            RaceId = 5,
            RaceName = "Kentucky Derby",
            WinnerHorseId = Guid.NewGuid(),
            WinnerName = "Secretariat",
            WinnerTime = 119.4,
            FieldSize = 12,
            CompletedAt = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<RaceCompleted>(json, options);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.CorrelationId, deserialized.CorrelationId);
        Assert.Equal(original.RaceRunId, deserialized.RaceRunId);
        Assert.Equal(original.RaceId, deserialized.RaceId);
        Assert.Equal(original.RaceName, deserialized.RaceName);
        Assert.Equal(original.WinnerHorseId, deserialized.WinnerHorseId);
        Assert.Equal(original.WinnerName, deserialized.WinnerName);
        Assert.Equal(original.WinnerTime, deserialized.WinnerTime);
        Assert.Equal(original.FieldSize, deserialized.FieldSize);
    }

    [Fact]
    public void RaceCompleted_WithResult_SerializesCorrectly()
    {
        // Arrange
        var original = new RaceCompleted
        {
            CorrelationId = Guid.NewGuid(),
            RaceRunId = Guid.NewGuid(),
            RaceId = 5,
            RaceName = "Kentucky Derby",
            WinnerHorseId = Guid.NewGuid(),
            WinnerName = "Secretariat",
            WinnerTime = 119.4,
            FieldSize = 12,
            CompletedAt = DateTime.UtcNow,
            Result = null // Result can be null or populated
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<RaceCompleted>(json, options);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Null(deserialized.Result);
    }

    [Fact]
    public void RaceRequested_CorrelationId_IsUnique()
    {
        // Arrange & Act
        var message1 = new RaceRequested { RaceId = 1, HorseId = Guid.NewGuid(), RequestedBy = Guid.NewGuid() };
        var message2 = new RaceRequested { RaceId = 1, HorseId = Guid.NewGuid(), RequestedBy = Guid.NewGuid() };

        // Assert
        Assert.NotEqual(message1.CorrelationId, message2.CorrelationId);
    }

    [Fact]
    public void RaceCompleted_AllProperties_CanBeSet()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var raceRunId = Guid.NewGuid();
        var winnerHorseId = Guid.NewGuid();

        // Act
        var message = new RaceCompleted
        {
            CorrelationId = correlationId,
            RaceRunId = raceRunId,
            RaceId = 8,
            RaceName = "Preakness Stakes",
            WinnerHorseId = winnerHorseId,
            WinnerName = "Northern Dancer",
            WinnerTime = 115.8,
            FieldSize = 10,
            CompletedAt = DateTime.UtcNow,
            Result = null
        };

        // Assert
        Assert.Equal(correlationId, message.CorrelationId);
        Assert.Equal(raceRunId, message.RaceRunId);
        Assert.Equal(8, message.RaceId);
        Assert.Equal("Preakness Stakes", message.RaceName);
        Assert.Equal(winnerHorseId, message.WinnerHorseId);
        Assert.Equal("Northern Dancer", message.WinnerName);
        Assert.Equal(115.8, message.WinnerTime);
        Assert.Equal(10, message.FieldSize);
        Assert.Null(message.Result);
    }
}
