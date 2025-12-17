namespace TripleDerby.SharedKernel;

public record TrainingsResult
{
    public byte Id { get; init; }

    public string Name { get; init; } = null!;

    public string Description { get; init; } = null!;
}
