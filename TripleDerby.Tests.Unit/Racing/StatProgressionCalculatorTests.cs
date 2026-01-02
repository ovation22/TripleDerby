using TripleDerby.Services.Racing.Calculators;

namespace TripleDerby.Tests.Unit.Racing;

/// <summary>
/// Tests for StatProgressionCalculator (Feature 018: Race Outcome Stat Progression).
/// Validates career phase multipliers, stat growth formulas, race-type focus,
/// performance bonuses, and happiness changes.
/// </summary>
public class StatProgressionCalculatorTests
{
    // ============================================================================
    // Phase 1: Career Phase System Tests
    // ============================================================================

    [Fact]
    public void CalculateAgeMultiplier_WithYoungHorse_Returns0Point80()
    {
        // Arrange - Young horse has 5 races
        var calculator = new StatProgressionCalculator();

        // Act
        var result = calculator.CalculateAgeMultiplier(raceStarts: 5);

        // Assert
        Assert.Equal(0.80, result);
    }

    [Fact]
    public void CalculateAgeMultiplier_WithPrimeHorse_Returns1Point20()
    {
        // Arrange - Prime horse has 15 races
        var calculator = new StatProgressionCalculator();

        // Act
        var result = calculator.CalculateAgeMultiplier(raceStarts: 15);

        // Assert
        Assert.Equal(1.20, result);
    }

    [Fact]
    public void CalculateAgeMultiplier_WithVeteranHorse_Returns0Point60()
    {
        // Arrange - Veteran horse has 35 races
        var calculator = new StatProgressionCalculator();

        // Act
        var result = calculator.CalculateAgeMultiplier(raceStarts: 35);

        // Assert
        Assert.Equal(0.60, result);
    }

    [Fact]
    public void CalculateAgeMultiplier_WithOldHorse_Returns0Point20()
    {
        // Arrange - Old horse has 55 races
        var calculator = new StatProgressionCalculator();

        // Act
        var result = calculator.CalculateAgeMultiplier(raceStarts: 55);

        // Assert
        Assert.Equal(0.20, result);
    }

    [Theory]
    [InlineData(0, 0.80)]   // Young lower bound
    [InlineData(9, 0.80)]   // Young upper bound
    [InlineData(10, 1.20)]  // Prime lower bound
    [InlineData(29, 1.20)]  // Prime upper bound
    [InlineData(30, 0.60)]  // Veteran lower bound
    [InlineData(49, 0.60)]  // Veteran upper bound
    [InlineData(50, 0.20)]  // Old lower bound
    [InlineData(100, 0.20)] // Old edge case
    public void CalculateAgeMultiplier_WithBoundaryValues_ReturnsCorrectMultiplier(
        short raceStarts,
        double expected)
    {
        // Arrange
        var calculator = new StatProgressionCalculator();

        // Act
        var result = calculator.CalculateAgeMultiplier(raceStarts);

        // Assert
        Assert.Equal(expected, result);
    }

    // ============================================================================
    // Phase 2: Core Stat Growth Formula Tests
    // ============================================================================

    [Fact]
    public void GrowStat_WithBasicGrowth_ReturnsCorrectIncrease()
    {
        // Arrange - Prime horse (1.20x multiplier) with 50 actual, 100 potential
        // Expected: (100 - 50) * 0.02 * 1.20 = 1.20
        var calculator = new StatProgressionCalculator();
        short actualStat = 50;
        short dominantPotential = 100;
        short raceStarts = 15; // Prime horse
        double careerMultiplier = 1.20;

        // Act
        var growth = calculator.GrowStat(actualStat, dominantPotential, careerMultiplier);

        // Assert
        Assert.Equal(1.20, growth, precision: 2);
    }

    [Fact]
    public void GrowStat_WithYoungHorse_ReturnsReducedGrowth()
    {
        // Arrange - Young horse (0.80x multiplier) with 50 actual, 100 potential
        // Expected: (100 - 50) * 0.02 * 0.80 = 0.80
        var calculator = new StatProgressionCalculator();
        short actualStat = 50;
        short dominantPotential = 100;
        double careerMultiplier = 0.80;

        // Act
        var growth = calculator.GrowStat(actualStat, dominantPotential, careerMultiplier);

        // Assert
        Assert.Equal(0.80, growth, precision: 2);
    }

    [Fact]
    public void GrowStat_WithVeteranHorse_ReturnsMinimalGrowth()
    {
        // Arrange - Veteran horse (0.60x multiplier) with 50 actual, 100 potential
        // Expected: (100 - 50) * 0.02 * 0.60 = 0.60
        var calculator = new StatProgressionCalculator();
        short actualStat = 50;
        short dominantPotential = 100;
        double careerMultiplier = 0.60;

        // Act
        var growth = calculator.GrowStat(actualStat, dominantPotential, careerMultiplier);

        // Assert
        Assert.Equal(0.60, growth, precision: 2);
    }

    [Fact]
    public void GrowStat_AtCeiling_ReturnsZeroGrowth()
    {
        // Arrange - Horse at genetic ceiling
        var calculator = new StatProgressionCalculator();
        short actualStat = 100;
        short dominantPotential = 100;
        double careerMultiplier = 1.20;

        // Act
        var growth = calculator.GrowStat(actualStat, dominantPotential, careerMultiplier);

        // Assert
        Assert.Equal(0, growth);
    }

    [Fact]
    public void GrowStat_NearCeiling_ReturnsSmallGrowth()
    {
        // Arrange - Horse very close to ceiling (95/100)
        // Expected: (100 - 95) * 0.02 * 1.20 = 0.12
        var calculator = new StatProgressionCalculator();
        short actualStat = 95;
        short dominantPotential = 100;
        double careerMultiplier = 1.20;

        // Act
        var growth = calculator.GrowStat(actualStat, dominantPotential, careerMultiplier);

        // Assert
        Assert.Equal(0.12, growth, precision: 2);
    }

    [Fact]
    public void GrowStat_LargeGap_ReturnsProportionalGrowth()
    {
        // Arrange - Large gap between actual and potential (20/100)
        // Expected: (100 - 20) * 0.02 * 1.20 = 1.92
        var calculator = new StatProgressionCalculator();
        short actualStat = 20;
        short dominantPotential = 100;
        double careerMultiplier = 1.20;

        // Act
        var growth = calculator.GrowStat(actualStat, dominantPotential, careerMultiplier);

        // Assert
        Assert.Equal(1.92, growth, precision: 2);
    }

    [Theory]
    [InlineData(50, 100, 0.80, 0.80)]  // Young horse
    [InlineData(50, 100, 1.20, 1.20)]  // Prime horse
    [InlineData(50, 100, 0.60, 0.60)]  // Veteran horse
    [InlineData(50, 100, 0.20, 0.20)]  // Old horse
    [InlineData(100, 100, 1.20, 0)]    // At ceiling
    [InlineData(99, 100, 1.20, 0.024)] // 1 point from ceiling
    public void GrowStat_WithVariousScenarios_ReturnsCorrectGrowth(
        short actualStat,
        short dominantPotential,
        double careerMultiplier,
        double expectedGrowth)
    {
        // Arrange
        var calculator = new StatProgressionCalculator();

        // Act
        var growth = calculator.GrowStat(actualStat, dominantPotential, careerMultiplier);

        // Assert
        Assert.Equal(expectedGrowth, growth, precision: 3);
    }
}
