using TripleDerby.Core.Entities;

namespace TripleDerby.Services.Racing.Abstractions;

/// <summary>
/// Manages overtaking detection, lane changes, and traffic response during race simulation.
/// </summary>
public interface IOvertakingManager
{
    /// <summary>
    /// Handles overtaking detection and lane change logic for a horse.
    /// Called once per tick per horse during race simulation.
    /// </summary>
    /// <param name="horse">The horse being updated</param>
    /// <param name="raceRun">Current race state</param>
    /// <param name="currentTick">Current race tick</param>
    /// <param name="totalTicks">Total ticks in race</param>
    void HandleOvertaking(RaceRunHorse horse, RaceRun raceRun, short currentTick, short totalTicks);

    /// <summary>
    /// Applies leg-type-specific traffic response effects when horse is blocked.
    /// Modifies speed based on traffic ahead and horse's personality.
    /// Uses actual horse speed calculation for realistic traffic dynamics.
    /// </summary>
    /// <param name="horse">The horse being affected</param>
    /// <param name="raceRun">Current race state</param>
    /// <param name="currentTick">Current race tick</param>
    /// <param name="totalTicks">Total ticks in race</param>
    /// <param name="currentSpeed">Current speed to modify (passed by reference)</param>
    void ApplyTrafficEffects(
        RaceRunHorse horse,
        RaceRun raceRun,
        short currentTick,
        short totalTicks,
        ref double currentSpeed);
}
