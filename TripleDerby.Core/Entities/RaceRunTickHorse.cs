using System.ComponentModel.DataAnnotations;

namespace TripleDerby.Core.Entities;

public class RaceRunTickHorse
{
    [Key]
    public Guid Id { get; set; }

    public Guid RaceRunTickId { get; set; }

    public Guid? HorseId { get; set; }

    public virtual Horse Horse { get; set; } = default!;

    public byte Lane { get; set; }

    public byte Distance { get; set; }
}
