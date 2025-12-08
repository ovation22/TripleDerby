using System;
using System.Collections.Generic;
using System.Linq;
using TripleDerby.Core.Entities;
using TripleDerby.Infrastructure.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace TripleDerby.Tests.Unit;

public class BreedingDistributionSimulationTests
{
    private readonly ITestOutputHelper _output;

    public BreedingDistributionSimulationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // Long-running harness — skipped by default so CI isn't impacted.
    // Run locally to simulate distribution (1_000_000 iterations).
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void SimulateOneMillionBreeds_PrintDistribution(bool damSpecial, bool sireSpecial)
    {
        const int iterations = 1_000_000;
        var rng = new RandomGenerator();

        // Use the same Color.Weight semantics as your seed (Weight = 1 means common).
        var colors = new List<Color>
        {
            new Color { Id = 1,  Weight = 1,    IsSpecial = false, Name = "Gray" },
            new Color { Id = 2,  Weight = 1,    IsSpecial = false, Name = "Bay" },
            new Color { Id = 3,  Weight = 1,    IsSpecial = false, Name = "Seal Brown" },
            new Color { Id = 4,  Weight = 2,    IsSpecial = false, Name = "Chestnut" },
            new Color { Id = 5,  Weight = 3,    IsSpecial = false, Name = "Black" },
            new Color { Id = 6,  Weight = 4,    IsSpecial = false, Name = "Dapple Gray" },
            new Color { Id = 7,  Weight = 5,    IsSpecial = false, Name = "Roan" },
            new Color { Id = 8,  Weight = 6,    IsSpecial = false, Name = "Liver Chestnut" },
            new Color { Id = 9,  Weight = 7,    IsSpecial = false, Name = "Buckskin" },
            new Color { Id = 10, Weight = 8,    IsSpecial = false, Name = "Cremello" },
            new Color { Id = 11, Weight = 9,    IsSpecial = false, Name = "Grullo" },
            new Color { Id = 12, Weight = 10,   IsSpecial = false, Name = "Champagne" },
            new Color { Id = 13, Weight = 12,   IsSpecial = false, Name = "Palomino" },
            new Color { Id = 14, Weight = 50,   IsSpecial = false, Name = "White" },
            new Color { Id = 15, Weight = 100,  IsSpecial = false, Name = "Platinum" },
            new Color { Id = 16, Weight = 500,   IsSpecial = true, Name = "Pinto" },
            new Color { Id = 17, Weight = 1_000, IsSpecial = true, Name = "Appaloosa" },
            new Color { Id = 18, Weight = 5_000, IsSpecial = true, Name = "Holstein" },
            new Color { Id = 19, Weight = 10_000, IsSpecial = true, Name = "Przewalski" },
            new Color { Id = 20, Weight = 15_000, IsSpecial = true, Name = "Zebra" },
            new Color { Id = 21, Weight = 20_000, IsSpecial = true, Name = "Okapi" },
            new Color { Id = 22, Weight = 25_000, IsSpecial = true, Name = "Bengal" },
            new Color { Id = 23, Weight = 30_000, IsSpecial = true, Name = "Panda" },
            new Color { Id = 24, Weight = 100_000, IsSpecial = true, Name = "Unicorn" },
            new Color { Id = 25, Weight = 200_000, IsSpecial = true, Name = "Pegasus" },
        };

        // include special colors in the candidate set for this simulation
        bool includeSpecialColors = true;

        var candidates = colors.Where(c => includeSpecialColors || !c.IsSpecial).ToList();

        // Build inverted (frequency) weights exactly as BreedingService.GetRandomColor does,
        // including special multipliers when parents are special.
        double singleParentSpecialMultiplier = 10.0;
        double bothParentsSpecialMultiplier = 50.0;
        double specialMultiplier = 1.0;
        if (sireSpecial && damSpecial) specialMultiplier = bothParentsSpecialMultiplier;
        else if (sireSpecial || damSpecial) specialMultiplier = singleParentSpecialMultiplier;

        var weightList = new List<double>(candidates.Count);
        double totalWeight = 0.0;
        foreach (var c in candidates)
        {
            var denom = Math.Max(1.0, (double)c.Weight);
            double frequency = 1.0 / denom;

            if (c.IsSpecial)
                frequency *= specialMultiplier;

            weightList.Add(frequency);
            totalWeight += frequency;
        }

        var counts = candidates.ToDictionary(c => c.Id, c => 0L);

        for (int i = 0; i < iterations; i++)
        {
            // mirror the service's random draw
            int n = rng.Next(0, int.MaxValue);
            double r = (n / (double)int.MaxValue) * totalWeight;

            double acc = 0.0;
            for (int j = 0; j < candidates.Count; j++)
            {
                acc += weightList[j];
                if (r < acc)
                {
                    counts[candidates[j].Id]++;
                    break;
                }
            }
        }

        // Print results
        var ordered = counts
            .Select(kvp =>
            {
                var color = candidates.Single(c => c.Id == kvp.Key);
                return new
                {
                    colorId = kvp.Key,
                    colorName = color.Name,
                    count = kvp.Value,
                    percentage = kvp.Value * 100.0 / iterations
                };
            })
            .OrderByDescending(x => x.count)
            .ToList();

        _output.WriteLine("Simulation iterations: " + iterations);
        _output.WriteLine($"Parents special flags: Dam={damSpecial}, Sire={sireSpecial}, includeSpecialColors={includeSpecialColors}");
        foreach (var item in ordered)
        {
            _output.WriteLine($"{item.colorId} - {item.colorName} => {item.count} ({item.percentage:F6} %)");
        }
    }
}