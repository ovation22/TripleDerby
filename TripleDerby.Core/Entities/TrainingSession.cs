using System.ComponentModel.DataAnnotations;

namespace TripleDerby.Core.Entities;

public class TrainingSession
{
    [Key]
    public Guid Id { get; set; }

    public byte TrainingId { get; set; }

    public virtual Training Training { get; set; } = null!;

    public Guid HorseId { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public DateTime SessionDate { get; set; }

    public short RaceStartsAtTime { get; set; }

    public double SpeedGain { get; set; }

    public double StaminaGain { get; set; }

    public double AgilityGain { get; set; }

    public double DurabilityGain { get; set; }

    public double HappinessChange { get; set; }

    public bool OverworkOccurred { get; set; }

    public string Result { get; set; } = null!;
}
