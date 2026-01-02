using TripleDerby.Services.Racing.Calculators;

namespace TripleDerby.Services.Racing.Abstractions;

/// <summary>
/// Interface for calculating stat progression multipliers and growth for horses after races.
/// Implements career phase system, race-type focus, performance bonuses, and happiness changes.
/// Part of Feature 018: Race Outcome Stat Progression System.
/// </summary>
public interface IStatProgressionCalculator
{
    /// <summary>
    /// Calculates development efficiency multiplier based on career stage.
    /// Young horses (0-9 races) learn slower, prime horses (10-29) learn fastest,
    /// veterans (30-49) slow down, old horses (50+) show minimal development.
    /// </summary>
    /// <param name="raceStarts">Total number of races the horse has started</param>
    /// <returns>Development efficiency multiplier (0.20 to 1.20)</returns>
    double CalculateAgeMultiplier(short raceStarts);

    /// <summary>
    /// Calculates stat growth for a single stat based on the gap between actual and genetic potential.
    /// Uses the base formula: Growth = (DominantPotential - Actual) × BaseGrowthRate × CareerMultiplier
    /// Returns 0 if the horse has already reached its genetic ceiling.
    /// </summary>
    /// <param name="actualStat">Current stat value</param>
    /// <param name="dominantPotential">Genetic ceiling from breeding</param>
    /// <param name="careerMultiplier">Career phase efficiency multiplier</param>
    /// <returns>Stat growth amount (0 if at ceiling)</returns>
    double GrowStat(short actualStat, short dominantPotential, double careerMultiplier);

    /// <summary>
    /// Calculates performance-based growth multiplier from race finishing position.
    /// Winning horses learn more, back-of-pack horses learn less.
    /// Win: 1.50x, Place: 1.25x, Show: 1.10x, Mid-pack: 1.00x, Back of pack: 0.75x
    /// </summary>
    /// <param name="finishPosition">Final position in race (1 = first)</param>
    /// <param name="fieldSize">Total horses in race</param>
    /// <returns>Performance multiplier (0.75 to 1.50)</returns>
    double CalculatePerformanceMultiplier(byte finishPosition, byte fieldSize);

    /// <summary>
    /// Calculates stat-specific growth multipliers based on race distance type.
    /// Sprints develop Speed/Agility, distance races develop Stamina/Durability,
    /// classic races provide balanced growth.
    /// </summary>
    /// <param name="raceDistance">Race distance in furlongs</param>
    /// <returns>Multipliers for each stat based on race type</returns>
    RaceTypeFocusMultipliers CalculateRaceTypeFocusMultipliers(decimal raceDistance);
}
