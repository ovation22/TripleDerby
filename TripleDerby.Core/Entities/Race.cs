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

    public RaceClassId RaceClassId { get; set; }

    public virtual RaceClass RaceClass { get; set; } = null!;

    public byte MinFieldSize { get; set; } = 8;

    public byte MaxFieldSize { get; set; } = 12;

    public int Purse { get; set; }
}
