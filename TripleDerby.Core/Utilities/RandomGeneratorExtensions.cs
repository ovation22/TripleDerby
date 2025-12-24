using TripleDerby.Core.Abstractions.Utilities;

namespace TripleDerby.Core.Utilities;

/// <summary>
/// Extension methods for IRandomGenerator to support common patterns.
/// </summary>
public static class RandomGeneratorExtensions
{
    /// <summary>
    /// Picks a random element from an array.
    /// </summary>
    /// <typeparam name="T">Type of elements in the array</typeparam>
    /// <param name="random">Random generator instance</param>
    /// <param name="array">Array to pick from</param>
    /// <returns>Random element from the array</returns>
    /// <exception cref="ArgumentException">Thrown if array is null or empty</exception>
    public static T PickRandom<T>(this IRandomGenerator random, T[] array)
    {
        if (array == null || array.Length == 0)
            throw new ArgumentException("Array cannot be null or empty", nameof(array));

        var index = random.Next(array.Length);
        return array[index];
    }
}
