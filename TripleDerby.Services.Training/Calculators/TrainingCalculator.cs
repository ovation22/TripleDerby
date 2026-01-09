using TripleDerby.Core.Abstractions.Utilities;
using TripleDerby.Services.Training.Abstractions;
using TripleDerby.Services.Training.Config;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Training.Calculators;

public class TrainingCalculator(IRandomGenerator randomGenerator) : ITrainingCalculator
{
    /// <summary>
    /// Calculates stat growth from a single training session.
    /// Formula: Gap × BaseGrowthRate × TrainingModifier × CareerMultiplier × HappinessModifier × LegTypeBonus
    /// Returns 0 if horse has reached genetic ceiling (actualStat >= dominantPotential).
    /// Growth is capped to prevent exceeding the genetic ceiling.
    /// </summary>
    /// <param name="actualStat">Current stat value</param>
    /// <param name="dominantPotential">Genetic ceiling from breeding</param>
    /// <param name="trainingModifier">Training type effectiveness (0.1-1.0)</param>
    /// <param name="careerMultiplier">Career phase multiplier (0.40-1.40)</param>
    /// <param name="happinessModifier">Happiness effectiveness (0.5-1.0)</param>
    /// <param name="legTypeBonus">LegType match bonus (1.0 or 1.20)</param>
    /// <returns>Stat gain amount (capped at gap to ceiling)</returns>
    public double CalculateTrainingGain(
        double actualStat,
        double dominantPotential,
        double trainingModifier,
        double careerMultiplier,
        double happinessModifier,
        double legTypeBonus)
    {
        // No growth if already at or above genetic ceiling
        if (actualStat >= dominantPotential)
            return 0;

        // Calculate gap-based growth: larger gaps = more growth per session
        var gap = dominantPotential - actualStat;
        var baseGrowth = gap * TrainingConfig.BaseTrainingGrowthRate;

        // Apply all multipliers
        var growth = baseGrowth
            * trainingModifier
            * careerMultiplier
            * happinessModifier
            * legTypeBonus;

        // Cap growth to not exceed ceiling
        return Math.Min(growth, gap);
    }

    /// <summary>
    /// Calculates career phase multiplier for training effectiveness.
    /// Young (0-19): 1.20x, Prime (20-59): 1.40x, Veteran (60-99): 0.80x, Old (100+): 0.40x.
    /// Note: These multipliers are DIFFERENT from racing progression.
    /// Young horses train better (1.20x) but race worse (0.80x in racing system).
    /// </summary>
    /// <param name="raceStarts">Total races the horse has started</param>
    /// <returns>Career phase multiplier (0.40-1.40)</returns>
    public double CalculateTrainingCareerMultiplier(short raceStarts)
    {
        return raceStarts switch
        {
            < 20 => TrainingConfig.YoungHorseTrainingMultiplier,     // 1.20
            < 60 => TrainingConfig.PrimeHorseTrainingMultiplier,     // 1.40
            < 100 => TrainingConfig.VeteranHorseTrainingMultiplier,  // 0.80
            _ => TrainingConfig.OldHorseTrainingMultiplier           // 0.40
        };
    }

    /// <summary>
    /// Calculates happiness impact from training, including overwork check.
    /// Overwork risk increases as happiness decreases:
    /// - At 50% happiness: 2x risk
    /// - At 25% happiness: 4x risk
    /// If overwork occurs, applies additional -5 happiness penalty and reduces training gains by 50%.
    /// </summary>
    /// <param name="baseHappinessCost">Base happiness cost from training type (negative for recovery)</param>
    /// <param name="currentHappiness">Horse's current happiness (0-100)</param>
    /// <param name="overworkRisk">Base overwork probability (0.0-0.25)</param>
    /// <returns>Tuple of (happiness change, overwork occurred)</returns>
    public (double happinessChange, bool overwork) CalculateHappinessImpact(
        double baseHappinessCost,
        double currentHappiness,
        double overworkRisk)
    {
        // Recovery training (negative cost) never causes overwork
        if (baseHappinessCost < 0)
            return (happinessChange: -baseHappinessCost, overwork: false);

        // Calculate adjusted overwork risk based on current happiness
        // Lower happiness = higher overwork risk (exponential increase)
        var happinessRatio = currentHappiness / 100.0;
        var riskMultiplier = happinessRatio >= 0.5
            ? 1.0                                    // Above 50%: normal risk
            : happinessRatio >= 0.25
                ? 2.0                                // 25-50%: 2x risk
                : 4.0;                               // Below 25%: 4x risk

        var adjustedRisk = overworkRisk * riskMultiplier;

        // Roll for overwork
        var overworkOccurred = randomGenerator.NextDouble() < adjustedRisk;

        // Calculate final happiness change
        var happinessChange = -baseHappinessCost;
        if (overworkOccurred)
        {
            happinessChange -= TrainingConfig.OverworkHappinessPenalty;
        }

        return (happinessChange, overworkOccurred);
    }

    /// <summary>
    /// Calculates training effectiveness modifier based on happiness.
    /// Linear scale: 0% happiness = 0.5x effectiveness, 100% happiness = 1.0x effectiveness.
    /// Formula: 0.5 + (happiness / 100) × 0.5
    /// Lower happiness = less effective training but still provides some benefit.
    /// </summary>
    /// <param name="happiness">Current happiness (0-100)</param>
    /// <returns>Effectiveness multiplier (0.5-1.0)</returns>
    public double CalculateHappinessEffectivenessModifier(double happiness)
    {
        // Clamp happiness to valid range
        var clampedHappiness = Math.Clamp(happiness, 0, 100);

        // Linear interpolation: 0.5 at 0% happiness, 1.0 at 100% happiness
        return 0.5 + (clampedHappiness / 100.0) * 0.5;
    }

    /// <summary>
    /// Calculates LegType bonus for training that matches running style.
    /// Each LegType has a preferred training that provides +20% effectiveness.
    /// - StartDash → Sprint Drills (ID 1)
    /// - FrontRunner → Interval Training (ID 6)
    /// - StretchRunner → Distance Gallops (ID 2)
    /// - LastSpurt → Hill Climbing (ID 5)
    /// - RailRunner → Agility Course (ID 3)
    /// </summary>
    /// <param name="legType">Horse's running style</param>
    /// <param name="trainingId">ID of training type being performed</param>
    /// <returns>LegType bonus multiplier (1.0 or 1.20)</returns>
    public double CalculateLegTypeBonus(LegTypeId legType, byte trainingId)
    {
        // Check if this training is the horse's preferred type
        if (TrainingConfig.LegTypePreferredTraining.TryGetValue(legType, out var preferredTrainingId))
        {
            if (trainingId == preferredTrainingId)
                return TrainingConfig.LegTypeBonusMultiplier;  // 1.20
        }

        return 1.0;  // No bonus
    }
}
