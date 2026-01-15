using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

/// <summary>
/// Stores a horse's discovered preference for a specific feeding type.
/// Preferences are generated deterministically on first feeding and stored permanently.
/// </summary>
public class HorseFeedingPreference
{
    [Key]
    public Guid Id { get; set; }

    public Guid HorseId { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public byte FeedingId { get; set; }

    public virtual Feeding Feeding { get; set; } = null!;

    /// <summary>
    /// The horse's preference level for this feeding type.
    /// Determines effectiveness multipliers when this feed is given.
    /// </summary>
    public FeedResponse Preference { get; set; }

    /// <summary>
    /// When this preference was first discovered (first time horse tried this feed).
    /// </summary>
    public DateTime DiscoveredDate { get; set; }
}
