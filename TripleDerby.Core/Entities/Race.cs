using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class Race
{
    [Key]
    public byte Id { get; set; }

    public string Name { get; set; } = default!;

    public string Description { get; set; } = default!;

    public decimal Furlongs { get; set; }

    public TrackId TrackId { get; set; }

    public virtual Track Track { get; set; } = default!;

    public SurfaceId SurfaceId { get; set; }

    public virtual Surface Surface { get; set; } = default!;
}
