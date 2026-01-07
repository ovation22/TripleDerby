using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Services.Training.Config;

/// <summary>
/// Configuration constants for the horse training system
/// Feature 020: Horse Training System
/// </summary>
public static class TrainingConfig
{
    // Base Training Constants
    public const double BaseTrainingGrowthRate = 0.015;
    public const double MinimumHappinessToTrain = 15.0;
    public const double HappinessWarningThreshold = 40.0;

    // Overwork Constants
    public const double OverworkHappinessPenalty = 5.0;
    public const double OverworkGainReduction = 0.50;

    // Career Phase Multipliers (different from racing multipliers)
    public const double YoungHorseTrainingMultiplier = 1.20;     // 0-19 races: Young horses learn better
    public const double PrimeHorseTrainingMultiplier = 1.40;     // 20-59 races: Peak training response
    public const double VeteranHorseTrainingMultiplier = 0.80;   // 60-99 races: Declining training response
    public const double OldHorseTrainingMultiplier = 0.40;       // 100+ races: Minimal training gains

    // Training Options
    public const int TrainingOptionsOffered = 3;

    // LegType Bonus
    public const double LegTypeBonusMultiplier = 1.20;

    // Redis Cache Configuration
    public const int TrainingOptionsCacheDurationMinutes = 30;

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

    /// <summary>
    /// Gets the career phase multiplier based on race starts
    /// </summary>
    public static double GetCareerPhaseMultiplier(short raceStarts)
    {
        return raceStarts switch
        {
            < 20 => YoungHorseTrainingMultiplier,
            < 60 => PrimeHorseTrainingMultiplier,
            < 100 => VeteranHorseTrainingMultiplier,
            _ => OldHorseTrainingMultiplier
        };
    }

    /// <summary>
    /// Checks if a horse has sufficient happiness to train without high overwork risk
    /// </summary>
    public static bool HasSufficientHappiness(double happiness)
    {
        return happiness >= MinimumHappinessToTrain;
    }

    /// <summary>
    /// Checks if a horse's happiness is in the warning zone (can train but risky)
    /// </summary>
    public static bool IsInWarningZone(double happiness)
    {
        return happiness >= MinimumHappinessToTrain && happiness < HappinessWarningThreshold;
    }

    /// <summary>
    /// Gets the Redis cache key for training options
    /// </summary>
    public static string GetTrainingOptionsCacheKey(Guid horseId, Guid sessionId)
    {
        return $"training:options:{horseId}:{sessionId}";
    }
}
