using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Breeding;

namespace TripleDerby.Tests.Unit.Breeding;

public class GetRandomColorTests
{
    private static BreedingRequestProcessor CreateProcessor(
        Mock<IRandomGenerator> rnd,
        Mock<ITripleDerbyRepository> repo)
    {
        var logger = new Mock<ILogger<BreedingRequestProcessor>>();

        var msgPub = Mock.Of<Core.Abstractions.Messaging.IMessagePublisher>();
        var horseNameGen = Mock.Of<Core.Abstractions.Generators.IHorseNameGenerator>();
        var timeManager = Mock.Of<ITimeManager>();

        return new BreedingRequestProcessor(
            logger.Object,
            rnd.Object,
            repo.Object,
            msgPub,
            horseNameGen,
            timeManager);
    }

    private static MethodInfo GetGetRandomColorMethod()
    {
        var mi = typeof(BreedingRequestProcessor).GetMethod(
            "GetRandomColor",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(bool), typeof(bool), typeof(bool), typeof(CancellationToken)],
            null);

        if (mi == null) throw new InvalidOperationException("GetRandomColor method not found via reflection.");
        return mi;
    }

    [Fact]
    public async Task GetRandomColor_ExcludeSpecialColors_OnlyNonSpecialCandidatesUsed()
    {
        // Arrange
        var rnd = new Mock<IRandomGenerator>();
        rnd.Setup(r => r.Next(It.IsAny<int>(), It.IsAny<int>())).Returns(0); // pick first

        var colors = new List<Color>
        {
            new() { Id = 1, Name = "CommonA", Weight = 1, IsSpecial = false },
            new() { Id = 2, Name = "SpecialX", Weight = 1, IsSpecial = true },
            new() { Id = 3, Name = "CommonB", Weight = 1, IsSpecial = false }
        };

        var repo = new Mock<ITripleDerbyRepository>();
        repo.Setup(r => r.GetAllAsync<Color>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(colors);

        var processor = CreateProcessor(rnd, repo);
        var mi = GetGetRandomColorMethod();

        // Act
        var task = (Task<Color>)mi.Invoke(processor, [false, false, false, CancellationToken.None])!;
        var selected = await task;

        // Assert - special color should not be chosen; because rnd returns 0 we expect first non-special candidate (CommonA)
        Assert.NotNull(selected);
        Assert.False(selected.IsSpecial);
        Assert.Equal(1, selected.Id);
    }

    [Fact]
    public async Task GetRandomColor_N_ShouldSelectFirstWhenZero()
    {
        // Arrange
        var rnd = new Mock<IRandomGenerator>();
        // Called as Next(0, int.MaxValue)
        rnd.Setup(r => r.Next(0, int.MaxValue)).Returns(0);

        var colors = new List<Color>
        {
            new() { Id = 10, Name = "C1", Weight = 1, IsSpecial = false },
            new() { Id = 11, Name = "C2", Weight = 1, IsSpecial = false },
            new() { Id = 12, Name = "C3", Weight = 1, IsSpecial = false }
        };

        var repo = new Mock<ITripleDerbyRepository>();
        repo.Setup(r => r.GetAllAsync<Color>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(colors);

        var processor = CreateProcessor(rnd, repo);
        var mi = GetGetRandomColorMethod();

        // Act
        var task = (Task<Color>)mi.Invoke(processor, [false, false, true, CancellationToken.None])!;
        var selected = await task;

        // Assert - with n == 0 we should pick the first candidate
        Assert.Equal(10, selected.Id);
    }

    [Fact]
    public async Task GetRandomColor_N_ShouldSelectLastWhenMax()
    {
        // Arrange
        var rnd = new Mock<IRandomGenerator>();
        // simulate largest possible n (int.MaxValue - 1)
        rnd.Setup(r => r.Next(0, int.MaxValue)).Returns(int.MaxValue - 1);

        var colors = new List<Color>
        {
            new() { Id = 20, Name = "A", Weight = 1, IsSpecial = false },
            new() { Id = 21, Name = "B", Weight = 1, IsSpecial = false },
            new() { Id = 22, Name = "C", Weight = 1, IsSpecial = false }
        };

        var repo = new Mock<ITripleDerbyRepository>();
        repo.Setup(r => r.GetAllAsync<Color>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(colors);

        var processor = CreateProcessor(rnd, repo);
        var mi = GetGetRandomColorMethod();

        // Act
        var task = (Task<Color>)mi.Invoke(processor, [false, false, true, CancellationToken.None])!;
        var selected = await task;

        // Assert - when r is effectively totalWeight we expect the fallback to return the last candidate
        Assert.Equal(22, selected.Id);
    }

    [Fact]
    public async Task GetRandomColor_NoCandidates_ThrowsInvalidOperationException()
    {
        // Arrange
        var rnd = new Mock<IRandomGenerator>();

        var colors = new List<Color>
        {
            new() { Id = 30, Name = "SpecialOnly", Weight = 1, IsSpecial = true }
        };

        var repo = new Mock<ITripleDerbyRepository>();
        repo.Setup(r => r.GetAllAsync<Color>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(colors);

        var processor = CreateProcessor(rnd, repo);
        var mi = GetGetRandomColorMethod();

        // Act & Assert - when includeSpecialColors == false and all colors are special we should get InvalidOperationException
        var exTask = (Task)mi.Invoke(processor, [false, false, false, CancellationToken.None])!;
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await exTask);
    }
}