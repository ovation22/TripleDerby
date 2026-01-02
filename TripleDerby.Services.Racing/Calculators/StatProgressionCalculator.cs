using TripleDerby.Services.Racing.Abstractions;
using TripleDerby.Services.Racing.Config;

namespace TripleDerby.Services.Racing.Calculators;

/// <summary>
/// Holds stat-specific growth multipliers for race-type focus.
/// </summary>
public record struct RaceTypeFocusMultipliers(
    double Speed,
    double Agility,
    double Stamina,
    double Durability);

/// <summary>
/// Calculates stat progression multipliers and growth for horses after races.
/// Implements career phase system, race-type focus, performance bonuses, and happiness changes.
/// Part of Feature 018: Race Outcome Stat Progression System.
/// </summary>
public class StatProgressionCalculator : IStatProgressionCalculator
{
    /// <summary>
    /// Calculates development efficiency multiplier based on career stage.
    /// Young horses (0-9 races) learn slower, prime horses (10-29) learn fastest,
    /// veterans (30-49) slow down, old horses (50+) show minimal development.
    /// </summary>
    /// <param name="raceStarts">Total number of races the horse has started</param>
    /// <returns>Development efficiency multiplier (0.20 to 1.20)</returns>
    public double CalculateAgeMultiplier(short raceStarts)
    {
        if (raceStarts < RaceModifierConfig.PrimeCareerStartRace)
            return RaceModifierConfig.YoungHorseMultiplier;      // 0.80

        if (raceStarts < RaceModifierConfig.VeteranCareerStartRace)
            return RaceModifierConfig.PrimeHorseMultiplier;      // 1.20

        if (raceStarts < RaceModifierConfig.OldCareerStartRace)
            return RaceModifierConfig.VeteranHorseMultiplier;    // 0.60

        return RaceModifierConfig.OldHorseMultiplier;            // 0.20
    }

    /// <summary>
    /// Calculates stat growth for a single stat based on the gap between actual and genetic potential.
    /// Uses the base formula: Growth = (DominantPotential - Actual) × BaseGrowthRate × CareerMultiplier
    /// Returns 0 if the horse has already reached its genetic ceiling.
    /// </summary>
    /// <param name="actualStat">Current stat value</param>
    /// <param name="dominantPotential">Genetic ceiling from breeding</param>
    /// <param name="careerMultiplier">Career phase efficiency multiplier</param>
    /// <returns>Stat growth amount (0 if at ceiling)</returns>
    public double GrowStat(short actualStat, short dominantPotential, double careerMultiplier)
    {
        // No growth if already at genetic ceiling
        if (actualStat >= dominantPotential)
            return 0;

        // Calculate gap-based growth: larger gaps = more growth per race
        var gap = dominantPotential - actualStat;
        var growth = gap * RaceModifierConfig.BaseStatGrowthRate * careerMultiplier;

        return growth;
    }

    /// <summary>
    /// Calculates performance-based growth multiplier from race finishing position.
    /// Winning horses learn more, back-of-pack horses learn less.
    /// Win: 1.50x, Place: 1.25x, Show: 1.10x, Mid-pack: 1.00x, Back of pack: 0.75x
    /// </summary>
    /// <param name="finishPosition">Final position in race (1 = first)</param>
    /// <param name="fieldSize">Total horses in race</param>
    /// <returns>Performance multiplier (0.75 to 1.50)</returns>
    public double CalculatePerformanceMultiplier(byte finishPosition, byte fieldSize)
    {
        // Win: 1.50x bonus
        if (finishPosition == 1)
            return RaceModifierConfig.WinBonus;

        // Place: 1.25x bonus
        if (finishPosition == 2)
            return RaceModifierConfig.PlaceBonus;

        // Show: 1.10x bonus
        if (finishPosition == 3)
            return RaceModifierConfig.ShowBonus;

        // Back of pack (7th or later): 0.75x penalty
        if (finishPosition >= 7)
            return RaceModifierConfig.BackOfPackPenalty;

        // Mid-pack (4th-6th): no modifier
        return RaceModifierConfig.MidPackMultiplier;
    }

    /// <summary>
    /// Calculates stat-specific growth multipliers based on race distance type.
    /// Sprints develop Speed/Agility, distance races develop Stamina/Durability,
    /// classic races provide balanced growth.
    /// </summary>
    /// <param name="raceDistance">Race distance in furlongs</param>
    /// <returns>Multipliers for each stat based on race type</returns>
    public RaceTypeFocusMultipliers CalculateRaceTypeFocusMultipliers(decimal raceDistance)
    {
        // Sprint races (≤6f): Emphasize Speed and Agility
        if (raceDistance <= RaceModifierConfig.SprintDistanceThreshold)
        {
            return new RaceTypeFocusMultipliers(
                Speed: RaceModifierConfig.SprintSpeedMultiplier,      // 1.50
                Agility: RaceModifierConfig.SprintAgilityMultiplier,  // 1.25
                Stamina: RaceModifierConfig.SprintOtherMultiplier,    // 0.75
                Durability: RaceModifierConfig.SprintOtherMultiplier  // 0.75
            );
        }

        // Distance races (≥11f): Emphasize Stamina and Durability
        if (raceDistance >= RaceModifierConfig.DistanceRaceThreshold)
        {
            return new RaceTypeFocusMultipliers(
                Speed: RaceModifierConfig.DistanceOtherMultiplier,         // 0.75
                Agility: RaceModifierConfig.DistanceOtherMultiplier,       // 0.75
                Stamina: RaceModifierConfig.DistanceStaminaMultiplier,     // 1.50
                Durability: RaceModifierConfig.DistanceDurabilityMultiplier // 1.25
            );
        }

        // Classic races (7-10f): Balanced growth
        return new RaceTypeFocusMultipliers(
            Speed: RaceModifierConfig.ClassicRaceMultiplier,      // 1.00
            Agility: RaceModifierConfig.ClassicRaceMultiplier,    // 1.00
            Stamina: RaceModifierConfig.ClassicRaceMultiplier,    // 1.00
            Durability: RaceModifierConfig.ClassicRaceMultiplier  // 1.00
        );
    }
}
