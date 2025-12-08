namespace TripleDerby.SharedKernel;

public record TrainingsResult
{
    public byte Id { get; init; }

    public string Name { get; init; } = default!;

    public string Description { get; init; } = default!;
}
