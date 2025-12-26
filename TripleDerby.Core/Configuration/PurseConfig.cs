using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Configuration;

/// <summary>
/// Purse configuration for race prize money distribution.
/// Based on real-world championship stakes and premier event patterns.
/// Implements realistic purse depth and top-heavy distribution for elite races.
/// </summary>
public static class PurseConfig
{
    // ============================================================================
    // Base Purse Amounts (10 furlong baseline)
    // ============================================================================

    /// <summary>
    /// Base purse amounts by race class for 10 furlong baseline distance.
    /// Longer races scale up, shorter races scale down using DistanceScalingFactor.
    /// </summary>
    public static readonly IReadOnlyDictionary<RaceClassId, int> BasePurseByClass =
        new Dictionary<RaceClassId, int>
        {
            { RaceClassId.Maiden, 20000 },           // $20,000 for maiden races
            { RaceClassId.MaidenClaiming, 18000 },   // $18,000 for maiden claiming
            { RaceClassId.Claiming, 25000 },         // $25,000 for claiming
            { RaceClassId.Allowance, 40000 },        // $40,000 for allowance
            { RaceClassId.AllowanceOptional, 50000 },// $50,000 for allowance optional
            { RaceClassId.Stakes, 100000 },          // $100,000 for stakes
            { RaceClassId.GradeIII, 200000 },        // $200,000 for Grade III
            { RaceClassId.GradeII, 500000 },         // $500,000 for Grade II
            { RaceClassId.GradeI, 1000000 }          // $1,000,000 for Grade I
        };

    /// <summary>
    /// Distance scaling factor per furlong.
    /// Longer races have slightly higher purses.
    /// Formula: totalPurse = basePurse × (1 + (furlongs - 10) × DistanceScalingFactor)
    /// </summary>
    public const decimal DistanceScalingFactor = 0.05m;  // +5% per furlong above 10f

    // ============================================================================
    // Purse Distribution Patterns (Real-World Based)
    // ============================================================================

    /// <summary>
    /// Purse distribution structure defining paid places and percentages.
    /// </summary>
    public class PurseDistribution
    {
        public int PaidPlaces { get; set; }                // How many places receive purse money
        public decimal[] Percentages { get; set; } = [];   // Percentage for each place
        public string Description { get; set; } = string.Empty;  // Pattern description
    }

    /// <summary>
    /// Distribution patterns by race class.
    /// Lower-class races: more balanced distribution (competitive balance)
    /// Elite races: top-heavy distribution (winner-centric prestige)
    /// </summary>
    public static readonly IReadOnlyDictionary<RaceClassId, PurseDistribution> DistributionByClass =
        new Dictionary<RaceClassId, PurseDistribution>
        {
            // Lower-tier races: balanced distribution (pay top 3)
            { RaceClassId.Maiden, new PurseDistribution
                {
                    PaidPlaces = 3,
                    Percentages = [0.60m, 0.20m, 0.10m],
                    Description = "Standard Win/Place/Show"
                }
            },
            { RaceClassId.MaidenClaiming, new PurseDistribution
                {
                    PaidPlaces = 3,
                    Percentages = [0.60m, 0.20m, 0.10m],
                    Description = "Standard Win/Place/Show"
                }
            },
            { RaceClassId.Claiming, new PurseDistribution
                {
                    PaidPlaces = 3,
                    Percentages = [0.60m, 0.20m, 0.10m],
                    Description = "Standard Win/Place/Show"
                }
            },

            // Mid-tier races: slight expansion (pay top 4)
            { RaceClassId.Allowance, new PurseDistribution
                {
                    PaidPlaces = 4,
                    Percentages = [0.58m, 0.20m, 0.10m, 0.05m],
                    Description = "Competitive balance"
                }
            },
            { RaceClassId.AllowanceOptional, new PurseDistribution
                {
                    PaidPlaces = 4,
                    Percentages = [0.58m, 0.20m, 0.10m, 0.05m],
                    Description = "Competitive balance"
                }
            },

            // Stakes races: moderate top-heavy (pay top 4)
            { RaceClassId.Stakes, new PurseDistribution
                {
                    PaidPlaces = 4,
                    Percentages = [0.55m, 0.20m, 0.10m, 0.05m],
                    Description = "Winner-centric (typical G1 pattern)"
                }
            },

            // Grade III: Premier event pattern (pay top 4)
            { RaceClassId.GradeIII, new PurseDistribution
                {
                    PaidPlaces = 4,
                    Percentages = [0.55m, 0.20m, 0.10m, 0.05m],
                    Description = "Premier event pattern"
                }
            },

            // Grade II: Elite prestige (pay top 5)
            { RaceClassId.GradeII, new PurseDistribution
                {
                    PaidPlaces = 5,
                    Percentages = [0.55m, 0.20m, 0.10m, 0.07m, 0.03m],
                    Description = "Marathon stakes pattern"
                }
            },

            // Grade I: Championship pattern (extremely top-heavy, pay top 5)
            { RaceClassId.GradeI, new PurseDistribution
                {
                    PaidPlaces = 5,
                    Percentages = [0.62m, 0.20m, 0.10m, 0.05m, 0.03m],
                    Description = "Championship pattern (most punitive)"
                }
            }
        };

    // ============================================================================
    // Starter Stipend (Optional)
    // ============================================================================

    /// <summary>
    /// Optional flat stipend for horses finishing outside the money.
    /// Does NOT count toward horse Earnings stat (cash-flow only for owner).
    /// Set to 0 to disable. Suggested: $1,000-$2,000 for realism.
    /// </summary>
    public const int StarterStipend = 1000;  // $1,000 per starter (optional)

    /// <summary>
    /// Whether starter stipend is enabled.
    /// </summary>
    public const bool StarterStipendEnabled = false;  // Default: disabled
}
