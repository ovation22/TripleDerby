using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Feeding.Config;

/// <summary>
/// Configuration constants for the feeding system.
/// Defines preference weights, effectiveness multipliers, and upset stomach mechanics.
/// </summary>
public static class FeedingConfig
{
    /// <summary>
    /// Preference effectiveness multipliers.
    /// Applied to base happiness and stat gains.
    /// </summary>
    public static readonly Dictionary<FeedResponse, double> PreferenceMultipliers = new()
    {
        { FeedResponse.Favorite, 1.5 },   // +50% effectiveness
        { FeedResponse.Liked, 1.25 },     // +25% effectiveness
        { FeedResponse.Neutral, 1.0 },    // No modifier
        { FeedResponse.Disliked, 0.75 },  // -25% effectiveness
        { FeedResponse.Hated, 0.5 },      // -50% effectiveness
        { FeedResponse.Rejected, 0.0 }    // No effects (refused to eat)
    };

    /// <summary>
    /// Category-based preference probability weights.
    /// Determines likelihood of each preference level for different feed categories.
    /// Format: [Favorite, Liked, Neutral, Disliked, Hated]
    /// </summary>
    public static readonly Dictionary<FeedingCategoryId, int[]> CategoryPreferenceWeights = new()
    {
        // Treats: Most likely to be favorites (high happiness, no stats)
        { FeedingCategoryId.Treats, new[] { 40, 30, 20, 7, 3 } },

        // Fruits: Good balance, moderate favorites
        { FeedingCategoryId.Fruits, new[] { 25, 35, 25, 10, 5 } },

        // Grains: Balanced, slightly more neutral
        { FeedingCategoryId.Grains, new[] { 20, 30, 35, 10, 5 } },

        // Proteins: Less appealing, more neutral/disliked
        { FeedingCategoryId.Proteins, new[] { 15, 25, 35, 15, 10 } },

        // Supplements: Very neutral, least favorites
        { FeedingCategoryId.Supplements, new[] { 10, 20, 45, 15, 10 } },

        // Premium: High favorites but some horses hate fancy stuff
        { FeedingCategoryId.Premium, new[] { 35, 30, 20, 10, 5 } }
    };

    /// <summary>
    /// Chance of upset stomach when eating hated food (30%).
    /// </summary>
    public const double UpsetStomachChance = 0.30;

    /// <summary>
    /// Additional happiness penalty when upset stomach occurs.
    /// </summary>
    public const double UpsetStomachPenalty = -5.0;

    /// <summary>
    /// Happiness effectiveness modifier range.
    /// At 100 happiness: 1.0 (full effectiveness)
    /// At 0 happiness: 0.5 (half effectiveness)
    /// Linear scale between.
    /// </summary>
    public const double MinHappinessEffectiveness = 0.5;
    public const double MaxHappinessEffectiveness = 1.0;

    /// <summary>
    /// Career phase effectiveness modifiers.
    /// Unraced: Young horses benefit more from feeding (growth phase)
    /// Active: Standard effectiveness
    /// Retired: Reduced benefit (maintenance mode)
    /// </summary>
    public const double UnracedCareerModifier = 1.1;    // +10%
    public const double ActiveCareerModifier = 1.0;     // Standard
    public const double RetiredCareerModifier = 0.8;    // -20%

    /// <summary>
    /// LegType bonuses per feeding category.
    /// Certain leg types benefit more from specific feed types.
    /// </summary>
    public static readonly Dictionary<FeedingCategoryId, Dictionary<LegTypeId, double>> LegTypeBonuses = new()
    {
        // Treats: Universal appeal, no specific bonuses
        { FeedingCategoryId.Treats, new Dictionary<LegTypeId, double>() },

        // Fruits: General health, no specific bonuses
        { FeedingCategoryId.Fruits, new Dictionary<LegTypeId, double>() },

        // Grains: Endurance benefit for FrontRunner and StretchRunner
        {
            FeedingCategoryId.Grains, new Dictionary<LegTypeId, double>
            {
                { LegTypeId.FrontRunner, 1.1 },
                { LegTypeId.StretchRunner, 1.1 }
            }
        },

        // Proteins: Power benefit for StartDash and LastSpurt
        {
            FeedingCategoryId.Proteins, new Dictionary<LegTypeId, double>
            {
                { LegTypeId.StartDash, 1.1 },
                { LegTypeId.LastSpurt, 1.1 }
            }
        },

        // Supplements: Technical positioning benefit for RailRunner
        {
            FeedingCategoryId.Supplements, new Dictionary<LegTypeId, double>
            {
                { LegTypeId.RailRunner, 1.1 }
            }
        },

        // Premium: All types benefit (+5% universal bonus)
        {
            FeedingCategoryId.Premium, new Dictionary<LegTypeId, double>
            {
                { LegTypeId.FrontRunner, 1.05 },
                { LegTypeId.StartDash, 1.05 },
                { LegTypeId.LastSpurt, 1.05 },
                { LegTypeId.StretchRunner, 1.05 },
                { LegTypeId.RailRunner, 1.05 }
            }
        }
    };
}
