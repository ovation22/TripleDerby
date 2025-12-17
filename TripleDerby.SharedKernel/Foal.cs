using System;

namespace TripleDerby.SharedKernel;

public record Foal
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public byte ColorId { get; init; }

    public string Color { get; init; } = null!;
}
