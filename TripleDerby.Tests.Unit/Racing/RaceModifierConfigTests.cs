using TripleDerby.Services.Racing.Racing;
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

    // Phase 3: Environmental Modifier Tests

    [Fact]
    public void SurfaceModifiers_ShouldContainAllSurfaceTypes()
    {
        // Act
        var surfaceModifiers = RaceModifierConfig.SurfaceModifiers;

        // Assert
        Assert.Equal(3, surfaceModifiers.Count);
        Assert.True(surfaceModifiers.ContainsKey(SurfaceId.Dirt));
        Assert.True(surfaceModifiers.ContainsKey(SurfaceId.Turf));
        Assert.True(surfaceModifiers.ContainsKey(SurfaceId.Artificial));
    }

    [Fact]
    public void SurfaceModifiers_Dirt_ShouldBe1Point00()
    {
        // Act
        var modifier = RaceModifierConfig.SurfaceModifiers[SurfaceId.Dirt];

        // Assert
        Assert.Equal(1.00, modifier);
    }

    [Fact]
    public void SurfaceModifiers_Turf_ShouldBe1Point02()
    {
        // Act
        var modifier = RaceModifierConfig.SurfaceModifiers[SurfaceId.Turf];

        // Assert
        Assert.Equal(1.02, modifier);
    }

    [Fact]
    public void SurfaceModifiers_Artificial_ShouldBe1Point01()
    {
        // Act
        var modifier = RaceModifierConfig.SurfaceModifiers[SurfaceId.Artificial];

        // Assert
        Assert.Equal(1.01, modifier);
    }

    [Fact]
    public void ConditionModifiers_ShouldContainAll11Conditions()
    {
        // Act
        var conditionModifiers = RaceModifierConfig.ConditionModifiers;

        // Assert - All 11 ConditionId values should be present
        Assert.Equal(11, conditionModifiers.Count);
        Assert.True(conditionModifiers.ContainsKey(ConditionId.Fast));
        Assert.True(conditionModifiers.ContainsKey(ConditionId.Firm));
        Assert.True(conditionModifiers.ContainsKey(ConditionId.Good));
        Assert.True(conditionModifiers.ContainsKey(ConditionId.WetFast));
        Assert.True(conditionModifiers.ContainsKey(ConditionId.Soft));
        Assert.True(conditionModifiers.ContainsKey(ConditionId.Yielding));
        Assert.True(conditionModifiers.ContainsKey(ConditionId.Muddy));
        Assert.True(conditionModifiers.ContainsKey(ConditionId.Sloppy));
        Assert.True(conditionModifiers.ContainsKey(ConditionId.Heavy));
        Assert.True(conditionModifiers.ContainsKey(ConditionId.Frozen));
        Assert.True(conditionModifiers.ContainsKey(ConditionId.Slow));
    }

    [Theory]
    [InlineData(ConditionId.Fast, 1.03)]
    [InlineData(ConditionId.Firm, 1.02)]
    [InlineData(ConditionId.Good, 1.00)]
    [InlineData(ConditionId.WetFast, 0.99)]
    [InlineData(ConditionId.Soft, 0.98)]
    [InlineData(ConditionId.Yielding, 0.97)]
    [InlineData(ConditionId.Muddy, 0.96)]
    [InlineData(ConditionId.Sloppy, 0.95)]
    [InlineData(ConditionId.Heavy, 0.93)]
    [InlineData(ConditionId.Frozen, 0.92)]
    [InlineData(ConditionId.Slow, 0.90)]
    public void ConditionModifiers_ShouldHaveCorrectValue(ConditionId conditionId, double expectedModifier)
    {
        // Act
        var modifier = RaceModifierConfig.ConditionModifiers[conditionId];

        // Assert
        Assert.Equal(expectedModifier, modifier);
    }

    // Phase 4: Phase Modifier Tests

    [Fact]
    public void LegTypePhaseModifiers_ShouldContain4PhaseBasedLegTypes()
    {
        // RailRunner excluded - uses conditional bonus instead (Feature 005)
        // Act
        var legTypeModifiers = RaceModifierConfig.LegTypePhaseModifiers;

        // Assert
        Assert.Equal(4, legTypeModifiers.Count);
        Assert.True(legTypeModifiers.ContainsKey(LegTypeId.StartDash));
        Assert.True(legTypeModifiers.ContainsKey(LegTypeId.FrontRunner));
        Assert.True(legTypeModifiers.ContainsKey(LegTypeId.StretchRunner));
        Assert.True(legTypeModifiers.ContainsKey(LegTypeId.LastSpurt));
        Assert.False(legTypeModifiers.ContainsKey(LegTypeId.RailRunner)); // RailRunner uses conditional bonus
    }

    [Fact]
    public void LegTypePhaseModifiers_StartDash_ShouldHaveCorrectValues()
    {
        // Act
        var modifier = RaceModifierConfig.LegTypePhaseModifiers[LegTypeId.StartDash];

        // Assert
        Assert.Equal(0.00, modifier.StartPercent);
        Assert.Equal(0.25, modifier.EndPercent);
        Assert.Equal(1.04, modifier.Multiplier);
    }

    [Fact]
    public void LegTypePhaseModifiers_FrontRunner_ShouldHaveCorrectValues()
    {
        // Act
        var modifier = RaceModifierConfig.LegTypePhaseModifiers[LegTypeId.FrontRunner];

        // Assert
        Assert.Equal(0.00, modifier.StartPercent);
        Assert.Equal(0.20, modifier.EndPercent);
        Assert.Equal(1.03, modifier.Multiplier);
    }

    [Fact]
    public void LegTypePhaseModifiers_StretchRunner_ShouldHaveCorrectValues()
    {
        // Act
        var modifier = RaceModifierConfig.LegTypePhaseModifiers[LegTypeId.StretchRunner];

        // Assert
        Assert.Equal(0.60, modifier.StartPercent);
        Assert.Equal(0.80, modifier.EndPercent);
        Assert.Equal(1.03, modifier.Multiplier);
    }

    [Fact]
    public void LegTypePhaseModifiers_LastSpurt_ShouldHaveCorrectValues()
    {
        // Act
        var modifier = RaceModifierConfig.LegTypePhaseModifiers[LegTypeId.LastSpurt];

        // Assert
        Assert.Equal(0.75, modifier.StartPercent);
        Assert.Equal(1.00, modifier.EndPercent);
        Assert.Equal(1.04, modifier.Multiplier);
    }

    [Fact]
    public void RailRunnerConfiguration_ShouldHaveCorrectValues()
    {
        // RailRunner no longer uses phase-based modifiers (Feature 005)
        // Instead uses conditional lane/traffic bonus

        // Assert - RailRunner not in phase modifiers dictionary
        Assert.False(RaceModifierConfig.LegTypePhaseModifiers.ContainsKey(LegTypeId.RailRunner));

        // Assert - Rail runner specific configuration exists
        Assert.Equal(1.03, RaceModifierConfig.RailRunnerBonusMultiplier);
        Assert.Equal(0.5m, RaceModifierConfig.RailRunnerClearPathDistance);
    }
}
