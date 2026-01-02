using TripleDerby.Services.Racing.Racing;

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
}
