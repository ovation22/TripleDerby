using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Entities;
using TripleDerby.Services.Training.Abstractions;
using TripleDerby.Services.Training.Config;
using TripleDerby.Services.Training.DTOs;
using TripleDerby.SharedKernel.Enums;
using TrainingEntity = TripleDerby.Core.Entities.Training;

namespace TripleDerby.Services.Training;

/// <summary>
/// Service for managing horse training sessions.
/// Orchestrates training execution following clean architecture principles.
/// Part of Feature 020: Horse Training System.
/// </summary>
public class TrainingService : ITrainingService
{
    private readonly ITrainingCalculator _calculator;
    private readonly ITripleDerbyRepository _repository;

    public TrainingService(
        ITrainingCalculator calculator,
        ITripleDerbyRepository repository)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Executes a training session for a horse.
    /// </summary>
    public async Task<TrainingSessionResult> TrainAsync(
        Guid horseId,
        byte trainingId,
        CancellationToken cancellationToken = default)
    {
        // 1. Load horse with statistics
        var horse = await LoadHorseWithStatisticsAsync(horseId, cancellationToken);

        // 2. Validate eligibility
        ValidateCanTrain(horse);

        // 3. Load training type
        var training = await LoadTrainingAsync(trainingId, cancellationToken);

        // 4. Calculate multipliers
        var careerMultiplier = _calculator.CalculateTrainingCareerMultiplier(horse.RaceStarts);
        var happinessModifier = _calculator.CalculateHappinessEffectivenessModifier(horse.Happiness);
        var legTypeBonus = _calculator.CalculateLegTypeBonus(horse.LegTypeId, training.Id);

        // 5. Calculate stat gains
        var statGains = CalculateStatGains(horse, training, careerMultiplier, happinessModifier, legTypeBonus);

        // 6. Calculate happiness impact
        var (happinessChange, overworkOccurred) = _calculator.CalculateHappinessImpact(
            training.HappinessCost,
            horse.Happiness,
            training.OverworkRisk);

        // 7. Apply changes to horse
        ApplyStatChanges(horse, statGains, happinessChange);
        horse.HasTrainedSinceLastRace = true;

        // 8. Create training session record
        var session = CreateTrainingSession(horse, training, statGains, happinessChange, overworkOccurred);

        // 9. Persist changes
        await _repository.UpdateAsync(horse, cancellationToken);
        await _repository.CreateAsync(session, cancellationToken);

        // 10. Return result
        return CreateResult(session, training, horse, statGains);
    }

