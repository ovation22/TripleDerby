using TripleDerby.Core.Configuration;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Racing;

/// <summary>
/// Calculates race purse amounts and distributions based on race class and distance.
/// </summary>
public class PurseCalculator : IPurseCalculator
{
    /// <summary>
    /// Calculates total purse for a race based on class and distance.
    /// Uses base purse for race class scaled by distance (10 furlongs baseline).
    /// </summary>
    /// <param name="raceClass">Race class ID</param>
    /// <param name="furlongs">Race distance in furlongs</param>
    /// <returns>Total purse amount</returns>
    public int CalculateTotalPurse(RaceClassId raceClass, decimal furlongs)
    {
        // Get base purse for this class
        if (!PurseConfig.BasePurseByClass.TryGetValue(raceClass, out var basePurse))
        {
            // Default fallback to Claiming if unknown class
            basePurse = PurseConfig.BasePurseByClass[RaceClassId.Claiming];
        }

        // Scale by distance (baseline is 10 furlongs)
        var distanceMultiplier = 1.0m + ((furlongs - 10m) * PurseConfig.DistanceScalingFactor);

        // Ensure non-negative multiplier (minimum 50% for very short races)
        distanceMultiplier = Math.Max(0.5m, distanceMultiplier);

        var totalPurse = (int)(basePurse * distanceMultiplier);

        return totalPurse;
    }

    /// <summary>
    /// Calculates payout for a specific finishing position.
    /// Uses race class to determine distribution pattern and paid places.
    /// </summary>
    /// <param name="raceClass">Race class ID (determines distribution pattern)</param>
    /// <param name="totalPurse">Total race purse</param>
    /// <param name="place">Finishing position (1=Win, 2=Place, 3=Show, etc.)</param>
    /// <returns>Payout amount for this position (0 if outside the money)</returns>
    public int CalculatePayout(RaceClassId raceClass, int totalPurse, int place)
    {
        // Get distribution pattern for this race class
        if (!PurseConfig.DistributionByClass.TryGetValue(raceClass, out var distribution))
        {
            // Default to standard Win/Place/Show if unknown class
            distribution = PurseConfig.DistributionByClass[RaceClassId.Claiming];
        }

        // Check if this place gets paid
        if (place < 1 || place > distribution.PaidPlaces)
        {
            return 0;  // Outside the money
        }

        // Get percentage for this place (1-indexed)
        var percentage = distribution.Percentages[place - 1];

        return (int)(totalPurse * percentage);
    }

    /// <summary>
    /// Calculates all payouts for a race.
    /// Returns dictionary mapping each paid position to its payout amount.
    /// </summary>
    /// <param name="raceClass">Race class ID (determines distribution pattern)</param>
    /// <param name="totalPurse">Total race purse</param>
    /// <returns>Dictionary of place → payout amount</returns>
    public Dictionary<int, int> CalculateAllPayouts(RaceClassId raceClass, int totalPurse)
    {
        // Get distribution pattern for this race class
        if (!PurseConfig.DistributionByClass.TryGetValue(raceClass, out var distribution))
        {
            // Default to standard Win/Place/Show if unknown class
            distribution = PurseConfig.DistributionByClass[RaceClassId.Claiming];
        }

        var payouts = new Dictionary<int, int>();

        for (int place = 1; place <= distribution.PaidPlaces; place++)
        {
            payouts[place] = CalculatePayout(raceClass, totalPurse, place);
        }

        return payouts;
    }
}
