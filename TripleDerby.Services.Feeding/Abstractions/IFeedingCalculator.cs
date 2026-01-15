using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Feeding.Abstractions;

/// <summary>
/// Calculator for feeding-related computations.
/// Handles deterministic preference generation and effect calculations.
/// </summary>
public interface IFeedingCalculator
{
    /// <summary>
    /// Calculates a horse's preference for a specific feed using deterministic seeding.
    /// Same horse + feed combination will always produce the same preference.
    /// </summary>
    /// <param name="horseId">The horse's unique ID</param>
    /// <param name="feedingId">The feeding type ID</param>
    /// <param name="categoryId">The feeding category (affects probability weights)</param>
    /// <returns>The horse's preference level for this feed</returns>
    FeedResponse CalculatePreference(Guid horseId, byte feedingId, FeedingCategoryId categoryId);

    /// <summary>
    /// Calculates happiness gain from feeding, applying preference and effectiveness modifiers.
    /// </summary>
    /// <param name="baseHappinessMin">Minimum base happiness for this feed type</param>
    /// <param name="baseHappinessMax">Maximum base happiness for this feed type</param>
    /// <param name="preference">Horse's preference for this feed</param>
    /// <param name="happinessEffectiveness">Modifier based on current happiness (0.5-1.0)</param>
    /// <returns>Actual happiness gain for this feeding session</returns>
    double CalculateHappinessGain(
        double baseHappinessMin,
        double baseHappinessMax,
        FeedResponse preference,
        double happinessEffectiveness);

    /// <summary>
    /// Calculates stat gain from feeding, respecting genetic ceiling and applying modifiers.
    /// </summary>
    /// <param name="currentStatValue">Current stat value</param>
    /// <param name="dominantPotential">Genetic ceiling for this stat</param>
    /// <param name="baseStatMin">Minimum base stat gain for this feed</param>
    /// <param name="baseStatMax">Maximum base stat gain for this feed</param>
    /// <param name="preference">Horse's preference for this feed</param>
    /// <param name="happinessEffectiveness">Modifier based on current happiness</param>
    /// <returns>Actual stat gain (0 if at/above ceiling)</returns>
    double CalculateStatGain(
        double currentStatValue,
        double dominantPotential,
        double baseStatMin,
        double baseStatMax,
        FeedResponse preference,
        double happinessEffectiveness);

    /// <summary>
    /// Calculates the happiness effectiveness modifier based on current happiness.
    /// Linear scale: 0.5 at 0 happiness, 1.0 at 100 happiness.
    /// </summary>
    /// <param name="currentHappiness">Current happiness value (0-100)</param>
    /// <returns>Effectiveness modifier (0.5-1.0)</returns>
    double CalculateHappinessEffectivenessModifier(double currentHappiness);

    /// <summary>
    /// Determines if upset stomach occurs when eating hated food (30% chance).
    /// Uses deterministic seeding for reproducibility.
    /// </summary>
    /// <param name="horseId">The horse's unique ID</param>
    /// <param name="feedingId">The feeding type ID</param>
    /// <param name="sessionDate">The feeding session date (for seeding)</param>
    /// <returns>True if upset stomach occurs</returns>
    bool RollUpsetStomach(Guid horseId, byte feedingId, DateTime sessionDate);

    /// <summary>
    /// Calculates career phase modifier based on horse's racing history.
    /// Unraced: 1.1x (young horses benefit more)
    /// Active: 1.0x (standard)
    /// Retired: 0.8x (reduced benefit)
    /// </summary>
    /// <param name="raceStarts">Number of races the horse has run</param>
    /// <param name="isRetired">Whether the horse is retired</param>
    /// <returns>Career phase modifier (0.8-1.1)</returns>
    double CalculateCareerPhaseModifier(short raceStarts, bool isRetired);

    /// <summary>
    /// Calculates LegType-specific bonus for a feeding category.
    /// Different leg types benefit more from certain feed types.
    /// </summary>
    /// <param name="legType">The horse's leg type</param>
    /// <param name="categoryId">The feeding category</param>
    /// <returns>LegType bonus multiplier (1.0 if no bonus, 1.05-1.1 if bonus applies)</returns>
    double CalculateLegTypeBonus(LegTypeId legType, FeedingCategoryId categoryId);
}
