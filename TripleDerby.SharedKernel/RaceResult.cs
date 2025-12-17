
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

public record RaceResult
{
    public byte Id { get; init; }

    public string Name { get; init; } = null!;

    public string Description { get; init; } = null!;

    public decimal Furlongs { get; init; }

    public SurfaceId SurfaceId { get; init; }

    public string Surface { get; init; } = null!;

    public TrackId TrackId { get; init; }

    public string Track { get; init; } = null!;
}
