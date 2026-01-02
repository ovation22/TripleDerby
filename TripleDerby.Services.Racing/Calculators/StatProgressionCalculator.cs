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
}
