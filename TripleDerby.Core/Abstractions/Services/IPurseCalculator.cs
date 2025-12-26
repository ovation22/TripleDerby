using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Abstractions.Services;

/// <summary>
/// Service for calculating race purse amounts and distributions.
/// </summary>
public interface IPurseCalculator
{
    /// <summary>
    /// Calculates total purse for a race based on class and distance.
    /// </summary>
    /// <param name="raceClass">Race class ID</param>
    /// <param name="furlongs">Race distance in furlongs</param>
    /// <returns>Total purse amount</returns>
    int CalculateTotalPurse(RaceClassId raceClass, decimal furlongs);

    /// <summary>
    /// Calculates payout for a specific finishing position.
    /// </summary>
    /// <param name="raceClass">Race class ID (determines distribution pattern)</param>
    /// <param name="totalPurse">Total race purse</param>
    /// <param name="place">Finishing position (1=Win, 2=Place, 3=Show, etc.)</param>
    /// <returns>Payout amount for this position (0 if outside the money)</returns>
    int CalculatePayout(RaceClassId raceClass, int totalPurse, int place);

    /// <summary>
    /// Calculates all payouts for a race.
    /// </summary>
    /// <param name="raceClass">Race class ID (determines distribution pattern)</param>
    /// <param name="totalPurse">Total race purse</param>
    /// <returns>Dictionary of place → payout amount</returns>
    Dictionary<int, int> CalculateAllPayouts(RaceClassId raceClass, int totalPurse);
}
