using TripleDerby.Services.Racing.Config;

namespace TripleDerby.Services.Racing.Calculators;

/// <summary>
/// Calculates stat progression multipliers and growth for horses after races.
/// Implements career phase system, race-type focus, performance bonuses, and happiness changes.
/// Part of Feature 018: Race Outcome Stat Progression System.
/// </summary>
public class StatProgressionCalculator
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
}
