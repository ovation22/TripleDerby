using System;
using System.Collections.Generic;

namespace TripleDerby.SharedKernel;

public record RaceRunResult
{
    public Guid Id { get; init; }

    public byte RaceId { get; init; }

    public string WinHorse { get; init; } = default!;

    public string PlaceHorse { get; init; } = default!;

    public string ShowHorse { get; init; } = default!;

    public List<string> PlayByPlay { get; init; } = default!;
}
