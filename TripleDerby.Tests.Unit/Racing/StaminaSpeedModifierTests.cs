using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Racing.Calculators;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Racing;

/// <summary>
/// Unit tests for stamina-based speed modifier calculation.
/// Tests the penalty curve that reduces horse speed as stamina depletes.
/// </summary>
public class StaminaSpeedModifierTests
{
    [Trait("Category", "StaminaSpeedModifier")]
    [Theory]
    [InlineData(1.00, 1.00)]   // 100% stamina = no penalty
    [InlineData(0.75, 0.995)]  // 75% stamina = 0.5% penalty
    [InlineData(0.50, 0.9675)] // 50% stamina = 3.25% penalty (edge of quadratic)
    [InlineData(0.25, 0.939)]  // 25% stamina = 6.1% penalty (quadratic curve)
    [InlineData(0.10, 0.917)]  // 10% stamina = 8.3% penalty
    [InlineData(0.00, 0.90)]   // 0% stamina = 10% penalty (max penalty)
    public void CalculateStaminaModifier_ReturnsCorrectSpeedPenalty(double staminaPercent, double expectedModifier)
    {
        // Arrange
        var calculator = new SpeedModifierCalculator(new TestRandomGenerator());
        var raceRunHorse = CreateRaceRunHorseWithStamina(100, staminaPercent * 100);

        // Act
        var actualModifier = calculator.CalculateStaminaModifier(raceRunHorse);

        // Assert
        Assert.Equal(expectedModifier, actualModifier, precision: 3);
    }

    [Trait("Category", "StaminaSpeedModifier")]
    [Fact]
    public void CalculateStaminaModifier_FullStamina_NoPenalty()
    {
        // Arrange
        var calculator = new SpeedModifierCalculator(new TestRandomGenerator());
        var raceRunHorse = CreateRaceRunHorseWithStamina(100, 100);

        // Act
        var modifier = calculator.CalculateStaminaModifier(raceRunHorse);

        // Assert
        Assert.Equal(1.00, modifier, precision: 3);
    }

    [Trait("Category", "StaminaSpeedModifier")]
    [Fact]
    public void CalculateStaminaModifier_ZeroStamina_MaxPenalty()
    {
        // Arrange
        var calculator = new SpeedModifierCalculator(new TestRandomGenerator());
        var raceRunHorse = CreateRaceRunHorseWithStamina(100, 0);

        // Act
        var modifier = calculator.CalculateStaminaModifier(raceRunHorse);

        // Assert
        Assert.Equal(0.90, modifier, precision: 2); // 10% penalty
    }

    [Trait("Category", "StaminaSpeedModifier")]
    [Fact]
    public void CalculateStaminaModifier_AboveHalfStamina_MinimalPenalty()
    {
        // Arrange
        var calculator = new SpeedModifierCalculator(new TestRandomGenerator());
        var highStaminaHorse = CreateRaceRunHorseWithStamina(100, 80);
        var midStaminaHorse = CreateRaceRunHorseWithStamina(100, 60);

        // Act
        var highModifier = calculator.CalculateStaminaModifier(highStaminaHorse);
        var midModifier = calculator.CalculateStaminaModifier(midStaminaHorse);

        // Assert
        Assert.True(highModifier > 0.99, "Above 75% stamina should have minimal penalty");
        Assert.True(midModifier > 0.99, "Above 50% stamina should have minimal penalty");
    }