    /// <summary>
    /// Retrieves training history for a horse.
    /// </summary>
    public async Task<List<TrainingSession>> GetTrainingHistoryAsync(
        Guid horseId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _repository.ListAsync<TrainingSession>(
            s => s.HorseId == horseId,
            cancellationToken);

        return sessions
            .OrderByDescending(s => s.SessionDate)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Checks whether a horse is eligible to train.
    /// </summary>
    public bool CanTrain(Horse horse)
    {
        if (horse.HasTrainedSinceLastRace)
            return false;

        if (horse.Happiness < TrainingConfig.MinimumHappinessToTrain)
            return false;

        return true;
    }

    // ============================================================================
    // Private Helper Methods
    // ============================================================================

    private async Task<Horse> LoadHorseWithStatisticsAsync(Guid horseId, CancellationToken cancellationToken)
    {
        var horse = await _repository.SingleOrDefaultAsync<Horse>(
            h => h.Id == horseId,
            cancellationToken);

        if (horse == null)
            throw new KeyNotFoundException($"Horse with ID {horseId} not found");

        return horse;
    }

    private void ValidateCanTrain(Horse horse)
    {
        if (!CanTrain(horse))
        {
            if (horse.HasTrainedSinceLastRace)
                throw new InvalidOperationException($"Horse {horse.Name} has already trained since last race");

            if (horse.Happiness < TrainingConfig.MinimumHappinessToTrain)
                throw new InvalidOperationException($"Horse {horse.Name} happiness ({horse.Happiness:F1}) is below minimum ({TrainingConfig.MinimumHappinessToTrain})");
        }
    }

    private async Task<TrainingEntity> LoadTrainingAsync(byte trainingId, CancellationToken cancellationToken)
    {
        var training = await _repository.FindAsync<TrainingEntity>(trainingId, cancellationToken);

        if (training == null)
            throw new KeyNotFoundException($"Training with ID {trainingId} not found");

        return training;
    }

    private StatGains CalculateStatGains(
        Horse horse,
        TrainingEntity training,
        double careerMultiplier,
        double happinessModifier,
        double legTypeBonus)
    {
        var speedStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Speed);
        var staminaStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Stamina);
        var agilityStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Agility);
        var durabilityStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Durability);

        return new StatGains
        {
            SpeedGain = _calculator.CalculateTrainingGain(
                speedStat.Actual,
                speedStat.DominantPotential,
                training.SpeedModifier,
                careerMultiplier,
                happinessModifier,
                legTypeBonus),

            StaminaGain = _calculator.CalculateTrainingGain(
                staminaStat.Actual,
                staminaStat.DominantPotential,
                training.StaminaModifier,
                careerMultiplier,
                happinessModifier,
                legTypeBonus),

            AgilityGain = _calculator.CalculateTrainingGain(
                agilityStat.Actual,
                agilityStat.DominantPotential,
                training.AgilityModifier,
                careerMultiplier,
                happinessModifier,
                legTypeBonus),

            DurabilityGain = _calculator.CalculateTrainingGain(
                durabilityStat.Actual,
                durabilityStat.DominantPotential,
                training.DurabilityModifier,
                careerMultiplier,
                happinessModifier,
                legTypeBonus)
        };
    }

    private static void ApplyStatChanges(Horse horse, StatGains gains, double happinessChange)
    {
        horse.Speed += gains.SpeedGain;
        horse.Stamina += gains.StaminaGain;
        horse.Agility += gains.AgilityGain;
        horse.Durability += gains.DurabilityGain;
        horse.Happiness += happinessChange;  // Negative for cost, positive for recovery
    }

    private static TrainingSession CreateTrainingSession(
        Horse horse,
        TrainingEntity training,
        StatGains gains,
        double happinessChange,
        bool overworkOccurred)
    {
        return new TrainingSession
        {
            Id = Guid.NewGuid(),
            HorseId = horse.Id,
            TrainingId = training.Id,
            SessionDate = DateTime.UtcNow,
            RaceStartsAtTime = horse.RaceStarts,
            SpeedGain = gains.SpeedGain,
            StaminaGain = gains.StaminaGain,
            AgilityGain = gains.AgilityGain,
            DurabilityGain = gains.DurabilityGain,
            HappinessChange = happinessChange,
            OverworkOccurred = overworkOccurred,
            Result = BuildResultMessage(training, gains, overworkOccurred)
        };
    }

    private static TrainingSessionResult CreateResult(
        TrainingSession session,
        TrainingEntity training,
        Horse horse,
        StatGains gains)
    {
        var speedStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Speed);
        var staminaStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Stamina);
        var agilityStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Agility);
        var durabilityStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Durability);

        return new TrainingSessionResult
        {
            SessionId = session.Id,
            TrainingName = training.Name,
            Success = true,
            Message = session.Result,
            SpeedGain = gains.SpeedGain,
            StaminaGain = gains.StaminaGain,
            AgilityGain = gains.AgilityGain,
            DurabilityGain = gains.DurabilityGain,
            HappinessChange = session.HappinessChange,
            OverworkOccurred = session.OverworkOccurred,
            CurrentHappiness = horse.Happiness,
            SpeedAtCeiling = speedStat.Actual >= speedStat.DominantPotential,
            StaminaAtCeiling = staminaStat.Actual >= staminaStat.DominantPotential,
            AgilityAtCeiling = agilityStat.Actual >= agilityStat.DominantPotential,
            DurabilityAtCeiling = durabilityStat.Actual >= durabilityStat.DominantPotential
        };
    }

    private static string BuildResultMessage(TrainingEntity training, StatGains gains, bool overworkOccurred)
    {
        var totalGain = gains.SpeedGain + gains.StaminaGain + gains.AgilityGain + gains.DurabilityGain;

        if (overworkOccurred)
            return $"Completed {training.Name} but horse became overworked. Total stat gain: {totalGain:F2}";

        if (totalGain < 0.1)
            return $"Completed {training.Name} with minimal gains (stats near ceiling). Total gain: {totalGain:F2}";

        return $"Successfully completed {training.Name}. Total stat gain: {totalGain:F2}";
    }

    /// <summary>
    /// Internal DTO for passing stat gains around.
    /// </summary>
    private record StatGains
    {
        public double SpeedGain { get; init; }
        public double StaminaGain { get; init; }
        public double AgilityGain { get; init; }
        public double DurabilityGain { get; init; }
    }
}
