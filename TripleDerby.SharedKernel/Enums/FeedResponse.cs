namespace TripleDerby.SharedKernel.Enums;

/// <summary>
/// Horse preference response when fed a particular food.
/// Determines effectiveness multipliers for feeding effects.
/// </summary>
public enum FeedResponse : byte
{
    Favorite = 1,   // +50% effectiveness
    Liked = 2,      // +25% effectiveness
    Neutral = 3,    // No modifier
    Disliked = 4,   // -25% effectiveness
    Hated = 5,      // -50% effectiveness, may trigger upset stomach
    Rejected = 6    // Horse refuses to eat - no effects applied, wastes feeding opportunity
}
