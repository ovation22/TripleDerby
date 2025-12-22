using TripleDerby.Core.Configuration;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Racing;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Racing;

/// <summary>
/// Unit tests for stamina depletion calculation logic.
/// Tests the formulas that determine how quickly horses tire during races.
/// </summary>
public class StaminaCalculatorTests
{
    [Trait("Category", "StaminaDepletion")]
    [Theory]
    [InlineData(4, 0.08)]   // Sprint: minimal stamina impact
    [InlineData(6, 0.08)]   // Sprint upper bound
    [InlineData(7, 0.15)]   // Classic lower bound
    [InlineData(10, 0.15)]  // Classic: moderate stamina impact
    [InlineData(11, 0.22)]  // Long lower bound
    [InlineData(12, 0.22)]  // Long: significant stamina impact
    [InlineData(13, 0.30)]  // Marathon lower bound
    [InlineData(16, 0.30)]  // Marathon: severe stamina impact
    public void CalculateBaseDepletionRate_ReturnsCorrectRateForDistance(decimal furlongs, double expectedRate)
    {
        // Arrange
        var calculator = new StaminaCalculator();

        // Act
        var actualRate = calculator.CalculateBaseDepletionRate(furlongs);

        // Assert
        Assert.Equal(expectedRate, actualRate, precision: 3);
    }

    [Trait("Category", "StaminaDepletion")]
    [Theory]
    [InlineData(1)]    // Very short (unrealistic but valid)
    [InlineData(20)]   // Very long (extreme distance)
    public void CalculateBaseDepletionRate_HandlesEdgeCases(decimal furlongs)
    {
        // Arrange
        var calculator = new StaminaCalculator();

        // Act
        var rate = calculator.CalculateBaseDepletionRate(furlongs);

        // Assert
        Assert.InRange(rate, 0.01, 1.0); // Should return reasonable positive value
    }

    [Trait("Category", "StaminaEfficiency")]
    [Theory]
    [InlineData(100, 100, 0.68)]  // Marathon specialist (high stamina + durability): 0.80 * 0.85 = 0.68
    [InlineData(0, 0, 1.38)]      // Pure sprinter (low stamina + durability): 1.20 * 1.15 = 1.38
    [InlineData(50, 50, 1.00)]    // Neutral baseline: 1.00 * 1.00 = 1.00
    [InlineData(75, 50, 0.90)]    // High stamina, neutral durability: 0.90 * 1.00 = 0.90
    [InlineData(50, 75, 0.925)]   // Neutral stamina, high durability: 1.00 * 0.925 = 0.925
    [InlineData(100, 0, 0.92)]    // Big tank, inefficient: 0.80 * 1.15 = 0.92
    [InlineData(0, 100, 1.02)]    // Small tank, efficient: 1.20 * 0.85 = 1.02
    public void CalculateStaminaEfficiency_CombinesStaminaAndDurability(
        int stamina, int durability, double expectedMultiplier)
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var horse = CreateHorseWithStats(stamina: stamina, durability: durability);

        // Act
        var actualMultiplier = calculator.CalculateStaminaEfficiency(horse);

