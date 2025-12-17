using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class Race
{
    [Key]
    public byte Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Furlongs { get; set; }

    public TrackId TrackId { get; set; }

    public virtual Track Track { get; set; } = null!;

    public SurfaceId SurfaceId { get; set; }

    public virtual Surface Surface { get; set; } = null!;
}
