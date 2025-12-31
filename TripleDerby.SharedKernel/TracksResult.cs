
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

public record TracksResult
{
    public TrackId Id { get; init; }

    public string Name { get; init; } = null!;
}