        // Assert
        Assert.Equal(expectedMultiplier, actualMultiplier, precision: 3);
    }

    [Trait("Category", "StaminaEfficiency")]
    [Fact]
    public void CalculateStaminaEfficiency_HigherStamina_LowerDepletion()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var highStaminaHorse = CreateHorseWithStats(stamina: 80, durability: 50);
        var lowStaminaHorse = CreateHorseWithStats(stamina: 20, durability: 50);

        // Act
        var highStaminaMultiplier = calculator.CalculateStaminaEfficiency(highStaminaHorse);
        var lowStaminaMultiplier = calculator.CalculateStaminaEfficiency(lowStaminaHorse);

        // Assert
        Assert.True(highStaminaMultiplier < lowStaminaMultiplier,
            "Higher stamina should result in lower depletion multiplier");
    }

    [Trait("Category", "StaminaEfficiency")]
    [Fact]
    public void CalculateStaminaEfficiency_HigherDurability_LowerDepletion()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var highDurabilityHorse = CreateHorseWithStats(stamina: 50, durability: 80);
        var lowDurabilityHorse = CreateHorseWithStats(stamina: 50, durability: 20);

        // Act
        var highDurabilityMultiplier = calculator.CalculateStaminaEfficiency(highDurabilityHorse);
        var lowDurabilityMultiplier = calculator.CalculateStaminaEfficiency(lowDurabilityHorse);

        // Assert
        Assert.True(highDurabilityMultiplier < lowDurabilityMultiplier,
            "Higher durability should result in lower depletion multiplier");
    }

    [Trait("Category", "PaceMultiplier")]
    [Theory]
    [InlineData(1.10, 1.10)]  // Fast pace = faster depletion
    [InlineData(1.00, 1.00)]  // Neutral pace = baseline depletion
    [InlineData(0.90, 0.90)]  // Slow pace = slower depletion
    [InlineData(1.20, 1.20)]  // Very fast pace
    [InlineData(0.80, 0.80)]  // Very slow pace
    public void CalculatePaceMultiplier_ScalesLinearlyWithSpeed(double speedRatio, double expectedMultiplier)
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var baseSpeed = 0.0422; // Standard base speed for 10f
        var currentSpeed = baseSpeed * speedRatio;

        // Act
        var actualMultiplier = calculator.CalculatePaceMultiplier(currentSpeed, baseSpeed);

        // Assert
        Assert.Equal(expectedMultiplier, actualMultiplier, precision: 3);
    }

    [Trait("Category", "PaceMultiplier")]
    [Fact]
    public void CalculatePaceMultiplier_FasterSpeed_HigherDepletion()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var baseSpeed = 0.0422;
        var fastSpeed = baseSpeed * 1.15;
        var slowSpeed = baseSpeed * 0.85;

        // Act
        var fastMultiplier = calculator.CalculatePaceMultiplier(fastSpeed, baseSpeed);
        var slowMultiplier = calculator.CalculatePaceMultiplier(slowSpeed, baseSpeed);

        // Assert
        Assert.True(fastMultiplier > slowMultiplier,
            "Faster pace should cause more stamina depletion");
    }

    [Trait("Category", "LegTypeStamina")]
    [Theory]
    [InlineData(LegTypeId.StartDash, 0.10, 1.30)]  // Early phase: explosive start
    [InlineData(LegTypeId.StartDash, 0.50, 0.90)]  // Late phase: conserving
    [InlineData(LegTypeId.FrontRunner, 0.10, 1.10)] // Aggressive throughout
    [InlineData(LegTypeId.FrontRunner, 0.90, 1.10)] // Aggressive throughout
    [InlineData(LegTypeId.StretchRunner, 0.30, 0.85)] // Early: conserving
    [InlineData(LegTypeId.StretchRunner, 0.70, 1.15)] // Stretch: pushing
    [InlineData(LegTypeId.LastSpurt, 0.50, 0.80)]  // Mid-race: conserving
    [InlineData(LegTypeId.LastSpurt, 0.90, 1.40)]  // Final: explosive finish
    [InlineData(LegTypeId.RailRunner, 0.50, 0.90)] // Early: steady
    [InlineData(LegTypeId.RailRunner, 0.85, 1.05)] // Late: slight push
    public void CalculateLegTypeStaminaMultiplier_ReturnsCorrectMultiplierForPhase(
        LegTypeId legType, double raceProgress, double expectedMultiplier)
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var horse = CreateHorseWithStats(legType: legType);

        // Act
        var actualMultiplier = calculator.CalculateLegTypeStaminaMultiplier(horse, raceProgress);

        // Assert
        Assert.Equal(expectedMultiplier, actualMultiplier, precision: 3);
    }

    [Trait("Category", "LegTypeStamina")]
    [Fact]
    public void CalculateLegTypeStaminaMultiplier_StartDash_BurnsHardEarlyConservesLate()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var horse = CreateHorseWithStats(legType: LegTypeId.StartDash);

        // Act
        var earlyMultiplier = calculator.CalculateLegTypeStaminaMultiplier(horse, 0.10);  // 10% progress
        var lateMultiplier = calculator.CalculateLegTypeStaminaMultiplier(horse, 0.80);   // 80% progress

        // Assert
        Assert.True(earlyMultiplier > 1.0, "StartDash should burn hard early");
        Assert.True(lateMultiplier < 1.0, "StartDash should conserve late");
        Assert.True(earlyMultiplier > lateMultiplier, "Early burn should be higher than late");
    }

    [Trait("Category", "LegTypeStamina")]
    [Fact]
    public void CalculateLegTypeStaminaMultiplier_LastSpurt_ConservesEarlyExplodesLate()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var horse = CreateHorseWithStats(legType: LegTypeId.LastSpurt);

        // Act
        var earlyMultiplier = calculator.CalculateLegTypeStaminaMultiplier(horse, 0.30);  // 30% progress
        var lateMultiplier = calculator.CalculateLegTypeStaminaMultiplier(horse, 0.90);   // 90% progress

        // Assert
        Assert.True(earlyMultiplier < 1.0, "LastSpurt should conserve early");
        Assert.True(lateMultiplier > 1.0, "LastSpurt should explode late");
        Assert.True(lateMultiplier > earlyMultiplier, "Late explosion should be higher than early conservation");
    }

    [Trait("Category", "LegTypeStamina")]
    [Fact]
    public void CalculateLegTypeStaminaMultiplier_FrontRunner_ConsistentThroughout()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var horse = CreateHorseWithStats(legType: LegTypeId.FrontRunner);

        // Act
        var earlyMultiplier = calculator.CalculateLegTypeStaminaMultiplier(horse, 0.10);
        var midMultiplier = calculator.CalculateLegTypeStaminaMultiplier(horse, 0.50);
        var lateMultiplier = calculator.CalculateLegTypeStaminaMultiplier(horse, 0.90);

        // Assert
        Assert.Equal(earlyMultiplier, midMultiplier, precision: 3);
        Assert.Equal(midMultiplier, lateMultiplier, precision: 3);
        Assert.Equal(1.10, earlyMultiplier, precision: 3);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void CalculateDepletionAmount_CombinesAllFactorsCorrectly()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var horse = CreateHorseWithStats(stamina: 75, durability: 75, legType: LegTypeId.FrontRunner);
        var furlongs = 10m;
        var currentSpeed = 0.0422;
        var baseSpeed = 0.0422;
        var raceProgress = 0.50;

        // Act
        var depletionAmount = calculator.CalculateDepletionAmount(
            horse, furlongs, currentSpeed, baseSpeed, raceProgress);

        // Assert
        Assert.True(depletionAmount > 0, "Should deplete some stamina");
        Assert.InRange(depletionAmount, 0.001, 1.0); // Reasonable range per tick
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void CalculateDepletionAmount_MarathonSpecialist_DepletesSlower()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var marathonHorse = CreateHorseWithStats(stamina: 100, durability: 100);
        var sprintHorse = CreateHorseWithStats(stamina: 0, durability: 0);
        var furlongs = 16m; // Marathon distance
        var speed = 0.0422;
        var progress = 0.50;

        // Act
        var marathonDepletion = calculator.CalculateDepletionAmount(marathonHorse, furlongs, speed, speed, progress);
        var sprintDepletion = calculator.CalculateDepletionAmount(sprintHorse, furlongs, speed, speed, progress);

        // Assert
        Assert.True(marathonDepletion < sprintDepletion,
            "Marathon specialist should deplete stamina much slower than sprinter");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void CalculateDepletionAmount_LongerRace_DepletesMorePerTick()
    {
        // Arrange
        var calculator = new StaminaCalculator();
        var horse = CreateHorseWithStats(stamina: 50, durability: 50);
        var speed = 0.0422;
        var progress = 0.50;

        // Act
        var sprintDepletion = calculator.CalculateDepletionAmount(horse, 4m, speed, speed, progress);
        var marathonDepletion = calculator.CalculateDepletionAmount(horse, 16m, speed, speed, progress);

        // Assert
        Assert.True(marathonDepletion > sprintDepletion,
            "Longer races should have higher per-tick depletion rate");
    }

    private static Horse CreateHorseWithStats(
        int speed = 50,
        int stamina = 50,
        int agility = 50,
        int durability = 50,
        LegTypeId legType = LegTypeId.FrontRunner)
    {
        return new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Test Horse",
            LegTypeId = legType,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Speed, Actual = (byte)speed },
                new() { StatisticId = StatisticId.Stamina, Actual = (byte)stamina },
                new() { StatisticId = StatisticId.Agility, Actual = (byte)agility },
                new() { StatisticId = StatisticId.Durability, Actual = (byte)durability }
            }
        };
    }
}
