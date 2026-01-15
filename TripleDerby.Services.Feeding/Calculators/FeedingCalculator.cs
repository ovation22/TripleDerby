using TripleDerby.Services.Feeding.Abstractions;
using TripleDerby.Services.Feeding.Config;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Feeding.Calculators;

/// <summary>
/// Calculator for feeding-related computations.
/// Uses deterministic seeding for preference generation and effect calculations.
/// </summary>
public class FeedingCalculator : IFeedingCalculator
{
    public FeedResponse CalculatePreference(Guid horseId, byte feedingId, FeedingCategoryId categoryId)
    {
        // Generate deterministic seed from horse + feeding combination
        var seed = HashCode.Combine(horseId, feedingId);
        var random = new Random(seed);

        // Get category-specific preference weights
        var weights = FeedingConfig.CategoryPreferenceWeights[categoryId];
        var totalWeight = weights.Sum();

        // Roll for preference using weighted probability
        var roll = random.Next(totalWeight);
        var cumulative = 0;

        // Map roll to preference level
        // weights format: [Favorite, Liked, Neutral, Disliked, Hated]
        var preferences = new[]
        {
            FeedResponse.Favorite,
            FeedResponse.Liked,
            FeedResponse.Neutral,
            FeedResponse.Disliked,
            FeedResponse.Hated
        };

        for (var i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
            {
                return preferences[i];
            }
        }

        // Fallback (should never reach here)
        return FeedResponse.Neutral;
    }

    public double CalculateHappinessGain(
        double baseHappinessMin,
        double baseHappinessMax,
        FeedResponse preference,
        double happinessEffectiveness)
    {
        // Get preference multiplier
        var preferenceMultiplier = FeedingConfig.PreferenceMultipliers[preference];

        // Generate random value in base range
        var random = new Random();
        var baseGain = baseHappinessMin + (random.NextDouble() * (baseHappinessMax - baseHappinessMin));

        // Apply preference multiplier and happiness effectiveness
        return baseGain * preferenceMultiplier * happinessEffectiveness;
    }

    public double CalculateStatGain(
        double currentStatValue,
        double dominantPotential,
        double baseStatMin,
        double baseStatMax,
        FeedResponse preference,
        double happinessEffectiveness)
    {
        // No gains if at or above genetic ceiling
        if (currentStatValue >= dominantPotential)
        {
            return 0.0;
        }

        // Get preference multiplier
        var preferenceMultiplier = FeedingConfig.PreferenceMultipliers[preference];

        // Generate random value in base range
        var random = new Random();
        var baseGain = baseStatMin + (random.NextDouble() * (baseStatMax - baseStatMin));

        // Apply preference multiplier and happiness effectiveness
        var gain = baseGain * preferenceMultiplier * happinessEffectiveness;

        // Ensure we don't exceed ceiling
        var remainingRoom = dominantPotential - currentStatValue;
        return Math.Min(gain, remainingRoom);
    }

    public double CalculateHappinessEffectivenessModifier(double currentHappiness)
    {
        // Linear scale from 0.5 (at 0 happiness) to 1.0 (at 100 happiness)
        // Formula: 0.5 + (currentHappiness / 100 * 0.5)
        var normalized = Math.Clamp(currentHappiness / 100.0, 0.0, 1.0);
        return FeedingConfig.MinHappinessEffectiveness +
               (normalized * (FeedingConfig.MaxHappinessEffectiveness - FeedingConfig.MinHappinessEffectiveness));
    }

    public bool RollUpsetStomach(Guid horseId, byte feedingId, DateTime sessionDate)
    {
        // Generate deterministic seed from horse + feeding + date
        var seed = HashCode.Combine(horseId, feedingId, sessionDate.Date);
        var random = new Random(seed);

        // Roll for upset stomach (30% chance)
        var roll = random.NextDouble();
        return roll < FeedingConfig.UpsetStomachChance;
    }

    public double CalculateCareerPhaseModifier(short raceStarts, bool isRetired)
    {
        if (isRetired)
        {
            return FeedingConfig.RetiredCareerModifier;
        }

        if (raceStarts == 0)
        {
            return FeedingConfig.UnracedCareerModifier;
        }

        return FeedingConfig.ActiveCareerModifier;
    }

    public double CalculateLegTypeBonus(LegTypeId legType, FeedingCategoryId categoryId)
    {
        // Check if this category has bonuses defined
        if (!FeedingConfig.LegTypeBonuses.TryGetValue(categoryId, out var bonuses))
        {
            return 1.0;
        }

        // Check if this leg type has a bonus for this category
        if (bonuses.TryGetValue(legType, out var bonus))
        {
            return bonus;
        }

        // No bonus for this leg type/category combination
        return 1.0;
    }
}
