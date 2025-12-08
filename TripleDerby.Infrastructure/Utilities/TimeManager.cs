using TripleDerby.Core.Abstractions.Utilities;

namespace TripleDerby.Infrastructure.Utilities;

public class TimeManager : ITimeManager
{
    public DateTime Now() => DateTime.Now;

    public DateTimeOffset OffsetNow() => DateTimeOffset.Now;

    public DateTime UtcNow() => DateTime.UtcNow;

    public DateTimeOffset OffsetUtcNow() => DateTimeOffset.UtcNow;
}