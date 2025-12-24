using TripleDerby.Core.Entities;
using TripleDerby.Core.Services;

namespace TripleDerby.Core.Abstractions.Services;

/// <summary>
/// Generates natural language commentary for race events.
/// </summary>
public interface IRaceCommentaryGenerator
{
    /// <summary>
    /// Generates commentary text for a race tick based on detected events.
    /// </summary>
    /// <param name="events">Collection of events that occurred this tick</param>
    /// <param name="tick">Current tick number</param>
    /// <param name="raceRun">Current race state</param>
    /// <returns>Commentary text, or empty string if no events occurred</returns>
    string GenerateCommentary(TickEvents events, short tick, RaceRun raceRun);
}
