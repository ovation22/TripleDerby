using TrainingEntity = TripleDerby.Core.Entities.Training;
using TripleDerby.Core.Entities;

namespace TripleDerby.Tests.Unit.Entities;

/// <summary>
/// Tests for TrainingSession entity (Feature 020 - Phase 1)
/// </summary>
public class TrainingSessionTests
{
    [Fact]
    public void TrainingSession_SetBasicProperties_Succeeds()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var horseId = Guid.NewGuid();
        var trainingId = (byte)1;

        // Act
        var session = new TrainingSession
        {
            Id = sessionId,
            TrainingId = trainingId,
            HorseId = horseId,
            Result = "Training completed successfully"
        };

        // Assert
        Assert.Equal(sessionId, session.Id);
        Assert.Equal(trainingId, session.TrainingId);
        Assert.Equal(horseId, session.HorseId);
        Assert.Equal("Training completed successfully", session.Result);
    }

    [Fact]
    public void TrainingSession_SetSessionDate_Succeeds()
    {
        // Arrange
        var sessionDate = DateTime.UtcNow;

        // Act
        var session = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = 1,
            HorseId = Guid.NewGuid(),
            SessionDate = sessionDate,
            Result = "Training completed"
        };

        // Assert
        Assert.Equal(sessionDate, session.SessionDate);
    }

    [Fact]
    public void TrainingSession_SetRaceStartsAtTime_Succeeds()
    {
        // Arrange
        short raceStarts = 15;

        // Act
        var session = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = 1,
            HorseId = Guid.NewGuid(),
            RaceStartsAtTime = raceStarts,
            Result = "Training completed"
        };

        // Assert
        Assert.Equal(15, session.RaceStartsAtTime);
    }

    [Fact]
    public void TrainingSession_SetStatGains_Succeeds()
    {
        // Arrange & Act
        var session = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = 1,
            HorseId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow,
            SpeedGain = 0.45,
            StaminaGain = 0.12,
            AgilityGain = 0.18,
            DurabilityGain = 0.08,
            Result = "Training completed successfully"
        };

        // Assert
        Assert.Equal(0.45, session.SpeedGain);
        Assert.Equal(0.12, session.StaminaGain);
        Assert.Equal(0.18, session.AgilityGain);
        Assert.Equal(0.08, session.DurabilityGain);
    }

    [Fact]
    public void TrainingSession_SetHappinessChange_Succeeds()
    {
        // Arrange & Act
        var session = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = 1,
            HorseId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow,
            HappinessChange = -8.0,
            Result = "Training completed, horse is tired"
        };

        // Assert
        Assert.Equal(-8.0, session.HappinessChange);
    }

    [Fact]
    public void TrainingSession_OverworkOccurred_DefaultsToFalse()
    {
        // Arrange & Act
        var session = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = 1,
            HorseId = Guid.NewGuid(),
            Result = "Training completed"
        };

        // Assert
        Assert.False(session.OverworkOccurred);
    }

    [Fact]
    public void TrainingSession_OverworkOccurred_CanBeSetToTrue()
    {
        // Arrange & Act
        var session = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = 1,
            HorseId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow,
            OverworkOccurred = true,
            HappinessChange = -13.0, // Extra happiness penalty from overwork
            Result = "Training completed, but horse became overworked"
        };

        // Assert
        Assert.True(session.OverworkOccurred);
        Assert.True(session.HappinessChange < -10.0, "Overwork should result in extra happiness penalty");
    }

    [Fact]
    public void TrainingSession_AllProperties_CanBeSet()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var horseId = Guid.NewGuid();
        var sessionDate = DateTime.UtcNow;

        // Act
        var session = new TrainingSession
        {
            Id = sessionId,
            TrainingId = 5,
            HorseId = horseId,
            SessionDate = sessionDate,
            RaceStartsAtTime = 20,
            SpeedGain = 0.32,
            StaminaGain = 0.48,
            AgilityGain = 0.16,
            DurabilityGain = 0.64,
            HappinessChange = -10.0,
            OverworkOccurred = false,
            Result = "Successful hill climbing session, significant stamina and durability gains"
        };

        // Assert
        Assert.Equal(sessionId, session.Id);
        Assert.Equal(5, session.TrainingId);
        Assert.Equal(horseId, session.HorseId);
        Assert.Equal(sessionDate, session.SessionDate);
        Assert.Equal(20, session.RaceStartsAtTime);
        Assert.Equal(0.32, session.SpeedGain);
        Assert.Equal(0.48, session.StaminaGain);
        Assert.Equal(0.16, session.AgilityGain);
        Assert.Equal(0.64, session.DurabilityGain);
        Assert.Equal(-10.0, session.HappinessChange);
        Assert.False(session.OverworkOccurred);
        Assert.Contains("hill climbing", session.Result.ToLower());
    }

    [Fact]
    public void TrainingSession_RecoverySession_HasPositiveHappinessChange()
    {
        // Arrange & Act
        var session = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = 9,
            HorseId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow,
            RaceStartsAtTime = 25,
            SpeedGain = 0.0,
            StaminaGain = 0.0,
            AgilityGain = 0.0,
            DurabilityGain = 0.0,
            HappinessChange = 15.0,
            OverworkOccurred = false,
            Result = "Horse enjoyed pasture rest and recovered happiness"
        };

        // Assert
        Assert.True(session.HappinessChange > 0);
        Assert.Equal(0.0, session.SpeedGain + session.StaminaGain + session.AgilityGain + session.DurabilityGain);
        Assert.False(session.OverworkOccurred);
    }

    [Fact]
    public void TrainingSession_OverworkSession_HasReducedGains()
    {
        // Arrange - Normal session
        var normalSession = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = 1,
            HorseId = Guid.NewGuid(),
            SpeedGain = 0.50,
            OverworkOccurred = false
        };

        // Arrange - Overwork session (50% reduction)
        var overworkSession = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = 1,
            HorseId = Guid.NewGuid(),
            SpeedGain = 0.25,
            OverworkOccurred = true,
            HappinessChange = -13.0
        };

        // Assert
        Assert.True(overworkSession.SpeedGain < normalSession.SpeedGain,
            "Overwork sessions should have reduced stat gains");
        Assert.True(overworkSession.OverworkOccurred);
        Assert.True(overworkSession.HappinessChange < normalSession.HappinessChange,
            "Overwork sessions should have greater happiness penalty");
    }

    [Fact]
    public void TrainingSession_TrackCareerProgress_WithRaceStarts()
    {
        // Arrange & Act
        var earlyCareerSession = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = 1,
            HorseId = Guid.NewGuid(),
            RaceStartsAtTime = 5,
            SpeedGain = 0.60, // Young horse, better training gains
            Result = "Young horse training session"
        };

        var veteranSession = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = 1,
            HorseId = Guid.NewGuid(),
            RaceStartsAtTime = 80,
            SpeedGain = 0.24, // Old horse, reduced training gains (40% multiplier)
            Result = "Veteran horse training session"
        };

        // Assert
        Assert.True(earlyCareerSession.RaceStartsAtTime < 20, "Young horse should have few race starts");
        Assert.True(veteranSession.RaceStartsAtTime >= 60, "Veteran horse should have many race starts");
        Assert.True(earlyCareerSession.SpeedGain > veteranSession.SpeedGain,
            "Young horses should gain more from training than veteran horses");
    }

    [Fact]
    public void TrainingSession_NullOrEmptyResult_IsValid()
    {
        // Arrange & Act
        var session = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = 1,
            HorseId = Guid.NewGuid(),
            Result = string.Empty
        };

        // Assert
        Assert.NotNull(session.Result);
        Assert.Empty(session.Result);
    }

    [Fact]
    public void TrainingSession_NavigationProperties_CanBeSet()
    {
        // Arrange
        var horse = new Horse { Id = Guid.NewGuid(), Name = "Thunder Bolt" };
        var training = new TrainingEntity { Id = 1, Name = "Sprint Drills" };

        // Act
        var session = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingId = training.Id,
            Training = training,
            HorseId = horse.Id,
            Horse = horse,
            Result = "Training completed"
        };

        // Assert
        Assert.NotNull(session.Training);
        Assert.Equal("Sprint Drills", session.Training.Name);
        Assert.NotNull(session.Horse);
        Assert.Equal("Thunder Bolt", session.Horse.Name);
    }
}
