using Microsoft.Extensions.Logging;
using Moq;
using TripleDerby.Core.Abstractions.Generators;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Services;
using TripleDerby.Core.Specifications;
using TripleDerby.Services.Breeding;
using TripleDerby.SharedKernel;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Tests.Unit.Breeding;

public class BreedingExecutorColorCacheTests
{
    [Fact]
    public async Task Breed_UsesColorCache_NotRepository()
    {
        // Arrange
        var sireId = Guid.NewGuid();
        var damId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var sire = new Horse
        {
            Id = sireId,
            Name = "Test Sire",
            IsMale = true,
            ColorId = 1,
            Color = new Color { Id = 1, Name = "Bay", Weight = 10, IsSpecial = false },
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Speed, Actual = 50, DominantPotential = 80, RecessivePotential = 60 },
                new() { StatisticId = StatisticId.Stamina, Actual = 50, DominantPotential = 75, RecessivePotential = 65 },
                new() { StatisticId = StatisticId.Agility, Actual = 50, DominantPotential = 70, RecessivePotential = 60 },
                new() { StatisticId = StatisticId.Durability, Actual = 50, DominantPotential = 72, RecessivePotential = 62 }
            }
        };

        var dam = new Horse
        {
            Id = damId,
            Name = "Test Dam",
            IsMale = false,
            ColorId = 2,
            Color = new Color { Id = 2, Name = "Chestnut", Weight = 10, IsSpecial = false },
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Speed, Actual = 50, DominantPotential = 75, RecessivePotential = 65 },
                new() { StatisticId = StatisticId.Stamina, Actual = 50, DominantPotential = 80, RecessivePotential = 70 },
                new() { StatisticId = StatisticId.Agility, Actual = 50, DominantPotential = 72, RecessivePotential = 62 },
                new() { StatisticId = StatisticId.Durability, Actual = 50, DominantPotential = 74, RecessivePotential = 64 }
            }
        };

        var colors = new List<Color>
        {
            new() { Id = 1, Name = "Bay", Weight = 10, IsSpecial = false },
            new() { Id = 2, Name = "Chestnut", Weight = 10, IsSpecial = false },
            new() { Id = 3, Name = "Black", Weight = 15, IsSpecial = false }
        };

        var randomGeneratorMock = new Mock<IRandomGenerator>();
        randomGeneratorMock.Setup(r => r.Next(0, 2)).Returns(1); // Gender
        randomGeneratorMock.Setup(r => r.Next(0, It.IsAny<int>())).Returns(0); // Color selection
        randomGeneratorMock.Setup(r => r.Next(1, 5)).Returns(1); // Punnett quadrant
        randomGeneratorMock.Setup(r => r.Next(1, 3)).Returns(1); // Gene selection
        randomGeneratorMock.Setup(r => r.Next(1, 101)).Returns(50); // Mutation
        randomGeneratorMock.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(0);

        var repositoryMock = new Mock<ITripleDerbyRepository>();
        repositoryMock
            .Setup(r => r.SingleOrDefaultAsync(It.IsAny<ParentHorseSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParentHorseSpecification spec, CancellationToken ct) =>
            {
                // Return appropriate parent based on specification
                return spec.ToString().Contains(sireId.ToString()) ? sire : dam;
            });

        repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Horse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Horse h, CancellationToken ct) =>
            {
                h.Id = Guid.NewGuid();
                return h;
            });

        var colorCacheMock = new Mock<ColorCache>(Mock.Of<ILogger<ColorCache>>());
        colorCacheMock
            .Setup(c => c.GetColorsAsync(It.IsAny<ITripleDerbyRepository>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(colors);

        var horseNameGeneratorMock = new Mock<IHorseNameGenerator>();
        horseNameGeneratorMock.Setup(h => h.Generate()).Returns("Test Foal");

        var timeManagerMock = new Mock<ITimeManager>();
        timeManagerMock.Setup(t => t.OffsetUtcNow()).Returns(DateTimeOffset.UtcNow);

        var loggerMock = new Mock<ILogger<BreedingExecutor>>();

        var executor = new BreedingExecutor(
            randomGeneratorMock.Object,
            repositoryMock.Object,
            horseNameGeneratorMock.Object,
            timeManagerMock.Object,
            loggerMock.Object,
            colorCacheMock.Object);

        // Act
        var result = await executor.Breed(sireId, damId, ownerId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.FoalId);

        // Verify ColorCache was called
        colorCacheMock.Verify(
            c => c.GetColorsAsync(repositoryMock.Object, It.IsAny<CancellationToken>()),
            Times.Once,
            "ColorCache.GetColorsAsync should be called once");

        // Verify Repository.GetAllAsync<Color> was NOT called
        repositoryMock.Verify(
            r => r.GetAllAsync<Color>(It.IsAny<CancellationToken>()),
            Times.Never,
            "Repository.GetAllAsync<Color> should NOT be called when ColorCache is used");
    }
}
