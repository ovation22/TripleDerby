using System;

namespace TripleDerby.SharedKernel;

public record ParentHorse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Color { get; init; } = null!;

    public short RaceStarts { get; init; }

    public short RaceWins { get; init; }

    public short RacePlace { get; init; }

    public short RaceShow { get; init; }

    public int Earnings { get; init; }
}
