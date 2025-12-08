using TripleDerby.Infrastructure.Utilities;

namespace TripleDerby.Tests.Unit;

public class RandomGeneratorTests
{
    [Fact]
    public void Next_Returns_NonNegative_LessThanIntMax()
    {
        var rng = new RandomGenerator();
        for (int i = 0; i < 1000; i++)
        {
            var v = rng.Next();
            Assert.InRange(v, 0, int.MaxValue - 1);
        }
    }

    [Fact]
    public void Next_WithMax_Returns_InRange_And_Throws_On_Invalid()
    {
        var rng = new RandomGenerator();

        for (int i = 0; i < 1000; i++)
        {
            var v = rng.Next(10);
            Assert.InRange(v, 0, 9);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(-5));
    }

    [Fact]
    public void Next_WithMinMax_Returns_InRange_And_Throws_On_Invalid()
    {
        var rng = new RandomGenerator();

        for (int i = 0; i < 1000; i++)
        {
            var v = rng.Next(5, 10);
            Assert.InRange(v, 5, 9);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.Next(10, 5));
    }

    [Fact]
    public void Next_Max_Distribution_Is_Reasonably_Uniform()
    {
        var rng = new RandomGenerator();
        const int buckets = 3;
        const int samples = 30000;

        var counts = new int[buckets];
        for (int i = 0; i < samples; i++)
        {
            counts[rng.Next(buckets)]++;
        }

        double expected = samples / (double)buckets;
        double tolerance = expected * 0.15; // 15% tolerance to avoid flakiness in CI

        for (int j = 0; j < buckets; j++)
        {
            Assert.InRange(counts[j], expected - tolerance, expected + tolerance);
        }
    }

    [Fact]
    public void Next_Is_ThreadSafe_Under_Parallel_Load()
    {
        var rng = new RandomGenerator();
        const int tasks = 8;
        const int perTask = 10000;
        var results = new int[tasks * perTask];

        Parallel.For(0, tasks, t =>
        {
            for (int i = 0; i < perTask; i++)
            {
                results[t * perTask + i] = rng.Next(100);
            }
        });

        Assert.Equal(tasks * perTask, results.Length);
        Assert.All(results, r => Assert.InRange(r, 0, 99));
    }
}