using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripleDerby.Core.Entities;

public class RaceRunHorse
{
    [Key]
    public Guid Id { get; set; }

    public Guid RaceRunId { get; set; }

    public virtual RaceRun RaceRun { get; set; } = null!;

    public Guid HorseId { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public byte Lane { get; set; }

    public double InitialStamina { get; set; }

    public double CurrentStamina { get; set; }

    public decimal Distance { get; set; }

    public byte Place { get; set; }

    public double Time { get; set; }

    public int Payout { get; set; }

    /// <summary>
    /// Tracks ticks since last lane change for cooldown calculation.
    /// Reset to 0 when lane change attempted (success or failure).
    /// Agility-based cooldown: 10 - (Agility × 0.1) ticks required between attempts.
    /// </summary>
    [NotMapped]
    public short TicksSinceLastLaneChange { get; set; }

    /// <summary>
    /// Remaining ticks of speed penalty from risky lane change.
    /// Applied when successful risky squeeze play occurs.
    /// Duration based on Durability: 5 - (Durability × 0.04) ticks.
    /// Penalty magnitude: 0.95x speed (5% reduction).
    /// </summary>
    [NotMapped]
    public byte SpeedPenaltyTicksRemaining { get; set; }
}
