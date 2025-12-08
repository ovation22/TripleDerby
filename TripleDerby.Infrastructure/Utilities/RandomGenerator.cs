using System.Security.Cryptography;
using TripleDerby.Core.Abstractions.Utilities;

namespace TripleDerby.Infrastructure.Utilities;

public class RandomGenerator : IRandomGenerator
{
    public int Next()
    {
        return RandomNumberGenerator.GetInt32(int.MaxValue);
    }

    public int Next(int max)
    {
        if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max), "max must be > 0");
        return RandomNumberGenerator.GetInt32(max);
    }

    public int Next(int min, int max)
    {
        if (max <= min) throw new ArgumentOutOfRangeException(nameof(max), "max must be > min");
        return RandomNumberGenerator.GetInt32(min, max);
    }
}
