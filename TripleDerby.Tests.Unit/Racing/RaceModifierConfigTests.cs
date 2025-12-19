using TripleDerby.Core.Configuration;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Racing;

public class RaceModifierConfigTests
{
    [Fact]
    public void TargetTicksFor10Furlongs_ShouldBe237()
    {
        // Arrange & Act
        var targetTicks = RaceModifierConfig.TargetTicksFor10Furlongs;

        // Assert
        Assert.Equal(237.0, targetTicks);
    }

    [Fact]
    public void SpeedModifierPerPoint_ShouldBe0Point002()
    {
        // Arrange & Act
        var speedModifier = RaceModifierConfig.SpeedModifierPerPoint;

        // Assert
        Assert.Equal(0.002, speedModifier);
    }

    [Fact]
    public void AgilityModifierPerPoint_ShouldBe0Point001()
    {
        // Arrange & Act
        var agilityModifier = RaceModifierConfig.AgilityModifierPerPoint;

        // Assert
        Assert.Equal(0.001, agilityModifier);
    }

    [Fact]
    public void RandomVarianceRange_ShouldBe0Point01()
    {
        // Arrange & Act
        var varianceRange = RaceModifierConfig.RandomVarianceRange;

        // Assert
        Assert.Equal(0.01, varianceRange);
    }

    [Fact]
    public void SurfaceModifiers_ShouldNotBeNull()
    {
        // Arrange & Act
        var surfaceModifiers = RaceModifierConfig.SurfaceModifiers;

        // Assert
        Assert.NotNull(surfaceModifiers);
    }

    [Fact]
    public void ConditionModifiers_ShouldNotBeNull()
    {
        // Arrange & Act
        var conditionModifiers = RaceModifierConfig.ConditionModifiers;

        // Assert
        Assert.NotNull(conditionModifiers);
    }

    [Fact]
    public void LegTypePhaseModifiers_ShouldNotBeNull()
    {
        // Arrange & Act
        var legTypeModifiers = RaceModifierConfig.LegTypePhaseModifiers;

        // Assert
        Assert.NotNull(legTypeModifiers);
    }

    // Note: Dictionary population tests will be added in later phases
    // Phase 1 only verifies infrastructure exists, not that it's populated

    [Fact]
    public void SurfaceModifiers_ShouldBeEmptyInPhase1()
    {
        // Act
        var count = RaceModifierConfig.SurfaceModifiers.Count;

        // Assert - Empty in Phase 1, will be populated in Phase 3
        Assert.Equal(0, count);
    }

    [Fact]
    public void ConditionModifiers_ShouldBeEmptyInPhase1()
    {
        // Act
        var count = RaceModifierConfig.ConditionModifiers.Count;

        // Assert - Empty in Phase 1, will be populated in Phase 3
        Assert.Equal(0, count);
    }

    [Fact]
    public void LegTypePhaseModifiers_ShouldBeEmptyInPhase1()
    {
        // Act
        var count = RaceModifierConfig.LegTypePhaseModifiers.Count;

        // Assert - Empty in Phase 1, will be populated in Phase 4
        Assert.Equal(0, count);
    }
}
