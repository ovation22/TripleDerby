namespace TripleDerby.Core.Abstractions.Utilities;

public interface ITimeManager
{
    DateTime Now();
    DateTimeOffset OffsetNow();
    DateTime UtcNow();
    DateTimeOffset OffsetUtcNow();
}
