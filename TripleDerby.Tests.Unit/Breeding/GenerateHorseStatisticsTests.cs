using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using TripleDerby.Core.Abstractions.Generators;
using TripleDerby.Core.Abstractions.Messaging;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Breeding;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Breeding;

public class GenerateHorseStatisticsTests
{
    private static BreedingRequestProcessor CreateProcessor(Mock<IRandomGenerator> rnd)
    {
        Mock<ILogger<BreedingRequestProcessor>> logger = new();
        ITripleDerbyRepository repo = Mock.Of<ITripleDerbyRepository>();
        IMessagePublisher pub = Mock.Of<IMessagePublisher>();
        IHorseNameGenerator nameGen = Mock.Of<IHorseNameGenerator>();
        ITimeManager timeMgr = Mock.Of<ITimeManager>();

        return new BreedingRequestProcessor(
            logger.Object,
            rnd.Object,
            repo,
            pub,
            nameGen,
            timeMgr);
    }

    private static MethodInfo GetGenerateHorseStatisticsMethod()
    {
        MethodInfo? mi = typeof(BreedingRequestProcessor).GetMethod(
            "GenerateHorseStatistics",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(ICollection<HorseStatistic>), typeof(ICollection<HorseStatistic>)],
            null);

        if (mi == null)
        {
            throw new InvalidOperationException("GenerateHorseStatistics method not found via reflection.");
        }

