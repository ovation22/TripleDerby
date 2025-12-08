using System.ComponentModel.DataAnnotations;

namespace TripleDerby.Core.Entities;

public class RaceRunHorse
{
    [Key]
    public Guid Id { get; set; }

    public Guid RaceRunId { get; set; }

    public virtual RaceRun RaceRun { get; set; } = default!;

    public Guid HorseId { get; set; }

    public virtual Horse Horse { get; set; } = default!;

    public byte Lane { get; set; }
}
