using System.ComponentModel.DataAnnotations;

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

    public byte InitialStamina { get; set; }

    public double CurrentStamina { get; set; }

    public decimal Distance { get; set; }

    public byte Place { get; set; }

    public double Time { get; set; }
}
