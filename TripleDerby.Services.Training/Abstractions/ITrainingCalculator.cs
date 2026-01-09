using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Training.Abstractions;

/// <summary>
/// Pure domain calculator for horse training mechanics.
/// Contains zero infrastructure dependencies - all calculations are deterministic functions.
/// </summary>
public interface ITrainingCalculator
{
    /// <summary>
    /// Calculates stat growth from a single training session.
    /// Formula: Gap × BaseGrowthRate × TrainingModifier × CareerMultiplier × HappinessModifier × LegTypeBonus
    /// Returns 0 if horse has reached genetic ceiling (actualStat >= dominantPotential).
    /// </summary>
    /// <param name="actualStat">Current stat value</param>
    /// <param name="dominantPotential">Genetic ceiling from breeding</param>
    /// <param name="trainingModifier">Training type effectiveness (0.1-1.0)</param>
    /// <param name="careerMultiplier">Career phase multiplier (0.40-1.40)</param>
    /// <param name="happinessModifier">Happiness effectiveness (0.5-1.0)</param>
    /// <param name="legTypeBonus">LegType match bonus (1.0 or 1.20)</param>
    /// <returns>Stat gain amount (capped at gap to ceiling)</returns>
    double CalculateTrainingGain(
        double actualStat,
        double dominantPotential,
        double trainingModifier,
        double careerMultiplier,
        double happinessModifier,
        double legTypeBonus);

    /// <summary>
    /// Calculates career phase multiplier for training effectiveness.
    /// Young (0-19): 1.20x, Prime (20-59): 1.40x, Veteran (60-99): 0.80x, Old (100+): 0.40x.
    /// Note: These multipliers are DIFFERENT from racing (young horses train better than they race).
    /// </summary>
    /// <param name="raceStarts">Total races the horse has started</param>
    /// <returns>Career phase multiplier (0.40-1.40)</returns>
    double CalculateTrainingCareerMultiplier(short raceStarts);

    /// <summary>
    /// Calculates happiness impact from training, including overwork check.
    /// Base happiness cost is modified by overwork risk (increases at low happiness).
    /// If overwork occurs, applies additional -5 happiness penalty.
    /// </summary>
    /// <param name="baseHappinessCost">Base happiness cost from training type (negative for recovery)</param>
    /// <param name="currentHappiness">Horse's current happiness (0-100)</param>
    /// <param name="overworkRisk">Base overwork probability (0.0-0.25)</param>
    /// <returns>Tuple of (happiness change, overwork occurred)</returns>
    (double happinessChange, bool overwork) CalculateHappinessImpact(
        double baseHappinessCost,
        double currentHappiness,
        double overworkRisk);

    /// <summary>
    /// Calculates training effectiveness modifier based on happiness.
    /// Linear scale: 0% happiness = 0.5x, 100% happiness = 1.0x.
    /// Lower happiness = less effective training.
    /// </summary>
    /// <param name="happiness">Current happiness (0-100)</param>
    /// <returns>Effectiveness multiplier (0.5-1.0)</returns>
    double CalculateHappinessEffectivenessModifier(double happiness);

    /// <summary>
    /// Calculates LegType bonus for training that matches running style.
    /// Returns 1.20 if training matches horse's preferred style, 1.0 otherwise.
    /// </summary>
    /// <param name="legType">Horse's running style</param>
    /// <param name="trainingId">ID of training type being performed</param>
    /// <returns>LegType bonus multiplier (1.0 or 1.20)</returns>
    double CalculateLegTypeBonus(LegTypeId legType, byte trainingId);
}
