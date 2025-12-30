
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

public record RacesResult
{
    public byte Id { get; init; }

    public string Name { get; init; } = null!;

    public string Description { get; init; } = null!;

    public decimal Furlongs { get; init; }

    public SurfaceId SurfaceId { get; init; }

    public string Surface { get; init; } = null!;

    public TrackId TrackId { get; init; }

    public string Track { get; init; } = null!;

    public RaceClassId RaceClassId { get; init; }

    public string RaceClass { get; init; } = null!;

    public byte MinFieldSize { get; init; }

    public byte MaxFieldSize { get; init; }

    public int Purse { get; set; }
}
