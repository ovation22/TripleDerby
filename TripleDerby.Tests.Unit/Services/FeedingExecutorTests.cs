using Moq;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.Services.Feeding;
using TripleDerby.Services.Feeding.Abstractions;
using TripleDerby.Services.Feeding.DTOs;
using TripleDerby.SharedKernel.Enums;
using FeedingEntity = TripleDerby.Core.Entities.Feeding;

namespace TripleDerby.Tests.Unit.Services;

public class FeedingExecutorTests
{
    private readonly Mock<IFeedingCalculator> _calculatorMock;
    private readonly Mock<ITripleDerbyRepository> _repositoryMock;
    private readonly FeedingExecutor _executor;

    public FeedingExecutorTests()
    {
        _calculatorMock = new Mock<IFeedingCalculator>();
        _repositoryMock = new Mock<ITripleDerbyRepository>();
        _executor = new FeedingExecutor(_calculatorMock.Object, _repositoryMock.Object);
    }

    [Fact]
    [Trait("Category", "ErrorHandling")]
    public async Task ExecuteFeedingAsync_HorseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        byte feedingId = 1;

        _repositoryMock.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<HorseWithStatsSpecification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Horse?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _executor.ExecuteFeedingAsync(horseId, feedingId));
    }

    [Fact]
    [Trait("Category", "ErrorHandling")]
    public async Task ExecuteFeedingAsync_FeedingNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var horse = CreateHorse();
        byte feedingId = 99;

        SetupHorseRepository(horse);
        _repositoryMock.Setup(r => r.FindAsync<FeedingEntity>(feedingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedingEntity?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _executor.ExecuteFeedingAsync(horse.Id, feedingId));
    }

    [Fact]
    [Trait("Category", "ErrorHandling")]
    public async Task ExecuteFeedingAsync_HorseAlreadyFed_ThrowsInvalidOperationException()
    {
        // Arrange
        var horse = CreateHorse();
        horse.HasFedSinceLastRace = true;
        byte feedingId = 1;

        SetupHorseRepository(horse);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _executor.ExecuteFeedingAsync(horse.Id, feedingId));
    }

    [Fact]
    [Trait("Category", "PreferenceHandling")]
    public async Task ExecuteFeedingAsync_RejectedFeeding_ReturnsRejectedResult()
    {
        // Arrange
        var horse = CreateHorse();
        var feeding = CreateFeeding();

        SetupHorseRepository(horse);
        SetupFeedingRepository(feeding);
        SetupPreferenceCalculation(FeedResponse.Rejected, discovered: true);

        _repositoryMock.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<HorseFeedingPreferenceSpecification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HorseFeedingPreference?)null);

        _repositoryMock.Setup(r => r.CreateAsync(
                It.IsAny<HorseFeedingPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<HorseFeedingPreference>());

        _repositoryMock.Setup(r => r.UpdateAsync(
                It.IsAny<Horse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(horse);

        // Act
        var result = await _executor.ExecuteFeedingAsync(horse.Id, feeding.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(FeedResponse.Rejected, result.Response);
        Assert.Equal(0, result.SpeedGain);
        Assert.Equal(0, result.StaminaGain);
        Assert.Equal(0, result.AgilityGain);
        Assert.Equal(0, result.DurabilityGain);
        Assert.Equal(0, result.HappinessGain);
        Assert.False(result.UpsetStomachOccurred);
        Assert.True(result.PreferenceDiscovered);
        Assert.Contains("refused to eat", result.Message);

        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<Horse>(h => h.HasFedSinceLastRace),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "PreferenceHandling")]
    public async Task ExecuteFeedingAsync_FavoriteFeeding_AppliesPositiveGains()
    {
        // Arrange
        var horse = CreateHorse();
        var feeding = CreateFeeding();

        SetupHorseRepository(horse);
        SetupFeedingRepository(feeding);
        SetupPreferenceCalculation(FeedResponse.Favorite, discovered: false);
        SetupCalculatorForPositiveGains();

        var existingPreference = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = horse.Id,
            FeedingId = feeding.Id,
            Preference = FeedResponse.Favorite
        };

        _repositoryMock.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<HorseFeedingPreferenceSpecification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPreference);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Horse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(horse);

        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<FeedingSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedingSession fs, CancellationToken ct) => fs);

        // Act
        var result = await _executor.ExecuteFeedingAsync(horse.Id, feeding.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(FeedResponse.Favorite, result.Response);
        Assert.True(result.SpeedGain > 0);
        Assert.True(result.StaminaGain > 0);
        Assert.True(result.AgilityGain > 0);
        Assert.True(result.DurabilityGain > 0);
        Assert.True(result.HappinessGain > 0);
        Assert.False(result.PreferenceDiscovered); // Already known
        Assert.False(result.UpsetStomachOccurred);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Horse>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<FeedingSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "PreferenceHandling")]
    public async Task ExecuteFeedingAsync_HatedFeedingWithUpsetStomach_AppliesNegativeHappiness()
    {
        // Arrange
        var horse = CreateHorse();
        var feeding = CreateFeeding();

        SetupHorseRepository(horse);
        SetupFeedingRepository(feeding);
        SetupPreferenceCalculation(FeedResponse.Hated, discovered: true);

        _calculatorMock.Setup(c => c.CalculateHappinessGain(
                It.IsAny<double>(), It.IsAny<double>(), FeedResponse.Hated, It.IsAny<double>()))
            .Returns(-5.0);

        _calculatorMock.Setup(c => c.RollUpsetStomach(It.IsAny<Guid>(), It.IsAny<byte>(), It.IsAny<DateTime>()))
            .Returns(true); // Upset stomach occurs

        _calculatorMock.Setup(c => c.CalculateStatGain(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
                FeedResponse.Hated, It.IsAny<double>()))
            .Returns(0.5); // Small gains

        SetupModifierCalculations();

        _repositoryMock.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<HorseFeedingPreferenceSpecification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HorseFeedingPreference?)null);

        _repositoryMock.Setup(r => r.CreateAsync(
                It.IsAny<HorseFeedingPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<HorseFeedingPreference>());

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Horse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(horse);

        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<FeedingSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedingSession fs, CancellationToken ct) => fs);

        // Act
        var result = await _executor.ExecuteFeedingAsync(horse.Id, feeding.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(FeedResponse.Hated, result.Response);
        Assert.True(result.UpsetStomachOccurred);
        Assert.True(result.HappinessGain < -5.0); // Should be worse than -5 due to upset stomach penalty
        Assert.True(result.PreferenceDiscovered);
        Assert.Contains("upset stomach", result.Message);

        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<FeedingSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "PreferenceHandling")]
    public async Task ExecuteFeedingAsync_NeutralFeeding_AppliesModerateGains()
    {
        // Arrange
        var horse = CreateHorse();
        var feeding = CreateFeeding();

        SetupHorseRepository(horse);
        SetupFeedingRepository(feeding);
        SetupPreferenceCalculation(FeedResponse.Neutral, discovered: false);

        _calculatorMock.Setup(c => c.CalculateHappinessGain(
                It.IsAny<double>(), It.IsAny<double>(), FeedResponse.Neutral, It.IsAny<double>()))
            .Returns(3.0);

        _calculatorMock.Setup(c => c.CalculateStatGain(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
                FeedResponse.Neutral, It.IsAny<double>()))
            .Returns(2.0); // Moderate gains

        SetupModifierCalculations();

        var existingPreference = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = horse.Id,
            FeedingId = feeding.Id,
            Preference = FeedResponse.Neutral
        };

        _repositoryMock.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<HorseFeedingPreferenceSpecification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPreference);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Horse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(horse);

        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<FeedingSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedingSession fs, CancellationToken ct) => fs);

        // Act
        var result = await _executor.ExecuteFeedingAsync(horse.Id, feeding.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(FeedResponse.Neutral, result.Response);
        Assert.Equal(2.0, result.SpeedGain);
        Assert.Equal(2.0, result.StaminaGain);
        Assert.Equal(2.0, result.AgilityGain);
        Assert.Equal(2.0, result.DurabilityGain);
        Assert.Equal(3.0, result.HappinessGain);
        Assert.False(result.UpsetStomachOccurred);
        Assert.False(result.PreferenceDiscovered); // Already known
    }

    [Fact]
    [Trait("Category", "PreferenceHandling")]
    public async Task ExecuteFeedingAsync_NewPreference_SavesPreferenceAndMarksDiscovered()
    {
        // Arrange
        var horse = CreateHorse();
        var feeding = CreateFeeding();

        SetupHorseRepository(horse);
        SetupFeedingRepository(feeding);
        SetupPreferenceCalculation(FeedResponse.Favorite, discovered: true);
        SetupCalculatorForPositiveGains();

        HorseFeedingPreference? capturedPreference = null;

        _repositoryMock.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<HorseFeedingPreferenceSpecification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HorseFeedingPreference?)null); // No existing preference

        _repositoryMock.Setup(r => r.CreateAsync(
                It.IsAny<HorseFeedingPreference>(),
                It.IsAny<CancellationToken>()))
            .Callback<HorseFeedingPreference, CancellationToken>((pref, ct) => capturedPreference = pref)
            .ReturnsAsync((HorseFeedingPreference pref, CancellationToken ct) => pref);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Horse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(horse);

        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<FeedingSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedingSession fs, CancellationToken ct) => fs);

        // Act
        var result = await _executor.ExecuteFeedingAsync(horse.Id, feeding.Id);

        // Assert
        Assert.True(result.PreferenceDiscovered);
        Assert.NotNull(capturedPreference);
        Assert.Equal(horse.Id, capturedPreference.HorseId);
        Assert.Equal(feeding.Id, capturedPreference.FeedingId);
        Assert.Equal(FeedResponse.Favorite, capturedPreference.Preference);

        _repositoryMock.Verify(r => r.CreateAsync(
            It.IsAny<HorseFeedingPreference>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "StatUpdates")]
    public async Task ExecuteFeedingAsync_UpdatesHorseStatistics()
    {
        // Arrange
        var horse = CreateHorse();
        var initialSpeed = horse.Speed;
        var initialStamina = horse.Stamina;
        var initialAgility = horse.Agility;
        var initialDurability = horse.Durability;
        var initialHappiness = horse.Happiness;

        var feeding = CreateFeeding();

        SetupHorseRepository(horse);
        SetupFeedingRepository(feeding);
        SetupPreferenceCalculation(FeedResponse.Favorite, discovered: false);
        SetupCalculatorForPositiveGains();

        var existingPreference = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = horse.Id,
            FeedingId = feeding.Id,
            Preference = FeedResponse.Favorite
        };

        _repositoryMock.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<HorseFeedingPreferenceSpecification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPreference);

        Horse? updatedHorse = null;
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Horse>(), It.IsAny<CancellationToken>()))
            .Callback<Horse, CancellationToken>((h, ct) => updatedHorse = h)
            .ReturnsAsync(horse);

        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<FeedingSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedingSession fs, CancellationToken ct) => fs);

        // Act
        await _executor.ExecuteFeedingAsync(horse.Id, feeding.Id);

        // Assert
        Assert.NotNull(updatedHorse);
        Assert.True(updatedHorse.Speed > initialSpeed);
        Assert.True(updatedHorse.Stamina > initialStamina);
        Assert.True(updatedHorse.Agility > initialAgility);
        Assert.True(updatedHorse.Durability > initialDurability);
        Assert.True(updatedHorse.Happiness > initialHappiness);
        Assert.True(updatedHorse.HasFedSinceLastRace);
    }

    [Fact]
    [Trait("Category", "SessionCreation")]
    public async Task ExecuteFeedingAsync_CreatesFeedingSession()
    {
        // Arrange
        var horse = CreateHorse();
        var feeding = CreateFeeding();

        SetupHorseRepository(horse);
        SetupFeedingRepository(feeding);
        SetupPreferenceCalculation(FeedResponse.Liked, discovered: false);
        SetupCalculatorForPositiveGains();

        var existingPreference = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = horse.Id,
            FeedingId = feeding.Id,
            Preference = FeedResponse.Liked
        };

        _repositoryMock.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<HorseFeedingPreferenceSpecification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPreference);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Horse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(horse);

        FeedingSession? capturedSession = null;
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<FeedingSession>(), It.IsAny<CancellationToken>()))
            .Callback<FeedingSession, CancellationToken>((fs, ct) => capturedSession = fs)
            .ReturnsAsync((FeedingSession fs, CancellationToken ct) => fs);

        // Act
        await _executor.ExecuteFeedingAsync(horse.Id, feeding.Id);

        // Assert
        Assert.NotNull(capturedSession);
        Assert.Equal(horse.Id, capturedSession.HorseId);
        Assert.Equal(feeding.Id, capturedSession.FeedingId);
        Assert.Equal(FeedResponse.Liked, capturedSession.Result);
        Assert.Equal(horse.RaceStarts, capturedSession.RaceStartsAtTime);
        Assert.False(capturedSession.PreferenceDiscovered); // Known preference
        Assert.False(capturedSession.UpsetStomachOccurred);

        _repositoryMock.Verify(r => r.CreateAsync(
            It.IsAny<FeedingSession>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "StatUpdates")]
    public async Task ExecuteFeedingAsync_StatsAtCeiling_ReportsCorrectly()
    {
        // Arrange
        var horse = CreateHorse();
        // Set stats at ceiling
        horse.Statistics.First(s => s.StatisticId == StatisticId.Speed).Actual = 100;
        horse.Statistics.First(s => s.StatisticId == StatisticId.Speed).DominantPotential = 100;

        var feeding = CreateFeeding();

        SetupHorseRepository(horse);
        SetupFeedingRepository(feeding);
        SetupPreferenceCalculation(FeedResponse.Neutral, discovered: false);

        _calculatorMock.Setup(c => c.CalculateHappinessGain(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<FeedResponse>(), It.IsAny<double>()))
            .Returns(2.0);

        _calculatorMock.Setup(c => c.CalculateStatGain(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<FeedResponse>(), It.IsAny<double>()))
            .Returns(0.0); // No gain at ceiling

        SetupModifierCalculations();

        var existingPreference = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = horse.Id,
            FeedingId = feeding.Id,
            Preference = FeedResponse.Neutral
        };

        _repositoryMock.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<HorseFeedingPreferenceSpecification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPreference);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Horse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(horse);

        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<FeedingSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedingSession fs, CancellationToken ct) => fs);

        // Act
        var result = await _executor.ExecuteFeedingAsync(horse.Id, feeding.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0.0, result.SpeedGain);
        Assert.Equal(0.0, result.StaminaGain);
        Assert.Equal(0.0, result.AgilityGain);
        Assert.Equal(0.0, result.DurabilityGain);
        Assert.Equal(2.0, result.HappinessGain); // Happiness still gained
    }

    private static Horse CreateHorse()
    {
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            Name = "Test Horse",
            RaceStarts = 5,
            IsRetired = false,
            HasFedSinceLastRace = false,
            LegTypeId = LegTypeId.FrontRunner,
            Statistics = new List<HorseStatistic>()
        };

        horse.Statistics.Add(new HorseStatistic
        {
            HorseId = horse.Id,
            StatisticId = StatisticId.Speed,
            Actual = 50,
            DominantPotential = 80
        });

        horse.Statistics.Add(new HorseStatistic
        {
            HorseId = horse.Id,
            StatisticId = StatisticId.Stamina,
            Actual = 45,
            DominantPotential = 75
        });

        horse.Statistics.Add(new HorseStatistic
        {
            HorseId = horse.Id,
            StatisticId = StatisticId.Agility,
            Actual = 55,
            DominantPotential = 85
        });

        horse.Statistics.Add(new HorseStatistic
        {
            HorseId = horse.Id,
            StatisticId = StatisticId.Durability,
            Actual = 60,
            DominantPotential = 90
        });

        horse.Statistics.Add(new HorseStatistic
        {
            HorseId = horse.Id,
            StatisticId = StatisticId.Happiness,
            Actual = 70,
            DominantPotential = 100
        });

        return horse;
    }

    private static FeedingEntity CreateFeeding()
    {
        return new FeedingEntity
        {
            Id = 1,
            Name = "Test Feeding",
            Description = "A test feeding",
            CategoryId = FeedingCategoryId.Treats,
            SpeedMin = 1.0,
            SpeedMax = 3.0,
            StaminaMin = 1.0,
            StaminaMax = 3.0,
            AgilityMin = 1.0,
            AgilityMax = 3.0,
            DurabilityMin = 1.0,
            DurabilityMax = 3.0,
            HappinessMin = 3.0,
            HappinessMax = 8.0
        };
    }

    private void SetupHorseRepository(Horse horse)
    {
        _repositoryMock.Setup(r => r.SingleOrDefaultAsync(
                It.IsAny<HorseWithStatsSpecification>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(horse);
    }

    private void SetupFeedingRepository(FeedingEntity feeding)
    {
        _repositoryMock.Setup(r => r.FindAsync<FeedingEntity>(feeding.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feeding);
    }

    private void SetupPreferenceCalculation(FeedResponse preference, bool discovered)
    {
        _calculatorMock.Setup(c => c.CalculatePreference(
                It.IsAny<Guid>(), It.IsAny<byte>(), It.IsAny<FeedingCategoryId>()))
            .Returns(preference);
    }

    private void SetupCalculatorForPositiveGains()
    {
        _calculatorMock.Setup(c => c.CalculateHappinessGain(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<FeedResponse>(), It.IsAny<double>()))
            .Returns(5.0);

        _calculatorMock.Setup(c => c.CalculateStatGain(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<FeedResponse>(), It.IsAny<double>()))
            .Returns(3.0);

        SetupModifierCalculations();
    }

    private void SetupModifierCalculations()
    {
        _calculatorMock.Setup(c => c.CalculateCareerPhaseModifier(It.IsAny<short>(), It.IsAny<bool>()))
            .Returns(1.0);

        _calculatorMock.Setup(c => c.CalculateHappinessEffectivenessModifier(It.IsAny<double>()))
            .Returns(1.0);

        _calculatorMock.Setup(c => c.CalculateLegTypeBonus(It.IsAny<LegTypeId>(), It.IsAny<FeedingCategoryId>()))
            .Returns(1.0);
    }
}
