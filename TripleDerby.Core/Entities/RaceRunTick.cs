using System.ComponentModel.DataAnnotations;

namespace TripleDerby.Core.Entities;

public class RaceRunTick
{
    [Key]
    public Guid Id { get; set; }

    public Guid RaceRunId { get; set; }

    public virtual RaceRun RaceRun { get; set; } = default!;

    public byte Tick { get; set; }

    public string Note { get; set; } = default!;

    public virtual ICollection<RaceRunTickHorse> RaceRunTickHorses { get; set; } = default!;
}
