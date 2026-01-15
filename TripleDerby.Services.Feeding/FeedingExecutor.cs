using TripleDerby.Core.Abstractions.Repositories;
using TripleDerby.Core.Entities;
using TripleDerby.Core.Specifications;
using TripleDerby.Services.Feeding.Abstractions;
using TripleDerby.Services.Feeding.Config;
using TripleDerby.Services.Feeding.DTOs;
using TripleDerby.SharedKernel.Enums;
using FeedingEntity = TripleDerby.Core.Entities.Feeding;

namespace TripleDerby.Services.Feeding;

/// <summary>
/// Executor for horse feeding business logic.
/// </summary>
public class FeedingExecutor(
    IFeedingCalculator calculator,
    ITripleDerbyRepository repository)
    : IFeedingExecutor
{
    private readonly IFeedingCalculator _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    private readonly ITripleDerbyRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>
    /// Executes a feeding session for a horse.
    /// </summary>
    public async Task<FeedingSessionResult> ExecuteFeedingAsync(
        Guid horseId,
        byte feedingId,
        CancellationToken cancellationToken = default)
    {
        var horse = await LoadHorseWithStatisticsAsync(horseId, cancellationToken);
        ValidateCanFeed(horse);

        var feeding = await LoadFeedingAsync(feedingId, cancellationToken);

        var (preference, preferenceDiscovered) = await GetOrCalculatePreferenceAsync(horse, feeding, cancellationToken);

        if (preference == FeedResponse.Rejected)
        {
            horse.HasFedSinceLastRace = true;
            await _repository.UpdateAsync(horse, cancellationToken);

            return CreateRejectedResult(feeding, horse);
        }

        var careerMultiplier = _calculator.CalculateCareerPhaseModifier(horse.RaceStarts, horse.IsRetired);
        var happinessModifier = _calculator.CalculateHappinessEffectivenessModifier(horse.Happiness);
        var legTypeBonus = _calculator.CalculateLegTypeBonus(horse.LegTypeId, feeding.CategoryId);

        var happinessGain = _calculator.CalculateHappinessGain(
            feeding.HappinessMin,
            feeding.HappinessMax,
            preference,
            happinessModifier);

        var statGains = CalculateStatGains(horse, feeding, preference, happinessModifier, careerMultiplier, legTypeBonus);

        var upsetStomach = false;
        if (preference == FeedResponse.Hated)
        {
            upsetStomach = _calculator.RollUpsetStomach(horse.Id, feedingId, DateTime.UtcNow);
            if (upsetStomach)
            {
                happinessGain += FeedingConfig.UpsetStomachPenalty;
            }
        }

        ApplyStatChanges(horse, statGains, happinessGain);
        horse.HasFedSinceLastRace = true;

        var session = CreateFeedingSession(horse, feeding, preference, statGains, happinessGain, upsetStomach, preferenceDiscovered);

        await _repository.UpdateAsync(horse, cancellationToken);
        await _repository.CreateAsync(session, cancellationToken);

        return CreateResult(session, feeding, horse, preference, statGains, happinessGain, upsetStomach, preferenceDiscovered);
    }

    private async Task<Horse> LoadHorseWithStatisticsAsync(Guid horseId, CancellationToken cancellationToken)
    {
        var spec = new HorseWithStatsSpecification(horseId);
        var horse = await _repository.SingleOrDefaultAsync(spec, cancellationToken);

        if (horse == null)
            throw new KeyNotFoundException($"Horse with ID {horseId} not found");

        return horse;
    }

    private static void ValidateCanFeed(Horse horse)
    {
        if (horse.HasFedSinceLastRace)
            throw new InvalidOperationException($"Horse {horse.Name} has already been fed since last race");
    }

    private async Task<FeedingEntity> LoadFeedingAsync(byte feedingId, CancellationToken cancellationToken)
    {
        var feeding = await _repository.FindAsync<FeedingEntity>(feedingId, cancellationToken);

        if (feeding == null)
            throw new KeyNotFoundException($"Feeding with ID {feedingId} not found");

        return feeding;
    }

    private async Task<(FeedResponse preference, bool discovered)> GetOrCalculatePreferenceAsync(
        Horse horse,
        FeedingEntity feeding,
        CancellationToken cancellationToken)
    {
        var existingPreference = await _repository.SingleOrDefaultAsync(
            new HorseFeedingPreferenceSpecification(horse.Id, feeding.Id),
            cancellationToken);

        if (existingPreference != null)
        {
            return (existingPreference.Preference, false);
        }

        var calculatedPreference = _calculator.CalculatePreference(horse.Id, feeding.Id, feeding.CategoryId);

        var preferenceEntity = new HorseFeedingPreference
        {
            Id = Guid.NewGuid(),
            HorseId = horse.Id,
            FeedingId = feeding.Id,
            Preference = calculatedPreference,
            DiscoveredDate = DateTime.UtcNow
        };

        await _repository.CreateAsync(preferenceEntity, cancellationToken);

        return (calculatedPreference, true);
    }

    private StatGains CalculateStatGains(
        Horse horse,
        FeedingEntity feeding,
        FeedResponse preference,
        double happinessModifier,
        double careerMultiplier,
        double legTypeBonus)
    {
        var speedStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Speed);
        var staminaStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Stamina);
        var agilityStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Agility);
        var durabilityStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Durability);

        var combinedModifier = happinessModifier * careerMultiplier * legTypeBonus;

        return new StatGains
        {
            SpeedGain = _calculator.CalculateStatGain(
                speedStat.Actual,
                speedStat.DominantPotential,
                feeding.SpeedMin,
                feeding.SpeedMax,
                preference,
                combinedModifier),

            StaminaGain = _calculator.CalculateStatGain(
                staminaStat.Actual,
                staminaStat.DominantPotential,
                feeding.StaminaMin,
                feeding.StaminaMax,
                preference,
                combinedModifier),

            AgilityGain = _calculator.CalculateStatGain(
                agilityStat.Actual,
                agilityStat.DominantPotential,
                feeding.AgilityMin,
                feeding.AgilityMax,
                preference,
                combinedModifier),

            DurabilityGain = _calculator.CalculateStatGain(
                durabilityStat.Actual,
                durabilityStat.DominantPotential,
                feeding.DurabilityMin,
                feeding.DurabilityMax,
                preference,
                combinedModifier)
        };
    }

    private static void ApplyStatChanges(Horse horse, StatGains gains, double happinessGain)
    {
        horse.Speed += gains.SpeedGain;
        horse.Stamina += gains.StaminaGain;
        horse.Agility += gains.AgilityGain;
        horse.Durability += gains.DurabilityGain;
        horse.Happiness += happinessGain;
    }

    private static FeedingSession CreateFeedingSession(
        Horse horse,
        FeedingEntity feeding,
        FeedResponse preference,
        StatGains gains,
        double happinessGain,
        bool upsetStomach,
        bool preferenceDiscovered)
    {
        return new FeedingSession
        {
            Id = Guid.NewGuid(),
            HorseId = horse.Id,
            FeedingId = feeding.Id,
            SessionDate = DateTime.UtcNow,
            RaceStartsAtTime = horse.RaceStarts,
            Result = preference,
            HappinessGain = happinessGain,
            SpeedGain = gains.SpeedGain,
            StaminaGain = gains.StaminaGain,
            AgilityGain = gains.AgilityGain,
            DurabilityGain = gains.DurabilityGain,
            UpsetStomachOccurred = upsetStomach,
            PreferenceDiscovered = preferenceDiscovered
        };
    }

    private static FeedingSessionResult CreateRejectedResult(FeedingEntity feeding, Horse horse)
    {
        return new FeedingSessionResult
        {
            SessionId = Guid.NewGuid(),
            FeedingName = feeding.Name,
            Success = true,
            Message = $"{horse.Name} refused to eat {feeding.Name}. No effects applied.",
            Response = FeedResponse.Rejected,
            HappinessGain = 0,
            SpeedGain = 0,
            StaminaGain = 0,
            AgilityGain = 0,
            DurabilityGain = 0,
            UpsetStomachOccurred = false,
            PreferenceDiscovered = true,
            CurrentHappiness = horse.Happiness,
            SpeedAtCeiling = false,
            StaminaAtCeiling = false,
            AgilityAtCeiling = false,
            DurabilityAtCeiling = false
        };
    }

    private static FeedingSessionResult CreateResult(
        FeedingSession session,
        FeedingEntity feeding,
        Horse horse,
        FeedResponse preference,
        StatGains gains,
        double happinessGain,
        bool upsetStomach,
        bool preferenceDiscovered)
    {
        var speedStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Speed);
        var staminaStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Stamina);
        var agilityStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Agility);
        var durabilityStat = horse.Statistics.First(s => s.StatisticId == StatisticId.Durability);

        return new FeedingSessionResult
        {
            SessionId = session.Id,
            FeedingName = feeding.Name,
            Success = true,
            Message = BuildResultMessage(feeding.Name, preference, upsetStomach, preferenceDiscovered, gains, happinessGain),
            Response = preference,
            HappinessGain = happinessGain,
            SpeedGain = gains.SpeedGain,
            StaminaGain = gains.StaminaGain,
            AgilityGain = gains.AgilityGain,
            DurabilityGain = gains.DurabilityGain,
            UpsetStomachOccurred = upsetStomach,
            PreferenceDiscovered = preferenceDiscovered,
            CurrentHappiness = horse.Happiness,
            SpeedAtCeiling = speedStat.Actual >= speedStat.DominantPotential,
            StaminaAtCeiling = staminaStat.Actual >= staminaStat.DominantPotential,
            AgilityAtCeiling = agilityStat.Actual >= agilityStat.DominantPotential,
            DurabilityAtCeiling = durabilityStat.Actual >= durabilityStat.DominantPotential
        };
    }

    private static string BuildResultMessage(
        string feedingName,
        FeedResponse preference,
        bool upsetStomach,
        bool preferenceDiscovered,
        StatGains gains,
        double happinessGain)
    {
        var totalStatGain = gains.SpeedGain + gains.StaminaGain + gains.AgilityGain + gains.DurabilityGain;
        var discoveryText = preferenceDiscovered ? $" (discovered: {preference})" : "";

        if (upsetStomach)
            return $"Fed {feedingName}{discoveryText} but horse got upset stomach! Happiness: {happinessGain:F1}, Total stat gain: {totalStatGain:F2}";

        if (totalStatGain < 0.1 && happinessGain < 1.0)
            return $"Fed {feedingName}{discoveryText} with minimal gains (stats near ceiling). Happiness: {happinessGain:F1}, Total stat gain: {totalStatGain:F2}";

        return $"Successfully fed {feedingName}{discoveryText}. Happiness: {happinessGain:F1}, Total stat gain: {totalStatGain:F2}";
    }

    private record StatGains
    {
        public double SpeedGain { get; init; }
        public double StaminaGain { get; init; }
        public double AgilityGain { get; init; }
        public double DurabilityGain { get; init; }
    }
}
