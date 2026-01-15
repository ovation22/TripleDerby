using TripleDerby.Services.Feeding.Calculators;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Services;

public class FeedingCalculatorTests
{
    private readonly FeedingCalculator _calculator = new();

    #region CalculatePreference Tests

    [Fact]
    public void CalculatePreference_ReturnsSamePreferenceForSameHorseAndFeed()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        byte feedingId = 1;
        var category = FeedingCategoryId.Treats;

        // Act
        var result1 = _calculator.CalculatePreference(horseId, feedingId, category);
        var result2 = _calculator.CalculatePreference(horseId, feedingId, category);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void CalculatePreference_ReturnsDifferentPreferencesForDifferentFeeds()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var category = FeedingCategoryId.Treats;

        // Act
        var results = new List<FeedResponse>();
        for (byte i = 1; i <= 10; i++)
        {
            results.Add(_calculator.CalculatePreference(horseId, i, category));
        }

        // Assert - Should have at least some variation
        Assert.True(results.Distinct().Count() > 1);
    }

    [Fact]
    public void CalculatePreference_TreatsCategoryFavorsHigherPreferences()
    {
        // Arrange
        var category = FeedingCategoryId.Treats;
        var favorites = 0;
        var hated = 0;
        var iterations = 1000;

        // Act - Test many horses
        for (int i = 0; i < iterations; i++)
        {
            var horseId = Guid.NewGuid();
            var result = _calculator.CalculatePreference(horseId, 1, category);
            if (result == FeedResponse.Favorite) favorites++;
            if (result == FeedResponse.Hated) hated++;
        }

        // Assert - Treats should have more favorites than hated (40% vs 3%)
        Assert.True(favorites > hated);
    }

    [Fact]
    public void CalculatePreference_SupplementsCategoryFavorsNeutral()
    {
        // Arrange
        var category = FeedingCategoryId.Supplements;
        var neutral = 0;
        var favorites = 0;
        var iterations = 1000;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var horseId = Guid.NewGuid();
            var result = _calculator.CalculatePreference(horseId, 1, category);
            if (result == FeedResponse.Neutral) neutral++;
            if (result == FeedResponse.Favorite) favorites++;
        }

        // Assert - Supplements should have more neutral than favorite (45% vs 10%)
        Assert.True(neutral > favorites);
    }

    #endregion

    #region CalculateHappinessGain Tests

    [Fact]
    public void CalculateHappinessGain_ReturnsValueInExpectedRange()
    {
        // Arrange
        double baseMin = 5.0;
        double baseMax = 10.0;
        var preference = FeedResponse.Neutral;
        double effectiveness = 1.0;

        // Act
        var result = _calculator.CalculateHappinessGain(baseMin, baseMax, preference, effectiveness);

        // Assert - With neutral (1.0x) and full effectiveness, should be between 5-10
        Assert.True(result >= baseMin && result <= baseMax);
    }

    [Fact]
    public void CalculateHappinessGain_AppliesPreferenceMultiplier()
    {
        // Arrange
        double baseMin = 10.0;
        double baseMax = 10.0; // Fixed value for predictable test
        double effectiveness = 1.0;

        // Act
        var favorite = _calculator.CalculateHappinessGain(baseMin, baseMax, FeedResponse.Favorite, effectiveness);
        var neutral = _calculator.CalculateHappinessGain(baseMin, baseMax, FeedResponse.Neutral, effectiveness);
        var hated = _calculator.CalculateHappinessGain(baseMin, baseMax, FeedResponse.Hated, effectiveness);

        // Assert
        Assert.True(favorite > neutral); // 1.5x vs 1.0x
        Assert.True(neutral > hated);    // 1.0x vs 0.5x
    }

    [Fact]
    public void CalculateHappinessGain_AppliesEffectivenessModifier()
    {
        // Arrange
        double baseMin = 10.0;
        double baseMax = 10.0;
        var preference = FeedResponse.Neutral;

        // Act
        var fullEffectiveness = _calculator.CalculateHappinessGain(baseMin, baseMax, preference, 1.0);
        var halfEffectiveness = _calculator.CalculateHappinessGain(baseMin, baseMax, preference, 0.5);

        // Assert
        Assert.True(fullEffectiveness > halfEffectiveness);
    }

    #endregion

    #region CalculateStatGain Tests

    [Fact]
    public void CalculateStatGain_ReturnsZeroWhenAtCeiling()
    {
        // Arrange
        double currentStat = 100.0;
        double dominantPotential = 100.0;
        double baseMin = 1.0;
        double baseMax = 2.0;
        var preference = FeedResponse.Neutral;
        double effectiveness = 1.0;

        // Act
        var result = _calculator.CalculateStatGain(currentStat, dominantPotential, baseMin, baseMax, preference, effectiveness);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateStatGain_ReturnsZeroWhenAboveCeiling()
    {
        // Arrange
        double currentStat = 105.0;
        double dominantPotential = 100.0;
        double baseMin = 1.0;
        double baseMax = 2.0;
        var preference = FeedResponse.Neutral;
        double effectiveness = 1.0;

        // Act
        var result = _calculator.CalculateStatGain(currentStat, dominantPotential, baseMin, baseMax, preference, effectiveness);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateStatGain_CapsAtRemainingRoom()
    {
        // Arrange
        double currentStat = 99.5;
        double dominantPotential = 100.0;
        double baseMin = 2.0;
        double baseMax = 2.0; // Would gain 2.0, but only 0.5 room
        var preference = FeedResponse.Neutral;
        double effectiveness = 1.0;

        // Act
        var result = _calculator.CalculateStatGain(currentStat, dominantPotential, baseMin, baseMax, preference, effectiveness);

        // Assert - Should cap at 0.5 (remaining room)
        Assert.Equal(0.5, result);
    }

    [Fact]
    public void CalculateStatGain_AppliesPreferenceAndEffectiveness()
    {
        // Arrange
        double currentStat = 50.0;
        double dominantPotential = 100.0;
        double baseMin = 10.0;
        double baseMax = 10.0;
        double effectiveness = 1.0;

        // Act
        var favorite = _calculator.CalculateStatGain(currentStat, dominantPotential, baseMin, baseMax, FeedResponse.Favorite, effectiveness);
        var neutral = _calculator.CalculateStatGain(currentStat, dominantPotential, baseMin, baseMax, FeedResponse.Neutral, effectiveness);

        // Assert - Favorite should give more gain (1.5x vs 1.0x)
        Assert.True(favorite > neutral);
    }

    #endregion

    #region CalculateHappinessEffectivenessModifier Tests

    [Fact]
    public void CalculateHappinessEffectivenessModifier_ReturnsMinAtZeroHappiness()
    {
        // Act
        var result = _calculator.CalculateHappinessEffectivenessModifier(0.0);

        // Assert
        Assert.Equal(0.5, result);
    }

    [Fact]
    public void CalculateHappinessEffectivenessModifier_ReturnsMaxAtFullHappiness()
    {
        // Act
        var result = _calculator.CalculateHappinessEffectivenessModifier(100.0);

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateHappinessEffectivenessModifier_ReturnsLinearScaling()
    {
        // Act
        var at25 = _calculator.CalculateHappinessEffectivenessModifier(25.0);
        var at50 = _calculator.CalculateHappinessEffectivenessModifier(50.0);
        var at75 = _calculator.CalculateHappinessEffectivenessModifier(75.0);

        // Assert - Should increase linearly
        Assert.True(at25 > 0.5 && at25 < at50);
        Assert.True(at50 > at25 && at50 < at75);
        Assert.True(at75 > at50 && at75 < 1.0);
    }

    #endregion

    #region RollUpsetStomach Tests

    [Fact]
    public void RollUpsetStomach_ReturnsSameResultForSameInputs()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        byte feedingId = 1;
        var sessionDate = new DateTime(2024, 1, 15);

        // Act
        var result1 = _calculator.RollUpsetStomach(horseId, feedingId, sessionDate);
        var result2 = _calculator.RollUpsetStomach(horseId, feedingId, sessionDate);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void RollUpsetStomach_OccursAtExpectedRate()
    {
        // Arrange
        var sessionDate = new DateTime(2024, 1, 15);
        byte feedingId = 1;
        var upsetCount = 0;
        var iterations = 1000;

        // Act - Test many horses
        for (int i = 0; i < iterations; i++)
        {
            var horseId = Guid.NewGuid();
            if (_calculator.RollUpsetStomach(horseId, feedingId, sessionDate))
            {
                upsetCount++;
            }
        }

        // Assert - Should be around 30% (300 out of 1000), allow 20-40% range
        var percentage = (double)upsetCount / iterations;
        Assert.True(percentage >= 0.20 && percentage <= 0.40);
    }

    #endregion

    #region CalculateCareerPhaseModifier Tests

    [Fact]
    public void CalculateCareerPhaseModifier_ReturnsUnracedModifierForNewHorse()
    {
        // Arrange
        short raceStarts = 0;
        bool isRetired = false;

        // Act
        var result = _calculator.CalculateCareerPhaseModifier(raceStarts, isRetired);

        // Assert
        Assert.Equal(1.1, result);
    }

    [Fact]
    public void CalculateCareerPhaseModifier_ReturnsActiveModifierForActiveHorse()
    {
        // Arrange
        short raceStarts = 10;
        bool isRetired = false;

        // Act
        var result = _calculator.CalculateCareerPhaseModifier(raceStarts, isRetired);

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateCareerPhaseModifier_ReturnsRetiredModifierForRetiredHorse()
    {
        // Arrange
        short raceStarts = 50;
        bool isRetired = true;

        // Act
        var result = _calculator.CalculateCareerPhaseModifier(raceStarts, isRetired);

        // Assert
        Assert.Equal(0.8, result);
    }

    [Fact]
    public void CalculateCareerPhaseModifier_ReturnsRetiredModifierEvenWithZeroRaces()
    {
        // Arrange - Edge case: retired without racing
        short raceStarts = 0;
        bool isRetired = true;

        // Act
        var result = _calculator.CalculateCareerPhaseModifier(raceStarts, isRetired);

        // Assert
        Assert.Equal(0.8, result);
    }

    #endregion

    #region CalculateLegTypeBonus Tests

    [Fact]
    public void CalculateLegTypeBonus_ReturnsBonusForFrontRunnerWithGrains()
    {
        // Act
        var result = _calculator.CalculateLegTypeBonus(LegTypeId.FrontRunner, FeedingCategoryId.Grains);

        // Assert
        Assert.Equal(1.1, result);
    }

    [Fact]
    public void CalculateLegTypeBonus_ReturnsBonusForStartDashWithProteins()
    {
        // Act
        var result = _calculator.CalculateLegTypeBonus(LegTypeId.StartDash, FeedingCategoryId.Proteins);

        // Assert
        Assert.Equal(1.1, result);
    }

    [Fact]
    public void CalculateLegTypeBonus_ReturnsBonusForRailRunnerWithSupplements()
    {
        // Act
        var result = _calculator.CalculateLegTypeBonus(LegTypeId.RailRunner, FeedingCategoryId.Supplements);

        // Assert
        Assert.Equal(1.1, result);
    }

    [Fact]
    public void CalculateLegTypeBonus_ReturnsSmallBonusForAllTypesWithPremium()
    {
        // Act
        var frontRunner = _calculator.CalculateLegTypeBonus(LegTypeId.FrontRunner, FeedingCategoryId.Premium);
        var startDash = _calculator.CalculateLegTypeBonus(LegTypeId.StartDash, FeedingCategoryId.Premium);
        var railRunner = _calculator.CalculateLegTypeBonus(LegTypeId.RailRunner, FeedingCategoryId.Premium);

        // Assert - All should have 1.05 bonus for premium
        Assert.Equal(1.05, frontRunner);
        Assert.Equal(1.05, startDash);
        Assert.Equal(1.05, railRunner);
    }

    [Fact]
    public void CalculateLegTypeBonus_ReturnsNoBonusForTreats()
    {
        // Act
        var result = _calculator.CalculateLegTypeBonus(LegTypeId.FrontRunner, FeedingCategoryId.Treats);

        // Assert - No bonuses for treats
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateLegTypeBonus_ReturnsNoBonusWhenNoMatch()
    {
        // Act - StartDash doesn't get bonus for Grains (only FrontRunner/StretchRunner do)
        var result = _calculator.CalculateLegTypeBonus(LegTypeId.StartDash, FeedingCategoryId.Grains);

        // Assert
        Assert.Equal(1.0, result);
    }

    #endregion
}
