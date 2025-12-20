using System.ComponentModel.DataAnnotations;

namespace TripleDerby.Core.Entities;

public class RaceRunTick
{
    [Key]
    public Guid Id { get; set; }

    public Guid RaceRunId { get; set; }

    public virtual RaceRun RaceRun { get; set; } = null!;

    public short Tick { get; set; }

    public string Note { get; set; } = null!;

    public virtual ICollection<RaceRunTickHorse> RaceRunTickHorses { get; set; } = null!;
}
