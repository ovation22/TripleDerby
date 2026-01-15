using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Entities;

/// <summary>
/// Tests for FeedingSession entity (Feature 022 - Phase 2)
/// </summary>
public class FeedingSessionTests
{
    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_SetBasicProperties_Succeeds()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var horseId = Guid.NewGuid();
        var feedingId = (byte)1;

        // Act
        var session = new FeedingSession
        {
            Id = sessionId,
            FeedingId = feedingId,
            HorseId = horseId,
            Result = FeedResponse.Liked
        };

        // Assert
        Assert.Equal(sessionId, session.Id);
        Assert.Equal(feedingId, session.FeedingId);
        Assert.Equal(horseId, session.HorseId);
        Assert.Equal(FeedResponse.Liked, session.Result);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_SetSessionDate_Succeeds()
    {
        // Arrange
        var sessionDate = DateTime.UtcNow;

        // Act
        var session = new FeedingSession
        {
            Id = Guid.NewGuid(),
            FeedingId = 1,
            HorseId = Guid.NewGuid(),
            SessionDate = sessionDate,
            Result = FeedResponse.Favorite
        };

        // Assert
        Assert.Equal(sessionDate, session.SessionDate);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_SetRaceStartsAtTime_Succeeds()
    {
        // Arrange
        short raceStarts = 15;

        // Act
        var session = new FeedingSession
        {
            Id = Guid.NewGuid(),
            FeedingId = 1,
            HorseId = Guid.NewGuid(),
            RaceStartsAtTime = raceStarts,
            Result = FeedResponse.Neutral
        };

        // Assert
        Assert.Equal(15, session.RaceStartsAtTime);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_SetStatGains_Succeeds()
    {
        // Arrange & Act
        var session = new FeedingSession
        {
            Id = Guid.NewGuid(),
            FeedingId = 1,
            HorseId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow,
            HappinessGain = 8.5,
            SpeedGain = 0.1,
            StaminaGain = 0.2,
            AgilityGain = 0.05,
            DurabilityGain = 0.15,
            Result = FeedResponse.Favorite
        };

        // Assert
        Assert.Equal(8.5, session.HappinessGain);
        Assert.Equal(0.1, session.SpeedGain);
        Assert.Equal(0.2, session.StaminaGain);
        Assert.Equal(0.05, session.AgilityGain);
        Assert.Equal(0.15, session.DurabilityGain);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_PreferenceDiscovered_DefaultsToFalse()
    {
        // Arrange & Act
        var session = new FeedingSession
        {
            Id = Guid.NewGuid(),
            FeedingId = 1,
            HorseId = Guid.NewGuid(),
            Result = FeedResponse.Neutral
        };

        // Assert
        Assert.False(session.PreferenceDiscovered);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_PreferenceDiscovered_CanBeSetToTrue()
    {
        // Arrange & Act
        var session = new FeedingSession
        {
            Id = Guid.NewGuid(),
            FeedingId = 1,
            HorseId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow,
            PreferenceDiscovered = true,
            Result = FeedResponse.Favorite
        };

        // Assert
        Assert.True(session.PreferenceDiscovered);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_AllProperties_CanBeSet()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var horseId = Guid.NewGuid();
        var sessionDate = DateTime.UtcNow;

        // Act
        var session = new FeedingSession
        {
            Id = sessionId,
            FeedingId = 5,
            HorseId = horseId,
            SessionDate = sessionDate,
            RaceStartsAtTime = 20,
            HappinessGain = 12.0,
            SpeedGain = 0.0,
            StaminaGain = 0.0,
            AgilityGain = 0.0,
            DurabilityGain = 0.0,
            PreferenceDiscovered = true,
            Result = FeedResponse.Favorite
        };

        // Assert
        Assert.Equal(sessionId, session.Id);
        Assert.Equal(5, session.FeedingId);
        Assert.Equal(horseId, session.HorseId);
        Assert.Equal(sessionDate, session.SessionDate);
        Assert.Equal(20, session.RaceStartsAtTime);
        Assert.Equal(12.0, session.HappinessGain);
        Assert.Equal(0.0, session.SpeedGain);
        Assert.Equal(0.0, session.StaminaGain);
        Assert.Equal(0.0, session.AgilityGain);
        Assert.Equal(0.0, session.DurabilityGain);
        Assert.True(session.PreferenceDiscovered);
        Assert.Equal(FeedResponse.Favorite, session.Result);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_TreatSession_HasHighHappinessNoStatGains()
    {
        // Arrange & Act - Treats category should give high happiness, no stat gains
        var session = new FeedingSession
        {
            Id = Guid.NewGuid(),
            FeedingId = 1, // Sugar Cubes (Treats)
            HorseId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow,
            HappinessGain = 10.0,
            SpeedGain = 0.0,
            StaminaGain = 0.0,
            AgilityGain = 0.0,
            DurabilityGain = 0.0,
            Result = FeedResponse.Favorite
        };

        // Assert
        Assert.True(session.HappinessGain > 5.0);
        Assert.Equal(0.0, session.SpeedGain + session.StaminaGain + session.AgilityGain + session.DurabilityGain);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_ProteinSession_HasLowHappinessHighDurability()
    {
        // Arrange & Act - Proteins should give low happiness, high durability
        var session = new FeedingSession
        {
            Id = Guid.NewGuid(),
            FeedingId = 10, // Alfalfa Pellets (Proteins)
            HorseId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow,
            HappinessGain = 2.0,
            SpeedGain = 0.0,
            StaminaGain = 0.0,
            AgilityGain = 0.0,
            DurabilityGain = 0.3,
            Result = FeedResponse.Neutral
        };

        // Assert
        Assert.True(session.HappinessGain <= 3.0);
        Assert.True(session.DurabilityGain > 0);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_HatedFoodSession_HasNegativeHappinessGain()
    {
        // Arrange & Act - Hated food can result in negative happiness
        var session = new FeedingSession
        {
            Id = Guid.NewGuid(),
            FeedingId = 5,
            HorseId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow,
            HappinessGain = -3.0, // Upset stomach from hated food
            SpeedGain = 0.0,
            StaminaGain = 0.0,
            AgilityGain = 0.0,
            DurabilityGain = 0.0,
            PreferenceDiscovered = true,
            Result = FeedResponse.Hated
        };

        // Assert
        Assert.True(session.HappinessGain < 0);
        Assert.Equal(FeedResponse.Hated, session.Result);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_RejectedFoodSession_HasNoGains()
    {
        // Arrange & Act - Rejected food means horse refused to eat, no effects
        var session = new FeedingSession
        {
            Id = Guid.NewGuid(),
            FeedingId = 8,
            HorseId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow,
            HappinessGain = 0.0, // No effects when rejected
            SpeedGain = 0.0,
            StaminaGain = 0.0,
            AgilityGain = 0.0,
            DurabilityGain = 0.0,
            PreferenceDiscovered = true,
            Result = FeedResponse.Rejected
        };

        // Assert - Horse refused to eat, all gains should be zero
        Assert.Equal(0.0, session.HappinessGain);
        Assert.Equal(0.0, session.SpeedGain + session.StaminaGain + session.AgilityGain + session.DurabilityGain);
        Assert.Equal(FeedResponse.Rejected, session.Result);
        Assert.True(session.PreferenceDiscovered, "Rejection still discovers the preference");
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_NavigationProperties_CanBeSet()
    {
        // Arrange
        var horse = new Horse { Id = Guid.NewGuid(), Name = "Thunder Bolt" };
        var feeding = new Feeding { Id = 1, Name = "Sugar Cubes" };

        // Act
        var session = new FeedingSession
        {
            Id = Guid.NewGuid(),
            FeedingId = feeding.Id,
            Feeding = feeding,
            HorseId = horse.Id,
            Horse = horse,
            Result = FeedResponse.Favorite
        };

        // Assert
        Assert.NotNull(session.Feeding);
        Assert.Equal("Sugar Cubes", session.Feeding.Name);
        Assert.NotNull(session.Horse);
        Assert.Equal("Thunder Bolt", session.Horse.Name);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingSession_TrackCareerProgress_WithRaceStarts()
    {
        // Arrange & Act
        var earlyCareerSession = new FeedingSession
        {
            Id = Guid.NewGuid(),
            FeedingId = 1,
            HorseId = Guid.NewGuid(),
            RaceStartsAtTime = 5,
            HappinessGain = 10.0, // Young horse, more favorites
            Result = FeedResponse.Favorite
        };

        var veteranSession = new FeedingSession
        {
            Id = Guid.NewGuid(),
            FeedingId = 1,
            HorseId = Guid.NewGuid(),
            RaceStartsAtTime = 60,
            HappinessGain = 5.0, // Old horse, pickier
            Result = FeedResponse.Neutral
        };

        // Assert
        Assert.True(earlyCareerSession.RaceStartsAtTime < 10, "Young horse should have few race starts");
        Assert.True(veteranSession.RaceStartsAtTime >= 50, "Veteran horse should have many race starts");
    }
}
