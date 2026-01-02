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

    // ============================================================================
    // Phase 3: Performance Bonus System Tests
    // ============================================================================

    [Fact]
    public void CalculatePerformanceMultiplier_WithWin_Returns1Point50()
    {
        // Arrange - Horse wins race (1st place)
        var calculator = new StatProgressionCalculator();
        byte finishPosition = 1;
        byte fieldSize = 10;

        // Act
        var multiplier = calculator.CalculatePerformanceMultiplier(finishPosition, fieldSize);

        // Assert
        Assert.Equal(1.50, multiplier);
    }

    [Fact]
    public void CalculatePerformanceMultiplier_WithPlace_Returns1Point25()
    {
        // Arrange - Horse places (2nd)
        var calculator = new StatProgressionCalculator();
        byte finishPosition = 2;
        byte fieldSize = 10;

        // Act
        var multiplier = calculator.CalculatePerformanceMultiplier(finishPosition, fieldSize);

        // Assert
        Assert.Equal(1.25, multiplier);
    }

    [Fact]
    public void CalculatePerformanceMultiplier_WithShow_Returns1Point10()
    {
        // Arrange - Horse shows (3rd)
        var calculator = new StatProgressionCalculator();
        byte finishPosition = 3;
        byte fieldSize = 10;

        // Act
        var multiplier = calculator.CalculatePerformanceMultiplier(finishPosition, fieldSize);

        // Assert
        Assert.Equal(1.10, multiplier);
    }

    [Fact]
    public void CalculatePerformanceMultiplier_WithMidPack_Returns1Point00()
    {
        // Arrange - Horse finishes mid-pack (5th in 10-horse field)
        var calculator = new StatProgressionCalculator();
        byte finishPosition = 5;
        byte fieldSize = 10;

        // Act
        var multiplier = calculator.CalculatePerformanceMultiplier(finishPosition, fieldSize);

        // Assert
        Assert.Equal(1.00, multiplier);
    }

    [Fact]
    public void CalculatePerformanceMultiplier_WithBackOfPack_Returns0Point75()
    {
        // Arrange - Horse finishes back of pack (8th in 10-horse field)
        var calculator = new StatProgressionCalculator();
        byte finishPosition = 8;
        byte fieldSize = 10;

        // Act
        var multiplier = calculator.CalculatePerformanceMultiplier(finishPosition, fieldSize);

        // Assert
        Assert.Equal(0.75, multiplier);
    }

    [Theory]
    [InlineData(1, 8, 1.50)]   // Win
    [InlineData(2, 8, 1.25)]   // Place
    [InlineData(3, 8, 1.10)]   // Show
    [InlineData(4, 8, 1.00)]   // Mid-pack (4th in 8-horse field)
    [InlineData(5, 8, 1.00)]   // Mid-pack (5th in 8-horse field)
    [InlineData(6, 8, 1.00)]   // Mid-pack boundary
    [InlineData(7, 8, 0.75)]   // Back of pack (7th in 8-horse field)
    [InlineData(8, 8, 0.75)]   // Last place
    [InlineData(1, 3, 1.50)]   // Win in small field
    [InlineData(2, 3, 1.25)]   // Place in small field
    [InlineData(3, 3, 1.10)]   // Show in small field
    public void CalculatePerformanceMultiplier_WithVariousPositions_ReturnsCorrectMultiplier(
        byte finishPosition,
        byte fieldSize,
        double expected)
    {
        // Arrange
        var calculator = new StatProgressionCalculator();

        // Act
        var multiplier = calculator.CalculatePerformanceMultiplier(finishPosition, fieldSize);

        // Assert
        Assert.Equal(expected, multiplier);
    }

    // ============================================================================
    // Phase 4: Race-Type Stat Focus Tests
    // ============================================================================

    [Fact]
    public void CalculateRaceTypeFocusMultipliers_WithSprint_FavorsSpeedAndAgility()
    {
        // Arrange - Sprint race (5 furlongs)
        var calculator = new StatProgressionCalculator();
        decimal raceDistance = 5m;

        // Act
        var multipliers = calculator.CalculateRaceTypeFocusMultipliers(raceDistance);

        // Assert - Sprint emphasizes Speed and Agility
        Assert.Equal(1.50, multipliers.Speed);
        Assert.Equal(1.25, multipliers.Agility);
        Assert.Equal(0.75, multipliers.Stamina);
        Assert.Equal(0.75, multipliers.Durability);
    }

    [Fact]
    public void CalculateRaceTypeFocusMultipliers_WithDistance_FavorsStaminaAndDurability()
    {
        // Arrange - Distance race (12 furlongs)
        var calculator = new StatProgressionCalculator();
        decimal raceDistance = 12m;

        // Act
        var multipliers = calculator.CalculateRaceTypeFocusMultipliers(raceDistance);

        // Assert - Distance emphasizes Stamina and Durability
        Assert.Equal(0.75, multipliers.Speed);
        Assert.Equal(0.75, multipliers.Agility);
        Assert.Equal(1.50, multipliers.Stamina);
        Assert.Equal(1.25, multipliers.Durability);
    }

    [Fact]
    public void CalculateRaceTypeFocusMultipliers_WithClassic_BalancedGrowth()
    {
        // Arrange - Classic race (8 furlongs)
        var calculator = new StatProgressionCalculator();
        decimal raceDistance = 8m;

        // Act
        var multipliers = calculator.CalculateRaceTypeFocusMultipliers(raceDistance);

        // Assert - Classic provides balanced growth
        Assert.Equal(1.00, multipliers.Speed);
        Assert.Equal(1.00, multipliers.Agility);
        Assert.Equal(1.00, multipliers.Stamina);
        Assert.Equal(1.00, multipliers.Durability);
    }

    [Theory]
    [InlineData(5, 1.50, 1.25, 0.75, 0.75)]   // Sprint (5f)
    [InlineData(6, 1.50, 1.25, 0.75, 0.75)]   // Sprint boundary (6f)
    [InlineData(7, 1.00, 1.00, 1.00, 1.00)]   // Classic lower (7f)
    [InlineData(8, 1.00, 1.00, 1.00, 1.00)]   // Classic mid (8f)
    [InlineData(10, 1.00, 1.00, 1.00, 1.00)]  // Classic upper (10f)
    [InlineData(11, 0.75, 0.75, 1.50, 1.25)]  // Distance boundary (11f)
    [InlineData(12, 0.75, 0.75, 1.50, 1.25)]  // Distance (12f)
    public void CalculateRaceTypeFocusMultipliers_WithVariousDistances_ReturnsCorrectMultipliers(
        decimal distance,
        double expectedSpeed,
        double expectedAgility,
        double expectedStamina,
        double expectedDurability)
    {
        // Arrange
        var calculator = new StatProgressionCalculator();

        // Act
        var multipliers = calculator.CalculateRaceTypeFocusMultipliers(distance);

        // Assert
        Assert.Equal(expectedSpeed, multipliers.Speed);
        Assert.Equal(expectedAgility, multipliers.Agility);
        Assert.Equal(expectedStamina, multipliers.Stamina);
        Assert.Equal(expectedDurability, multipliers.Durability);
    }
}
