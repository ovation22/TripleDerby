using TripleDerby.Core.Entities;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Entities;

/// <summary>
/// Tests for the Feeding entity to verify category and effect range properties.
/// </summary>
public class FeedingTests
{
    [Fact]
    [Trait("Category", "Entity")]
    public void Feeding_HasCategoryId_Property()
    {
        // Arrange & Act
        var feeding = new Feeding
        {
            Id = 1,
            Name = "Apple",
            Description = "Crisp and refreshing",
            CategoryId = FeedingCategoryId.Fruits
        };

        // Assert
        Assert.Equal(FeedingCategoryId.Fruits, feeding.CategoryId);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void Feeding_HasHappinessMinMax_Properties()
    {
        // Arrange & Act
        var feeding = new Feeding
        {
            Id = 1,
            Name = "Apple",
            Description = "Crisp and refreshing",
            CategoryId = FeedingCategoryId.Fruits,
            HappinessMin = 2.0,
            HappinessMax = 3.0
        };

        // Assert
        Assert.Equal(2.0, feeding.HappinessMin);
        Assert.Equal(3.0, feeding.HappinessMax);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void Feeding_HasStaminaMinMax_Properties()
    {
        // Arrange & Act
        var feeding = new Feeding
        {
            Id = 1,
            Name = "Oats",
            Description = "Nutritious staple",
            CategoryId = FeedingCategoryId.Grains,
            HappinessMin = 1.0,
            HappinessMax = 2.0,
            StaminaMin = 0.2,
            StaminaMax = 0.35
        };

        // Assert
        Assert.Equal(0.2, feeding.StaminaMin);
        Assert.Equal(0.35, feeding.StaminaMax);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void Feeding_HasDurabilityMinMax_Properties()
    {
        // Arrange & Act
        var feeding = new Feeding
        {
            Id = 1,
            Name = "Flaxseed",
            Description = "Omega-rich supplement",
            CategoryId = FeedingCategoryId.Proteins,
            HappinessMin = 1.0,
            HappinessMax = 2.0,
            DurabilityMin = 0.3,
            DurabilityMax = 0.45
        };

        // Assert
        Assert.Equal(0.3, feeding.DurabilityMin);
        Assert.Equal(0.45, feeding.DurabilityMax);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void Feeding_HasSpeedMinMax_Properties()
    {
        // Arrange & Act
        var feeding = new Feeding
        {
            Id = 1,
            Name = "Performance Blend",
            Description = "Race day nutrition",
            CategoryId = FeedingCategoryId.Premium,
            HappinessMin = 2.0,
            HappinessMax = 3.5,
            SpeedMin = 0.1,
            SpeedMax = 0.2
        };

        // Assert
        Assert.Equal(0.1, feeding.SpeedMin);
        Assert.Equal(0.2, feeding.SpeedMax);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void Feeding_HasAgilityMinMax_Properties()
    {
        // Arrange & Act
        var feeding = new Feeding
        {
            Id = 1,
            Name = "Carrot",
            Description = "Crunchy favorite",
            CategoryId = FeedingCategoryId.Fruits,
            HappinessMin = 2.0,
            HappinessMax = 3.0,
            AgilityMin = 0.05,
            AgilityMax = 0.15
        };

        // Assert
        Assert.Equal(0.05, feeding.AgilityMin);
        Assert.Equal(0.15, feeding.AgilityMax);
    }

    [Theory]
    [Trait("Category", "Entity")]
    [InlineData(FeedingCategoryId.Treats)]
    [InlineData(FeedingCategoryId.Fruits)]
    [InlineData(FeedingCategoryId.Grains)]
    [InlineData(FeedingCategoryId.Proteins)]
    [InlineData(FeedingCategoryId.Supplements)]
    [InlineData(FeedingCategoryId.Premium)]
    public void FeedingCategoryId_HasAllSixCategories(FeedingCategoryId category)
    {
        // Assert - enum value exists and is valid
        Assert.True(Enum.IsDefined(typeof(FeedingCategoryId), category));
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void FeedingCategoryId_HasExactlySixValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<FeedingCategoryId>();

        // Assert
        Assert.Equal(6, values.Length);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void Feeding_TreatsCategory_HasHighHappinessNoStats()
    {
        // Arrange - typical treat configuration
        var sugarCube = new Feeding
        {
            Id = 1,
            Name = "Sugar Cube",
            Description = "A sweet treat",
            CategoryId = FeedingCategoryId.Treats,
            HappinessMin = 4.0,
            HappinessMax = 5.0,
            StaminaMin = 0,
            StaminaMax = 0,
            DurabilityMin = 0,
            DurabilityMax = 0,
            SpeedMin = 0,
            SpeedMax = 0,
            AgilityMin = 0,
            AgilityMax = 0
        };

        // Assert - treats have high happiness, no stats
        Assert.True(sugarCube.HappinessMin >= 3.0);
        Assert.Equal(0, sugarCube.StaminaMin);
        Assert.Equal(0, sugarCube.StaminaMax);
        Assert.Equal(0, sugarCube.SpeedMin);
        Assert.Equal(0, sugarCube.SpeedMax);
    }

    [Fact]
    [Trait("Category", "Entity")]
    public void Feeding_SupplementsCategory_HasBalancedSmallStats()
    {
        // Arrange - typical supplement configuration
        var electrolyteMix = new Feeding
        {
            Id = 14,
            Name = "Electrolyte Mix",
            Description = "Hydration support",
            CategoryId = FeedingCategoryId.Supplements,
            HappinessMin = 1.0,
            HappinessMax = 2.0,
            StaminaMin = 0.05,
            StaminaMax = 0.1,
            DurabilityMin = 0.05,
            DurabilityMax = 0.1,
            SpeedMin = 0.05,
            SpeedMax = 0.1,
            AgilityMin = 0.05,
            AgilityMax = 0.1
        };

        // Assert - supplements have balanced small stats
        Assert.True(electrolyteMix.StaminaMax <= 0.15);
        Assert.True(electrolyteMix.DurabilityMax <= 0.15);
        Assert.True(electrolyteMix.SpeedMax <= 0.15);
        Assert.True(electrolyteMix.AgilityMax <= 0.15);
        Assert.True(electrolyteMix.StaminaMin > 0);
        Assert.True(electrolyteMix.DurabilityMin > 0);
        Assert.True(electrolyteMix.SpeedMin > 0);
        Assert.True(electrolyteMix.AgilityMin > 0);
    }
}
