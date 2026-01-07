using Moq;
using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Training;
using TripleDerby.Services.Training.Abstractions;
using TripleDerby.Services.Training.Config;
using TripleDerby.SharedKernel.Enums;
using TrainingEntity = TripleDerby.Core.Entities.Training;

namespace TripleDerby.Tests.Unit.Training;

/// <summary>
/// Tests for TrainingService (Feature 020: Horse Training System Phase 3).
/// Validates business rules, stat application, and data persistence.
/// </summary>
public class TrainingServiceTests
{
    private readonly Mock<ITrainingCalculator> _mockCalculator;
    private readonly Mock<ITripleDerbyRepository> _mockRepository;
    private readonly TrainingService _service;

    public TrainingServiceTests()
    {
        _mockCalculator = new Mock<ITrainingCalculator>();
        _mockRepository = new Mock<ITripleDerbyRepository>();
        _service = new TrainingService(_mockCalculator.Object, _mockRepository.Object);
    }

    // ============================================================================
    // Phase 1: CanTrain Validation Tests
    // ============================================================================

    [Fact]
    public void CanTrain_WhenAlreadyTrainedSinceLastRace_ReturnsFalse()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            HasTrainedSinceLastRace = true,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Happiness, Actual = 80.0 }
            }
        };

        // Act
        var result = _service.CanTrain(horse);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanTrain_WhenHappinessBelowMinimum_ReturnsFalse()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            HasTrainedSinceLastRace = false,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Happiness, Actual = 10.0 }  // Below 15%
            }
        };

        // Act
        var result = _service.CanTrain(horse);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanTrain_WhenEligible_ReturnsTrue()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            HasTrainedSinceLastRace = false,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Happiness, Actual = 80.0 }
            }
        };

        // Act
        var result = _service.CanTrain(horse);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanTrain_WhenHappinessExactlyAtMinimum_ReturnsTrue()
    {
        // Arrange
        var horse = new Horse
        {
            Id = Guid.NewGuid(),
            HasTrainedSinceLastRace = false,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Happiness, Actual = TrainingConfig.MinimumHappinessToTrain }
            }
        };

        // Act
        var result = _service.CanTrain(horse);

        // Assert
        Assert.True(result);
    }

    // ============================================================================
    // Phase 2: TrainAsync Business Rule Tests
    // ============================================================================

    [Fact]
    public async Task TrainAsync_WhenAlreadyTrained_ThrowsInvalidOperationException()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var horse = new Horse
        {
            Id = horseId,
            HasTrainedSinceLastRace = true,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Happiness, Actual = 80.0 }
            }
        };

        _mockRepository.Setup(r => r.SingleOrDefaultAsync<Horse>(It.IsAny<System.Linq.Expressions.Expression<Func<Horse, bool>>>(), default))
            .ReturnsAsync(horse);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.TrainAsync(horseId, trainingId: 1));
    }

    [Fact]
    public async Task TrainAsync_WhenHappinessTooLow_ThrowsInvalidOperationException()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var horse = new Horse
        {
            Id = horseId,
            HasTrainedSinceLastRace = false,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Happiness, Actual = 10.0 }
            }
        };

        _mockRepository.Setup(r => r.SingleOrDefaultAsync<Horse>(It.IsAny<System.Linq.Expressions.Expression<Func<Horse, bool>>>(), default))
            .ReturnsAsync(horse);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.TrainAsync(horseId, trainingId: 1));
    }

    [Fact]
    public async Task TrainAsync_WhenHorseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var horseId = Guid.NewGuid();

        _mockRepository.Setup(r => r.SingleOrDefaultAsync<Horse>(It.IsAny<System.Linq.Expressions.Expression<Func<Horse, bool>>>(), default))
            .ReturnsAsync((Horse?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.TrainAsync(horseId, trainingId: 1));
    }

    [Fact]
    public async Task TrainAsync_WhenTrainingNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var horse = CreateValidHorse(horseId);

        _mockRepository.Setup(r => r.SingleOrDefaultAsync<Horse>(It.IsAny<System.Linq.Expressions.Expression<Func<Horse, bool>>>(), default))
            .ReturnsAsync(horse);
        _mockRepository.Setup(r => r.FindAsync<TrainingEntity>((byte)1, default))
            .ReturnsAsync((TrainingEntity?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.TrainAsync(horseId, trainingId: 1));
    }

    // ============================================================================
    // Phase 3: TrainAsync Happy Path Tests
    // ============================================================================

    [Fact]
    public async Task TrainAsync_WithValidInput_CalculatesStatGainsCorrectly()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var horse = CreateValidHorse(horseId);
        var training = CreateTraining();

        SetupMocksForSuccessfulTraining(horse, training);

        // Setup calculator mocks
        _mockCalculator.Setup(c => c.CalculateTrainingCareerMultiplier(horse.RaceStarts))
            .Returns(1.40);  // Prime
        _mockCalculator.Setup(c => c.CalculateHappinessEffectivenessModifier(80.0))
            .Returns(0.9);
        _mockCalculator.Setup(c => c.CalculateLegTypeBonus(horse.LegTypeId, training.Id))
            .Returns(1.20);  // Matching
        _mockCalculator.Setup(c => c.CalculateTrainingGain(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(0.85);
        _mockCalculator.Setup(c => c.CalculateHappinessImpact(8.0, 80.0, 0.15))
            .Returns((-8.0, false));

        // Act
        var result = await _service.TrainAsync(horseId, trainingId: 1);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0.85, result.SpeedGain);
        Assert.Equal(-8.0, result.HappinessChange);
        Assert.False(result.OverworkOccurred);
    }

    [Fact]
    public async Task TrainAsync_WithValidInput_AppliesStatChangesToHorse()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var horse = CreateValidHorse(horseId);
        var training = CreateTraining();

        SetupMocksForSuccessfulTraining(horse, training);

        _mockCalculator.Setup(c => c.CalculateTrainingCareerMultiplier(It.IsAny<short>())).Returns(1.40);
        _mockCalculator.Setup(c => c.CalculateHappinessEffectivenessModifier(It.IsAny<double>())).Returns(0.9);
        _mockCalculator.Setup(c => c.CalculateLegTypeBonus(It.IsAny<LegTypeId>(), It.IsAny<byte>())).Returns(1.0);
        _mockCalculator.Setup(c => c.CalculateTrainingGain(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(0.50);
        _mockCalculator.Setup(c => c.CalculateHappinessImpact(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns((-8.0, false));

        // Act
        await _service.TrainAsync(horseId, trainingId: 1);

        // Assert - Verify horse stats were updated
        Assert.Equal(50.5, horse.Speed);  // Was 50.0 + 0.50 gain
        Assert.Equal(72.0, horse.Happiness);  // Was 80.0 - 8.0 cost
    }

    [Fact]
    public async Task TrainAsync_WithValidInput_SetsHasTrainedSinceLastRaceFlag()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var horse = CreateValidHorse(horseId);
        var training = CreateTraining();

        SetupMocksForSuccessfulTraining(horse, training);

        _mockCalculator.Setup(c => c.CalculateTrainingCareerMultiplier(It.IsAny<short>())).Returns(1.40);
        _mockCalculator.Setup(c => c.CalculateHappinessEffectivenessModifier(It.IsAny<double>())).Returns(0.9);
        _mockCalculator.Setup(c => c.CalculateLegTypeBonus(It.IsAny<LegTypeId>(), It.IsAny<byte>())).Returns(1.0);
        _mockCalculator.Setup(c => c.CalculateTrainingGain(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(0.50);
        _mockCalculator.Setup(c => c.CalculateHappinessImpact(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns((-8.0, false));

        // Act
        await _service.TrainAsync(horseId, trainingId: 1);

        // Assert
        Assert.True(horse.HasTrainedSinceLastRace);
    }

    [Fact]
    public async Task TrainAsync_WithValidInput_SavesTrainingSessionWithDetails()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var horse = CreateValidHorse(horseId);
        var training = CreateTraining();

        SetupMocksForSuccessfulTraining(horse, training);

        _mockCalculator.Setup(c => c.CalculateTrainingCareerMultiplier(It.IsAny<short>())).Returns(1.40);
        _mockCalculator.Setup(c => c.CalculateHappinessEffectivenessModifier(It.IsAny<double>())).Returns(0.9);
        _mockCalculator.Setup(c => c.CalculateLegTypeBonus(It.IsAny<LegTypeId>(), It.IsAny<byte>())).Returns(1.0);
        _mockCalculator.Setup(c => c.CalculateTrainingGain(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(0.50);
        _mockCalculator.Setup(c => c.CalculateHappinessImpact(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns((-8.0, false));

        TrainingSession? savedSession = null;
        _mockRepository.Setup(r => r.CreateAsync<TrainingSession>(It.IsAny<TrainingSession>(), default))
            .Callback<TrainingSession, CancellationToken>((session, _) => savedSession = session)
            .ReturnsAsync((TrainingSession s, CancellationToken _) => s);

        // Act
        await _service.TrainAsync(horseId, trainingId: 1);

        // Assert
        Assert.NotNull(savedSession);
        Assert.Equal(horseId, savedSession.HorseId);
        Assert.Equal(1, savedSession.TrainingId);
        Assert.Equal(0.50, savedSession.SpeedGain);
        Assert.Equal(-8.0, savedSession.HappinessChange);
        Assert.False(savedSession.OverworkOccurred);
    }

    // ============================================================================
    // Phase 4: Overwork and Edge Case Tests
    // ============================================================================

    [Fact]
    public async Task TrainAsync_WhenOverworkOccurs_AppliesExtraHappinessPenalty()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var horse = CreateValidHorse(horseId);
        horse.Happiness = 30.0;  // Low happiness increases risk
        var training = CreateTraining();

        SetupMocksForSuccessfulTraining(horse, training);

        _mockCalculator.Setup(c => c.CalculateTrainingCareerMultiplier(It.IsAny<short>())).Returns(1.40);
        _mockCalculator.Setup(c => c.CalculateHappinessEffectivenessModifier(It.IsAny<double>())).Returns(0.65);
        _mockCalculator.Setup(c => c.CalculateLegTypeBonus(It.IsAny<LegTypeId>(), It.IsAny<byte>())).Returns(1.0);
        _mockCalculator.Setup(c => c.CalculateTrainingGain(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(0.40);
        _mockCalculator.Setup(c => c.CalculateHappinessImpact(8.0, 30.0, 0.15))
            .Returns((-13.0, true));  // Overwork: -8 base -5 penalty

        // Act
        var result = await _service.TrainAsync(horseId, trainingId: 1);

        // Assert
        Assert.True(result.OverworkOccurred);
        Assert.Equal(-13.0, result.HappinessChange);
        Assert.Equal(17.0, result.CurrentHappiness);  // 30 - 13
    }

    [Fact]
    public async Task TrainAsync_WhenStatAtCeiling_ReturnsZeroGainAndFlag()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var horse = CreateValidHorse(horseId);
        // Set Speed to ceiling
        var speedStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Speed);
        speedStat.Actual = speedStat.DominantPotential;

        var training = CreateTraining();
        SetupMocksForSuccessfulTraining(horse, training);

        _mockCalculator.Setup(c => c.CalculateTrainingCareerMultiplier(It.IsAny<short>())).Returns(1.40);
        _mockCalculator.Setup(c => c.CalculateHappinessEffectivenessModifier(It.IsAny<double>())).Returns(0.9);
        _mockCalculator.Setup(c => c.CalculateLegTypeBonus(It.IsAny<LegTypeId>(), It.IsAny<byte>())).Returns(1.0);

        // Speed at ceiling returns 0
        _mockCalculator.Setup(c => c.CalculateTrainingGain(
            speedStat.DominantPotential, speedStat.DominantPotential,
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(0.0);

        // Other stats return normal gains
        _mockCalculator.Setup(c => c.CalculateTrainingGain(
            It.Is<double>(d => d != speedStat.DominantPotential), It.IsAny<double>(),
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns(0.20);

        _mockCalculator.Setup(c => c.CalculateHappinessImpact(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .Returns((-8.0, false));

        // Act
        var result = await _service.TrainAsync(horseId, trainingId: 1);

        // Assert
        Assert.True(result.SpeedAtCeiling);
        Assert.Equal(0.0, result.SpeedGain);
    }

    // ============================================================================
    // Phase 5: GetTrainingHistoryAsync Tests
    // ============================================================================

    [Fact]
    public async Task GetTrainingHistoryAsync_ReturnsSessionsOrderedByDate()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var sessions = new List<TrainingSession>
        {
            new() { Id = Guid.NewGuid(), HorseId = horseId, SessionDate = DateTime.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), HorseId = horseId, SessionDate = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), HorseId = horseId, SessionDate = DateTime.UtcNow }
        };

        _mockRepository.Setup(r => r.ListAsync<TrainingSession>(It.IsAny<System.Linq.Expressions.Expression<Func<TrainingSession, bool>>>(), default))
            .ReturnsAsync(sessions.OrderByDescending(s => s.SessionDate).ToList());

        // Act
        var result = await _service.GetTrainingHistoryAsync(horseId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result[0].SessionDate > result[1].SessionDate);
        Assert.True(result[1].SessionDate > result[2].SessionDate);
    }

    [Fact]
    public async Task GetTrainingHistoryAsync_RespectsLimitParameter()
    {
        // Arrange
        var horseId = Guid.NewGuid();
        var sessions = Enumerable.Range(0, 30)
            .Select(i => new TrainingSession
            {
                Id = Guid.NewGuid(),
                HorseId = horseId,
                SessionDate = DateTime.UtcNow.AddDays(-i)
            })
            .ToList();

        _mockRepository.Setup(r => r.ListAsync<TrainingSession>(It.IsAny<System.Linq.Expressions.Expression<Func<TrainingSession, bool>>>(), default))
            .ReturnsAsync(sessions.Take(10).ToList());

        // Act
        var result = await _service.GetTrainingHistoryAsync(horseId, limit: 10);

        // Assert
        Assert.Equal(10, result.Count);
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private static Horse CreateValidHorse(Guid horseId)
    {
        return new Horse
        {
            Id = horseId,
            Name = "Test Horse",
            LegTypeId = LegTypeId.StartDash,
            RaceStarts = 25,  // Prime age
            HasTrainedSinceLastRace = false,
            Statistics = new List<HorseStatistic>
            {
                new() { StatisticId = StatisticId.Speed, Actual = 50.0, DominantPotential = 100.0 },
                new() { StatisticId = StatisticId.Stamina, Actual = 45.0, DominantPotential = 95.0 },
                new() { StatisticId = StatisticId.Agility, Actual = 55.0, DominantPotential = 105.0 },
                new() { StatisticId = StatisticId.Durability, Actual = 60.0, DominantPotential = 110.0 },
                new() { StatisticId = StatisticId.Happiness, Actual = 80.0, DominantPotential = 100.0 }
            }
        };
    }

    private static TrainingEntity CreateTraining()
    {
        return new TrainingEntity
        {
            Id = 1,
            Name = "Sprint Drills",
            Description = "High-intensity sprint training",
            SpeedModifier = 1.0,
            StaminaModifier = 0.2,
            AgilityModifier = 0.3,
            DurabilityModifier = 0.1,
            HappinessCost = 8.0,
            OverworkRisk = 0.15,
            IsRecovery = false
        };
    }

    private void SetupMocksForSuccessfulTraining(Horse horse, TrainingEntity training)
    {
        _mockRepository.Setup(r => r.SingleOrDefaultAsync<Horse>(It.IsAny<System.Linq.Expressions.Expression<Func<Horse, bool>>>(), default))
            .ReturnsAsync(horse);
        _mockRepository.Setup(r => r.FindAsync<TrainingEntity>((byte)1, default))
            .ReturnsAsync(training);
        _mockRepository.Setup(r => r.UpdateAsync<Horse>(It.IsAny<Horse>(), default))
            .ReturnsAsync((Horse h, CancellationToken _) => h);
        _mockRepository.Setup(r => r.CreateAsync<TrainingSession>(It.IsAny<TrainingSession>(), default))
            .ReturnsAsync((TrainingSession s, CancellationToken _) => s);
    }
}
