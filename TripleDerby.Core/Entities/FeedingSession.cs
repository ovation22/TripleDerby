using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

/// <summary>
/// Records a single feeding session for a horse.
/// Tracks the feeding type, effects applied, and preference discovery.
/// </summary>
public class FeedingSession
{
    [Key]
    public Guid Id { get; set; }

    public byte FeedingId { get; set; }

    public virtual Feeding Feeding { get; set; } = null!;

    public Guid HorseId { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    /// <summary>
    /// The horse's preference response for this feeding type.
    /// </summary>
    public FeedResponse Result { get; set; }

    /// <summary>
    /// When the feeding session occurred.
    /// </summary>
    public DateTime SessionDate { get; set; }

    /// <summary>
    /// Number of race starts the horse had at time of feeding.
    /// Used to track career phase for analytics.
    /// </summary>
    public short RaceStartsAtTime { get; set; }

    /// <summary>
    /// Happiness gained (or lost) from this feeding.
    /// Can be negative for upset stomach from hated food.
    /// </summary>
    public double HappinessGain { get; set; }

    /// <summary>
    /// Speed stat gained from this feeding.
    /// </summary>
    public double SpeedGain { get; set; }

    /// <summary>
    /// Stamina stat gained from this feeding.
    /// </summary>
    public double StaminaGain { get; set; }

    /// <summary>
    /// Agility stat gained from this feeding.
    /// </summary>
    public double AgilityGain { get; set; }

    /// <summary>
    /// Durability stat gained from this feeding.
    /// </summary>
    public double DurabilityGain { get; set; }

    /// <summary>
    /// Whether this was the first time the horse tried this feed type,
    /// meaning the preference was discovered during this session.
    /// </summary>
    public bool PreferenceDiscovered { get; set; }
}
