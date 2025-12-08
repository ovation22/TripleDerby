using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

public record FeedingSessionResult
{
    public FeedResponse Result { get; init; }
}
