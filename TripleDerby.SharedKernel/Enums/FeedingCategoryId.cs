namespace TripleDerby.SharedKernel.Enums;

/// <summary>
/// Categories for grouping feed types with similar effect profiles.
/// Each category has different preference probability weights.
/// </summary>
public enum FeedingCategoryId : byte
{
    /// <summary>High happiness, no stats. Most likely to be favorites.</summary>
    Treats = 1,

    /// <summary>Good happiness, minor stamina/agility.</summary>
    Fruits = 2,

    /// <summary>Moderate happiness, stamina/durability focus.</summary>
    Grains = 3,

    /// <summary>Moderate happiness, durability focus.</summary>
    Proteins = 4,

    /// <summary>Moderate happiness, balanced tiny boosts to all stats.</summary>
    Supplements = 5,

    /// <summary>High happiness, varied stat bonuses.</summary>
    Premium = 6
}