    [Trait("Category", "StaminaSpeedModifier")]
    [Fact]
    public void CalculateStaminaModifier_BelowHalfStamina_ProgressivePenalty()
    {
        // Arrange
        var calculator = new SpeedModifierCalculator(new TestRandomGenerator());
        var halfStaminaHorse = CreateRaceRunHorseWithStamina(100, 50);
        var quarterStaminaHorse = CreateRaceRunHorseWithStamina(100, 25);
        var emptyStaminaHorse = CreateRaceRunHorseWithStamina(100, 0);

        // Act
        var halfModifier = calculator.CalculateStaminaModifier(halfStaminaHorse);
        var quarterModifier = calculator.CalculateStaminaModifier(quarterStaminaHorse);
        var emptyModifier = calculator.CalculateStaminaModifier(emptyStaminaHorse);

        // Assert
        Assert.True(halfModifier > quarterModifier, "50% stamina penalty should be less than 25%");
        Assert.True(quarterModifier > emptyModifier, "25% stamina penalty should be less than 0%");

        // Verify quadratic progression (penalty accelerates)
        var penaltyAt50 = 1.0 - halfModifier;   // Should be ~0.0325 (3.25%)
        var penaltyAt25 = 1.0 - quarterModifier; // Should be ~0.061 (6.1%)
        var penaltyAt0 = 1.0 - emptyModifier;    // Should be 0.10 (10%)

        // Quadratic means penalty accelerates, but not necessarily doubles
        // At 50% stamina: fatigueLevel² = 0.25, penalty ≈ 3.25%
        // At 25% stamina: fatigueLevel² = 0.5625, penalty ≈ 6.1%
        // Ratio: 6.1 / 3.25 ≈ 1.88x (not quite double, but accelerating)
        Assert.True(penaltyAt25 > penaltyAt50 * 1.5, "Penalty at 25% should be significantly more than at 50% (quadratic)");
    }

    [Trait("Category", "StaminaSpeedModifier")]
    [Trait("Category", "StaminaSpeedModifier")]
    [Theory]
    [InlineData(100, 120, 1.00)]   // Over 100% stamina (edge case) = no penalty
    [InlineData(100, 100, 1.00)]   // Exactly 100% stamina = no penalty
    [InlineData(50, 50, 1.00)]     // Full of current max = no penalty
    [InlineData(50, 0, 0.90)]      // Empty stamina = max penalty (10%)
    public void CalculateStaminaModifier_HandlesEdgeCases(double initialStamina, double currentStamina, double expectedModifier)
    {
        // Arrange
        var calculator = new SpeedModifierCalculator(new TestRandomGenerator());
        var raceRunHorse = CreateRaceRunHorseWithStamina(initialStamina, currentStamina);

        // Act
        var actualModifier = calculator.CalculateStaminaModifier(raceRunHorse);

        // Assert
        Assert.Equal(expectedModifier, actualModifier, precision: 2);
    }

    [Trait("Category", "StaminaSpeedModifier")]
    [Fact]
    public void CalculateStaminaModifier_LowerStamina_GreaterPenalty()
    {
        // Arrange
        var calculator = new SpeedModifierCalculator(new TestRandomGenerator());
        var highStaminaHorse = CreateRaceRunHorseWithStamina(100, 70);
        var lowStaminaHorse = CreateRaceRunHorseWithStamina(100, 30);

        // Act
        var highModifier = calculator.CalculateStaminaModifier(highStaminaHorse);
        var lowModifier = calculator.CalculateStaminaModifier(lowStaminaHorse);

        // Assert
        Assert.True(lowModifier < highModifier, "Lower stamina should result in greater speed penalty");
    }

    [Trait("Category", "StaminaSpeedModifier")]
    [Fact]
    public void CalculateStaminaModifier_UsesMildPenaltyCurve()
    {
        // Arrange
        var calculator = new SpeedModifierCalculator(new TestRandomGenerator());
        var exhaustedHorse = CreateRaceRunHorseWithStamina(100, 0);

        // Act
        var modifier = calculator.CalculateStaminaModifier(exhaustedHorse);

        // Assert
        // Max penalty should be 10% (0.90 minimum modifier)
        Assert.InRange(modifier, 0.89, 0.91); // 10% penalty ±1%
    }

    private static RaceRunHorse CreateRaceRunHorseWithStamina(double initialStamina, double currentStamina)
    {
        return new RaceRunHorse
        {
            Id = Guid.NewGuid(),
            InitialStamina = (byte)initialStamina,
            CurrentStamina = currentStamina,
            Horse = new Horse
            {
                Id = Guid.NewGuid(),
                Name = "Test Horse",
                Statistics = new List<HorseStatistic>
                {
                    new() { StatisticId = StatisticId.Stamina, Actual = (byte)initialStamina }
                }
            }
        };
    }

    /// <summary>
    /// Test random generator that returns predictable values.
    /// Required for SpeedModifierCalculator constructor.
    /// </summary>
    private class TestRandomGenerator : IRandomGenerator
    {
        public int Next() => 0;
        public int Next(int maxValue) => 0;
        public int Next(int minValue, int maxValue) => minValue;
        public double NextDouble() => 0.5;
    }
}
