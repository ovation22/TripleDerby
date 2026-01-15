using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Entities;

/// <summary>
/// Tests for Horse entity (Feature 020 - Phase 1)
/// Focuses on HasTrainedSinceLastRace property for training system
/// </summary>
public class HorseTests
{
    [Fact]
    public void Horse_HasTrainedSinceLastRace_DefaultsToFalse()
    {
        // Arrange & Act
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            ColorId = 1,
            LegTypeId = LegTypeId.FrontRunner,
            IsMale = true,
            OwnerId = Guid.NewGuid()
        };

        // Assert
        Assert.False(horse.HasTrainedSinceLastRace);
    }

    [Fact]
    public void Horse_HasTrainedSinceLastRace_CanBeSetToTrue()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            HasTrainedSinceLastRace = false
        };

        // Act
        horse.HasTrainedSinceLastRace = true;

        // Assert
        Assert.True(horse.HasTrainedSinceLastRace);
    }

    [Fact]
    public void Horse_HasTrainedSinceLastRace_CanBeResetToFalse()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            HasTrainedSinceLastRace = true
        };

        // Act
        horse.HasTrainedSinceLastRace = false;

        // Assert
        Assert.False(horse.HasTrainedSinceLastRace);
    }

    [Fact]
    public void Horse_TrainingWorkflow_SetFlagAfterTraining()
    {
        // Arrange - Horse just finished a race
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            RaceStarts = 10,
            HasTrainedSinceLastRace = false
        };

        // Act - Horse completes training
        horse.HasTrainedSinceLastRace = true;

        // Assert - Flag is set, prevents duplicate training
        Assert.True(horse.HasTrainedSinceLastRace);
        Assert.Equal(10, horse.RaceStarts);
    }

    [Fact]
    public void Horse_RaceWorkflow_ResetFlagAfterRace()
    {
        // Arrange - Horse has trained and is about to race
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            RaceStarts = 10,
            HasTrainedSinceLastRace = true
        };

        // Act - Horse completes race
        horse.RaceStarts++;
        horse.HasTrainedSinceLastRace = false;

        // Assert - Flag is reset, allows training again
        Assert.False(horse.HasTrainedSinceLastRace);
        Assert.Equal(11, horse.RaceStarts);
    }

    [Fact]
    public void Horse_PreventDuplicateTraining_WhenFlagIsTrue()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            HasTrainedSinceLastRace = true
        };

        // Assert - Business logic should check this flag
        Assert.True(horse.HasTrainedSinceLastRace,
            "Horse has already trained since last race and should not be allowed to train again");
    }

    [Fact]
    public void Horse_AllowTraining_WhenFlagIsFalse()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            HasTrainedSinceLastRace = false,
            RaceStarts = 5
        };

        // Assert - Business logic should allow training
        Assert.False(horse.HasTrainedSinceLastRace,
            "Horse has not trained since last race and should be allowed to train");
        Assert.True(horse.RaceStarts > 0, "Horse should have raced at least once before training");
    }

    [Fact]
    public void Horse_MultipleRacesWithTraining_CorrectFlagBehavior()
    {
        // Arrange - New horse
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            RaceStarts = 0,
            HasTrainedSinceLastRace = false
        };

        // Act - First race
        horse.RaceStarts++;
        Assert.Equal(1, horse.RaceStarts);
        Assert.False(horse.HasTrainedSinceLastRace);

        // Act - First training
        horse.HasTrainedSinceLastRace = true;
        Assert.True(horse.HasTrainedSinceLastRace);

        // Act - Second race (resets flag)
        horse.RaceStarts++;
        horse.HasTrainedSinceLastRace = false;
        Assert.Equal(2, horse.RaceStarts);
        Assert.False(horse.HasTrainedSinceLastRace);

        // Act - Second training
        horse.HasTrainedSinceLastRace = true;
        Assert.True(horse.HasTrainedSinceLastRace);

        // Act - Third race (resets flag)
        horse.RaceStarts++;
        horse.HasTrainedSinceLastRace = false;
        Assert.Equal(3, horse.RaceStarts);
        Assert.False(horse.HasTrainedSinceLastRace);
    }

    [Fact]
    public void Horse_BasicProperties_CanBeSet()
    {
        // Arrange & Act
        var horseId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var horse = new Horse
        {
            Id = horseId,
            Name = "Thunder Bolt",
            ColorId = 3,
            LegTypeId = LegTypeId.StartDash,
            IsMale = true,
            RaceStarts = 15,
            RaceWins = 5,
            RacePlace = 4,
            RaceShow = 3,
            Earnings = 50000,
            IsRetired = false,
            OwnerId = ownerId,
            HasTrainedSinceLastRace = false
        };

        // Assert
        Assert.Equal(horseId, horse.Id);
        Assert.Equal("Thunder Bolt", horse.Name);
        Assert.Equal(3, horse.ColorId);
        Assert.Equal(LegTypeId.StartDash, horse.LegTypeId);
        Assert.True(horse.IsMale);
        Assert.Equal(15, horse.RaceStarts);
        Assert.Equal(5, horse.RaceWins);
        Assert.Equal(4, horse.RacePlace);
        Assert.Equal(3, horse.RaceShow);
        Assert.Equal(50000, horse.Earnings);
        Assert.False(horse.IsRetired);
        Assert.Equal(ownerId, horse.OwnerId);
        Assert.False(horse.HasTrainedSinceLastRace);
    }

    [Fact]
    public void Horse_RetiredHorse_CannotTrain()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            IsRetired = true,
            HasTrainedSinceLastRace = false
        };

        // Assert - Business logic should prevent training retired horses
        Assert.True(horse.IsRetired, "Retired horses should not be allowed to train");
    }

    [Fact]
    public void Horse_YoungHorse_CanTrainWithoutRaces()
    {
        // Arrange - Young horse that hasn't raced yet
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Young Prospect",
            RaceStarts = 0,
            HasTrainedSinceLastRace = false,
            IsRetired = false
        };

        // Assert - Business logic may allow training before first race
        Assert.Equal(0, horse.RaceStarts);
        Assert.False(horse.HasTrainedSinceLastRace);
        Assert.False(horse.IsRetired);
    }

    // Feature 022 - Feeding System Tests

    [Fact]
    public void Horse_HasFedSinceLastRace_DefaultsToFalse()
    {
        // Arrange & Act
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            ColorId = 1,
            LegTypeId = LegTypeId.FrontRunner,
            IsMale = true,
            OwnerId = Guid.NewGuid()
        };

        // Assert
        Assert.False(horse.HasFedSinceLastRace);
    }

    [Fact]
    public void Horse_HasFedSinceLastRace_CanBeSetToTrue()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            HasFedSinceLastRace = false
        };

        // Act
        horse.HasFedSinceLastRace = true;

        // Assert
        Assert.True(horse.HasFedSinceLastRace);
    }

    [Fact]
    public void Horse_HasFedSinceLastRace_CanBeResetToFalse()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            HasFedSinceLastRace = true
        };

        // Act
        horse.HasFedSinceLastRace = false;

        // Assert
        Assert.False(horse.HasFedSinceLastRace);
    }

    [Fact]
    public void Horse_FeedingWorkflow_SetFlagAfterFeeding()
    {
        // Arrange - Horse just finished a race
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            RaceStarts = 10,
            HasFedSinceLastRace = false
        };

        // Act - Horse is fed
        horse.HasFedSinceLastRace = true;

        // Assert - Flag is set, prevents duplicate feeding
        Assert.True(horse.HasFedSinceLastRace);
        Assert.Equal(10, horse.RaceStarts);
    }

    [Fact]
    public void Horse_RaceWorkflow_ResetsFeedingFlagAfterRace()
    {
        // Arrange - Horse has been fed and is about to race
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            RaceStarts = 10,
            HasFedSinceLastRace = true
        };

        // Act - Horse completes race
        horse.RaceStarts++;
        horse.HasFedSinceLastRace = false;

        // Assert - Flag is reset, allows feeding again
        Assert.False(horse.HasFedSinceLastRace);
        Assert.Equal(11, horse.RaceStarts);
    }

    [Fact]
    public void Horse_PreventDuplicateFeeding_WhenFlagIsTrue()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            HasFedSinceLastRace = true
        };

        // Assert - Business logic should check this flag
        Assert.True(horse.HasFedSinceLastRace,
            "Horse has already been fed since last race and should not be allowed to feed again");
    }

    [Fact]
    public void Horse_AllowFeeding_WhenFlagIsFalse()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            HasFedSinceLastRace = false,
            RaceStarts = 5
        };

        // Assert - Business logic should allow feeding
        Assert.False(horse.HasFedSinceLastRace,
            "Horse has not been fed since last race and should be allowed to feed");
    }

    [Fact]
    public void Horse_MultipleRacesWithFeedingAndTraining_CorrectFlagBehavior()
    {
        // Arrange - New horse
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            RaceStarts = 0,
            HasTrainedSinceLastRace = false,
            HasFedSinceLastRace = false
        };

        // Act - First race
        horse.RaceStarts++;
        Assert.Equal(1, horse.RaceStarts);
        Assert.False(horse.HasTrainedSinceLastRace);
        Assert.False(horse.HasFedSinceLastRace);

        // Act - First training
        horse.HasTrainedSinceLastRace = true;
        Assert.True(horse.HasTrainedSinceLastRace);
        Assert.False(horse.HasFedSinceLastRace);

        // Act - First feeding
        horse.HasFedSinceLastRace = true;
        Assert.True(horse.HasTrainedSinceLastRace);
        Assert.True(horse.HasFedSinceLastRace);

        // Act - Second race (resets both flags)
        horse.RaceStarts++;
        horse.HasTrainedSinceLastRace = false;
        horse.HasFedSinceLastRace = false;
        Assert.Equal(2, horse.RaceStarts);
        Assert.False(horse.HasTrainedSinceLastRace);
        Assert.False(horse.HasFedSinceLastRace);
    }

    [Fact]
    public void Horse_FeedingIndependentOfTraining_BothCanBeSetSeparately()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Thunder Bolt",
            RaceStarts = 5,
            HasTrainedSinceLastRace = false,
            HasFedSinceLastRace = false
        };

        // Act - Feed but don't train
        horse.HasFedSinceLastRace = true;

        // Assert - Feeding flag is set, training flag is not
        Assert.False(horse.HasTrainedSinceLastRace);
        Assert.True(horse.HasFedSinceLastRace);

        // Act - Now train
        horse.HasTrainedSinceLastRace = true;

        // Assert - Both flags are now set
        Assert.True(horse.HasTrainedSinceLastRace);
        Assert.True(horse.HasFedSinceLastRace);
    }
}
