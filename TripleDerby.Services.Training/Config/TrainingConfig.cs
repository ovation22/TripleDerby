using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Training.Config;

/// <summary>
/// Configuration constants for the horse training system
/// </summary>
public static class TrainingConfig
{
    // Base Training Constants
    public const double BaseTrainingGrowthRate = 0.015;
    public const double MinimumHappinessToTrain = 15.0;

    // Overwork Constants
    public const double OverworkHappinessPenalty = 5.0;

    // Career Phase Multipliers (different from racing multipliers)
    public const double YoungHorseTrainingMultiplier = 1.20;     // 0-19 races: Young horses learn better
    public const double PrimeHorseTrainingMultiplier = 1.40;     // 20-59 races: Peak training response
    public const double VeteranHorseTrainingMultiplier = 0.80;   // 60-99 races: Declining training response
    public const double OldHorseTrainingMultiplier = 0.40;       // 100+ races: Minimal training gains

    // LegType Bonus
    public const double LegTypeBonusMultiplier = 1.20;

    /// <summary>
    /// Maps each LegType to its preferred training type (bonus training)
    /// </summary>
    public static readonly Dictionary<LegTypeId, byte> LegTypePreferredTraining = new()
    {
        { LegTypeId.StartDash, 1 },      // Sprint Drills (Speed)
        { LegTypeId.FrontRunner, 6 },    // Interval Training (Balanced)
        { LegTypeId.StretchRunner, 2 },  // Distance Gallops (Stamina)
        { LegTypeId.LastSpurt, 5 },      // Hill Climbing (Power/Endurance)
        { LegTypeId.RailRunner, 3 }      // Agility Course (Agility)
    };
}
