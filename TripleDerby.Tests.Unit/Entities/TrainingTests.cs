using TrainingEntity = TripleDerby.Core.Entities.Training;

namespace TripleDerby.Tests.Unit.Entities;

/// <summary>
/// Tests for Training entity (Feature 020 - Phase 1)
/// </summary>
public class TrainingTests
{
    [Fact]
    public void Training_SetBasicProperties_Succeeds()
    {
        // Arrange & Act
        var training = new TrainingEntity
        {
            Id = 1,
            Name = "Sprint Drills",
            Description = "High-intensity sprint training to improve speed"
        };

        // Assert
        Assert.Equal(1, training.Id);
        Assert.Equal("Sprint Drills", training.Name);
        Assert.Equal("High-intensity sprint training to improve speed", training.Description);
    }

    [Fact]
    public void Training_SetStatModifiers_Succeeds()
    {
        // Arrange & Act
        var training = new TrainingEntity
        {
            Id = 1,
            Name = "Sprint Drills",
            Description = "High-intensity sprint training to improve speed",
            SpeedModifier = 1.0,
            StaminaModifier = 0.2,
            AgilityModifier = 0.3,
            DurabilityModifier = 0.1
        };

        // Assert
        Assert.Equal(1.0, training.SpeedModifier);
        Assert.Equal(0.2, training.StaminaModifier);
        Assert.Equal(0.3, training.AgilityModifier);
        Assert.Equal(0.1, training.DurabilityModifier);
    }

    [Fact]
    public void Training_SetHappinessCost_Succeeds()
    {
        // Arrange & Act
        var training = new TrainingEntity
        {
            Id = 1,
            Name = "Sprint Drills",
            Description = "High-intensity sprint training to improve speed",
            HappinessCost = 8.0
        };

        // Assert
        Assert.Equal(8.0, training.HappinessCost);
    }

    [Fact]
    public void Training_SetOverworkRisk_Succeeds()
    {
        // Arrange & Act
        var training = new TrainingEntity
        {
            Id = 1,
            Name = "Sprint Drills",
            Description = "High-intensity sprint training to improve speed",
            OverworkRisk = 0.15
        };

        // Assert
        Assert.Equal(0.15, training.OverworkRisk);
    }

    [Fact]
    public void Training_IsRecovery_DefaultsToFalse()
    {
        // Arrange & Act
        var training = new TrainingEntity
        {
            Id = 1,
            Name = "Sprint Drills",
            Description = "High-intensity sprint training to improve speed"
        };

        // Assert
        Assert.False(training.IsRecovery);
    }

    [Fact]
    public void Training_IsRecovery_CanBeSetToTrue()
    {
        // Arrange & Act
        var training = new TrainingEntity
        {
            Id = 9,
            Name = "Pasture Rest",
            Description = "Light grazing and rest to restore happiness",
            IsRecovery = true,
            HappinessCost = -15.0 // Negative cost means happiness gain
        };

        // Assert
        Assert.True(training.IsRecovery);
        Assert.Equal(-15.0, training.HappinessCost);
    }

    [Fact]
    public void Training_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var training = new TrainingEntity
        {
            Id = 5,
            Name = "Hill Climbing",
            Description = "Climbing steep inclines to build power and endurance",
            SpeedModifier = 0.4,
            StaminaModifier = 0.6,
            AgilityModifier = 0.2,
            DurabilityModifier = 0.8,
            HappinessCost = 10.0,
            OverworkRisk = 0.20,
            IsRecovery = false
        };

        // Assert
        Assert.Equal(5, training.Id);
        Assert.Equal("Hill Climbing", training.Name);
        Assert.Equal("Climbing steep inclines to build power and endurance", training.Description);
        Assert.Equal(0.4, training.SpeedModifier);
        Assert.Equal(0.6, training.StaminaModifier);
        Assert.Equal(0.2, training.AgilityModifier);
        Assert.Equal(0.8, training.DurabilityModifier);
        Assert.Equal(10.0, training.HappinessCost);
        Assert.Equal(0.20, training.OverworkRisk);
        Assert.False(training.IsRecovery);
    }

    [Fact]
    public void Training_RecoveryType_HasNegativeHappinessCost()
    {
        // Arrange & Act
        var training = new TrainingEntity
        {
            Id = 10,
            Name = "Spa Treatment",
            Description = "Massage, hydrotherapy, and pampering to restore morale",
            SpeedModifier = 0.0,
            StaminaModifier = 0.0,
            AgilityModifier = 0.0,
            DurabilityModifier = 0.0,
            HappinessCost = -20.0,
            OverworkRisk = 0.0,
            IsRecovery = true
        };

        // Assert
        Assert.True(training.IsRecovery);
        Assert.True(training.HappinessCost < 0, "Recovery training should have negative happiness cost (i.e., happiness gain)");
        Assert.Equal(0.0, training.OverworkRisk);
    }

    [Fact]
    public void Training_HighIntensityTraining_HasHigherOverworkRisk()
    {
        // Arrange
        var sprintDrills = new TrainingEntity
        {
            Id = 1,
            Name = "Sprint Drills",
            SpeedModifier = 1.0,
            OverworkRisk = 0.15
        };

        var distanceGallops = new TrainingEntity
        {
            Id = 2,
            Name = "Distance Gallops",
            StaminaModifier = 1.0,
            OverworkRisk = 0.12
        };

        // Assert
        Assert.True(sprintDrills.OverworkRisk > 0, "High intensity training should have overwork risk");
        Assert.True(distanceGallops.OverworkRisk > 0, "High intensity training should have overwork risk");
    }

    [Fact]
    public void Training_LowIntensityTraining_HasLowerOverworkRisk()
    {
        // Arrange & Act
        var swimming = new TrainingEntity
        {
            Id = 8,
            Name = "Swimming",
            StaminaModifier = 0.4,
            DurabilityModifier = 0.5,
            HappinessCost = 4.0,
            OverworkRisk = 0.05
        };

        // Assert
        Assert.True(swimming.OverworkRisk < 0.10, "Low intensity training should have lower overwork risk");
        Assert.True(swimming.HappinessCost < 6.0, "Low intensity training should have lower happiness cost");
    }

    [Fact]
    public void Training_BalancedTraining_HasModerateStats()
    {
        // Arrange & Act
        var agilityDrills = new TrainingEntity
        {
            Id = 3,
            Name = "Agility Course",
            SpeedModifier = 0.3,
            StaminaModifier = 0.2,
            AgilityModifier = 1.0,
            DurabilityModifier = 0.2,
            HappinessCost = 7.0,
            OverworkRisk = 0.10,
            IsRecovery = false
        };

        // Assert
        Assert.Equal(1.0, agilityDrills.AgilityModifier); // Primary stat
        Assert.True(agilityDrills.SpeedModifier < 1.0, "Secondary stats should be lower than primary");
        Assert.True(agilityDrills.StaminaModifier < 1.0, "Secondary stats should be lower than primary");
        Assert.True(agilityDrills.DurabilityModifier < 1.0, "Secondary stats should be lower than primary");
    }
}
