namespace TripleDerby.Core.Configuration;

/// <summary>
/// Configuration for race commentary generation including synonym pools and templates.
/// Provides variety in language to avoid repetitive race narration.
/// </summary>
public static class CommentaryConfig
{
    // ============================================================================
    // Synonym Pools - Action Verbs
    // ============================================================================

    /// <summary>
    /// Verbs for horses accelerating or moving forward aggressively.
    /// Used in position changes and surges.
    /// </summary>
    public static readonly string[] SurgeVerbs =
    [
        "surges", "charges", "accelerates", "pushes", "advances",
        "drives", "powers", "rockets", "bolts", "flies", "rushes"
    ];

    /// <summary>
    /// Verbs for horses overtaking or passing other horses.
    /// Used in position change commentary.
    /// </summary>
    public static readonly string[] PassVerbs =
    [
        "passes", "overtakes", "moves past", "goes by",
        "slips past", "edges past", "gets around", "sweeps by"
    ];

    /// <summary>
    /// Verbs for clean lane changes (non-risky).
    /// Used when horses move to adjacent lanes without traffic.
    /// </summary>
    public static readonly string[] LaneChangeVerbs =
    [
        "moves", "shifts", "drifts", "cuts", "slides", "swings", "settles"
    ];

    /// <summary>
    /// Verbs for risky squeeze plays through traffic.
    /// More dramatic language for successful risky lane changes.
    /// </summary>
    public static readonly string[] RiskySqueezeVerbs =
    [
        "threads through", "squeezes between", "darts through",
        "slips through traffic", "finds a seam", "navigates through",
        "weaves through", "splits horses"
    ];

    // ============================================================================
    // Synonym Pools - Positioning Phrases
    // ============================================================================

    /// <summary>
    /// Phrases for taking the lead position.
    /// Used in lead change commentary.
    /// </summary>
    public static readonly string[] LeadPhrases =
    [
        "takes the lead", "assumes command", "seizes control",
        "moves to the front", "takes over", "grabs the lead",
        "takes command", "moves ahead"
    ];

    /// <summary>
    /// Verbs for crossing the finish line.
    /// Used in finish commentary.
    /// </summary>
    public static readonly string[] FinishVerbs =
    [
        "crosses the line", "finishes", "completes the race",
        "hits the wire", "reaches the finish", "crosses"
    ];

    /// <summary>
    /// Intro phrases for final stretch entry.
    /// </summary>
    public static readonly string[] FinalStretchIntros =
    [
        "Into the final stretch!", "Entering the homestretch!",
        "Here comes the stretch run!", "Into the stretch!",
        "Down the homestretch!"
    ];

    // ============================================================================
    // Commentary Templates
    // ============================================================================

    /// <summary>
    /// Templates for clean lane changes.
    /// Placeholders: {horse}, {laneVerb}, {lane}, {oldLane}, {newLane}
    /// </summary>
    public static readonly string[] LaneChangeTemplates =
    [
        "{horse} {laneVerb} to lane {lane}",
        "{horse} {laneVerb} from lane {oldLane} to {newLane}",
        "Lane change: {horse} to {lane}"
    ];

    /// <summary>
    /// Templates for risky squeeze plays.
    /// Placeholders: {horse}, {squeezeVerb}, {lane}
    /// </summary>
    public static readonly string[] RiskySqueezeTemplates =
    [
        "{horse} {squeezeVerb} into lane {lane}!",
        "Risky move! {horse} {squeezeVerb}",
        "{horse} makes a daring squeeze to lane {lane}",
        "{horse} {squeezeVerb} to lane {lane}!"
    ];

    /// <summary>
    /// Templates for lead changes.
    /// Placeholders: {newLeader}, {oldLeader}, {leadPhrase}
    /// </summary>
    public static readonly string[] LeadChangeTemplates =
    [
        "{newLeader} {leadPhrase} from {oldLeader}!",
        "{newLeader} {leadPhrase}!",
        "Lead change! {newLeader} {leadPhrase}"
    ];

    /// <summary>
    /// Templates for horse finishes.
    /// Placeholders: {horse}, {finishVerb}, {place}
    /// </summary>
    public static readonly string[] FinishTemplates =
    [
        "{horse} {finishVerb} in {place} place",
        "{horse} {finishVerb} {place}",
        "{place} place: {horse}"
    ];

    // ============================================================================
    // Event Detection Thresholds
    // ============================================================================

    /// <summary>
    /// Time margin (in ticks) to consider a finish a "photo finish".
    /// Default: 0.5 ticks between 1st and 2nd place.
    /// </summary>
    public const double PhotoFinishMargin = 0.5;

    /// <summary>
    /// Cooldown window (in ticks) before a horse can have another position change reported.
    /// Prevents repetitive back-and-forth position swap commentary.
    /// Default: 10 ticks.
    /// </summary>
    public const short PositionChangeCooldown = 10;
}