        return mi;
    }

    [Fact]
    public void GenerateHorseStatistics_NullSire_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<IRandomGenerator> rnd = new();
        rnd.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns((int min, int _) => min);

        BreedingRequestProcessor processor = CreateProcessor(rnd);
        MethodInfo mi = GetGenerateHorseStatisticsMethod();

        ICollection<HorseStatistic> damStats = new List<HorseStatistic>
        {
            new() { StatisticId = StatisticId.Speed, DominantPotential = 80, RecessivePotential = 10 },
            new() { StatisticId = StatisticId.Stamina, DominantPotential = 80, RecessivePotential = 10 },
            new() { StatisticId = StatisticId.Agility, DominantPotential = 80, RecessivePotential = 10 },
            new() { StatisticId = StatisticId.Durability, DominantPotential = 80, RecessivePotential = 10 }
        };

        // Act & Assert - reflection invocation wraps exceptions in TargetInvocationException
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => mi.Invoke(processor, [null!, damStats
        ]));
        Assert.IsType<ArgumentNullException>(tie.InnerException);
    }

    [Fact]
    public void GenerateHorseStatistics_NullDam_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<IRandomGenerator> rnd = new();
        rnd.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns((int min, int _) => min);

        BreedingRequestProcessor processor = CreateProcessor(rnd);
        MethodInfo mi = GetGenerateHorseStatisticsMethod();

        ICollection<HorseStatistic> sireStats = new List<HorseStatistic>
        {
            new() { StatisticId = StatisticId.Speed, DominantPotential = 80, RecessivePotential = 10 },
            new() { StatisticId = StatisticId.Stamina, DominantPotential = 80, RecessivePotential = 10 },
            new() { StatisticId = StatisticId.Agility, DominantPotential = 80, RecessivePotential = 10 },
            new() { StatisticId = StatisticId.Durability, DominantPotential = 80, RecessivePotential = 10 }
        };

        // Act & Assert - reflection invocation wraps exceptions in TargetInvocationException
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => mi.Invoke(processor, [sireStats, null!
        ]));
        Assert.IsType<ArgumentNullException>(tie.InnerException);
    }

    [Fact]
    public void GenerateHorseStatistics_MissingStatisticInSire_ThrowsInvalidOperationException()
    {
        // Arrange
        Mock<IRandomGenerator> rnd = new();
        rnd.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns((int min, int _) => min);

        BreedingRequestProcessor processor = CreateProcessor(rnd);
        MethodInfo mi = GetGenerateHorseStatisticsMethod();

        // Sire missing Durability
        ICollection<HorseStatistic> sireStats = new List<HorseStatistic>
        {
            new() { StatisticId = StatisticId.Speed, DominantPotential = 80, RecessivePotential = 10 },
            new() { StatisticId = StatisticId.Stamina, DominantPotential = 80, RecessivePotential = 10 },
            new() { StatisticId = StatisticId.Agility, DominantPotential = 80, RecessivePotential = 10 }
        };

        ICollection<HorseStatistic> damStats = new List<HorseStatistic>
        {
            new() { StatisticId = StatisticId.Speed, DominantPotential = 70, RecessivePotential = 9 },
            new() { StatisticId = StatisticId.Stamina, DominantPotential = 70, RecessivePotential = 9 },
            new() { StatisticId = StatisticId.Agility, DominantPotential = 70, RecessivePotential = 9 },
            new() { StatisticId = StatisticId.Durability, DominantPotential = 70, RecessivePotential = 9 }
        };

        // Act & Assert
        TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => mi.Invoke(processor, [sireStats, damStats
        ]));
        Assert.IsType<InvalidOperationException>(tie.InnerException);
        Assert.Contains("Sire is missing statistic", tie.InnerException!.Message);
    }

    [Fact]
    public void GenerateHorseStatistics_ReturnsHappinessPlusAllRequiredStats_WithDeterministicValues()
    {
        // Arrange
        Mock<IRandomGenerator> rnd = new();
        // Deterministic strategy: always return the provided min for Next(min,max) so:
        // - punnettQuadrant = 1, whichGeneToPick = 1
        // - mutationMultiplier = 1 -> case 1 (safe non-negative mutation bounds)
        // - mutation delta = 0 (no mutation)
        // - actual = min (which equals Max(1, dominant/3))
        rnd.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int min, int _) => min);

        BreedingRequestProcessor processor = CreateProcessor(rnd);
        MethodInfo mi = GetGenerateHorseStatisticsMethod();

        ICollection<HorseStatistic> sireStats = new List<HorseStatistic>
        {
            new() { StatisticId = StatisticId.Speed, DominantPotential = 90, RecessivePotential = 10 },
            new() { StatisticId = StatisticId.Stamina, DominantPotential = 75, RecessivePotential = 12 },
            new() { StatisticId = StatisticId.Agility, DominantPotential = 60, RecessivePotential = 8 },
            new() { StatisticId = StatisticId.Durability, DominantPotential = 45, RecessivePotential = 6 }
        };

        ICollection<HorseStatistic> damStats = new List<HorseStatistic>
        {
            new() { StatisticId = StatisticId.Speed, DominantPotential = 85, RecessivePotential = 11 },
            new() { StatisticId = StatisticId.Stamina, DominantPotential = 70, RecessivePotential = 13 },
            new() { StatisticId = StatisticId.Agility, DominantPotential = 55, RecessivePotential = 9 },
            new() { StatisticId = StatisticId.Durability, DominantPotential = 40, RecessivePotential = 7 }
        };

        // Act
        object? resultObj = mi.Invoke(processor, [sireStats, damStats]);
        List<HorseStatistic> result = Assert.IsType<List<HorseStatistic>>(resultObj);

        // Assert - happiness + 4 stats
        Assert.Equal(5, result.Count);
        Assert.Equal(StatisticId.Happiness, result[0].StatisticId);
        Assert.Equal((byte)100, result[0].DominantPotential);

        // Required stats present in order: Speed, Stamina, Agility, Durability
        StatisticId[] expectedOrder = [StatisticId.Speed, StatisticId.Stamina, StatisticId.Agility, StatisticId.Durability
        ];
        for (int i = 0; i < expectedOrder.Length; i++)
        {
            HorseStatistic hs = result[i + 1];
            Assert.Equal(expectedOrder[i], hs.StatisticId);

            // Because our mock returns 'min' for actual selection,
            // expectedActual = Math.Max(1, dominantPotential / 3)
            byte expectedDominant = hs.DominantPotential;
            int expectedMin = Math.Max(1, expectedDominant / 3);
            Assert.Equal((byte)expectedMin, hs.Actual);
        }
    }
}