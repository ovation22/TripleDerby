namespace TripleDerby.Core.Configuration;

/// <summary>
/// Configuration for race commentary generation including synonym pools and templates.
/// Provides variety in language to avoid repetitive race narration.
/// </summary>
public static class CommentaryConfig
{
    // Synonym Pools - Action Verbs

    /// <summary>
    /// Verbs for horses accelerating or moving forward aggressively.
    /// Used in position changes and surges.
    /// </summary>
    public static readonly string[] SurgeVerbs =
    [
        "surges", "charges", "accelerates", "pushes", "advances",
        "drives", "powers", "rockets", "bolts", "flies", "rushes",
        "quickens", "picks up the pace", "steps it up",
        "presses on", "builds momentum", "lengthens stride",
        "starts to roll", "gathers speed", "ups the tempo",
        "moves up"
    ];

    /// <summary>
    /// Verbs for horses overtaking or passing other horses.
    /// Used in position change commentary.
    /// </summary>
    public static readonly string[] PassVerbs =
    [
        "passes", "overtakes", "moves past", "goes by",
        "slips past", "edges past", "gets around", "sweeps by",
        "edges ahead of", "draws alongside", "takes over from",
        "moves in front of", "claims position on",
        "reaches the shoulder of", "pulls ahead of",
        "goes on by",
        "works by",
        "gets the better of"
    ];

    /// <summary>
    /// Verbs for clean lane changes (non-risky).
    /// Used when horses move to adjacent lanes without traffic.
    /// </summary>
    public static readonly string[] LaneChangeVerbs =
    [
        "moves", "shifts", "drifts", "cuts", "slides", "swings", "settles",
        "angles out", "eases over", "guides inside",
        "works outward", "tucks in", "drops down",
        "switches"
    ];

    /// <summary>
    /// Verbs for risky squeeze plays through traffic.
    /// More dramatic language for successful risky lane changes.
    /// </summary>
    public static readonly string[] RiskySqueezeVerbs =
    [
        "threads through", "squeezes by", "darts through",
        "slips through traffic", "finds a seam", "navigates through",
        "weaves through", "splits horses",
        "squeezes through at the last moment",
        "forces a narrow opening",
        "barely finds room",
        "gets through by inches",
        "scrapes through traffic",
        "bulls through",
        "muscles between rivals",
        "powers through the gap"
    ];

    // Synonym Pools - Positioning Phrases

    /// <summary>
    /// Phrases for taking the lead position.
    /// Used in lead change commentary.
    /// </summary>
    public static readonly string[] LeadPhrases =
    [
        "takes the lead", "assumes command", "seizes control",
        "moves to the front", "takes over", "grabs the lead",
        "takes command", "moves ahead",
        "takes a narrow lead",
        "moves into a clear lead",
        "edges in front",
        "reclaims the lead",
        "opens up in front",
        "briefly takes command"
    ];

    /// <summary>
    /// Verbs for crossing the finish line.
    /// Used in finish commentary.
    /// </summary>
    public static readonly string[] FinishVerbs =
    [
        "crosses the line in", 
        "finishes in", 
        "completes the race in",
        "hits the wire in", 
        "reaches the finish in", 
        "crosses in",
        "storms across the line in",
        "runs it out in",
        "comes home in",
        "finishes powerfully in",
        "gets there in",
        "now in",
        "for",
        "to grab",
        "up to",
        "claiming",
        "to claim"
    ];

    /// <summary>
    /// Intro phrases for final stretch entry.
    /// </summary>
    public static readonly string[] FinalStretchIntros =
    [
        "Into the final stretch!", "Entering the homestretch!",
        "Here comes the stretch run!", "Into the stretch!",
        "Down the homestretch!",
        "They straighten away!",
        "Now they turn for home!",
        "Into the drive to the wire!",
        "The stretch duel is on!",
        "Here comes the run to the finish!"
    ];

    // Commentary Templates

    /// <summary>
    /// Templates for clean lane changes.
    /// Placeholders: {horse}, {laneVerb}, {lane}, {oldLane}, {newLane}
    /// </summary>
    public static readonly string[] LaneChangeTemplates =
    [
        "{horse} {laneVerb} to lane {lane}",
        "{horse} {laneVerb} from lane {oldLane} to {newLane}",
        "{horse} switches to lane {lane}",
        "{horse} moves lanes, now in lane {lane}",
        "{horse} repositions into lane {lane}"
    ];

    /// <summary>
    /// Templates for risky squeeze plays.
    /// Placeholders: {horse}, {squeezeVerb}, {lane}
    /// </summary>
    public static readonly string[] RiskySqueezeTemplates =
    [
        "{horse} {squeezeVerb} into lane {lane}!",
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
        "{horse} {finishVerb} {place} place",
        "{horse} {finishVerb} {place}",
        "{place} place: {horse}"
    ];

    // Event Detection Thresholds

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

    /// <summary>
    /// Cooldown window (in ticks) before a horse can have another lane change reported.
    /// Prevents repetitive consecutive lane change commentary that creates narrative clusters.
    /// Exception: Risky squeeze plays are always reported regardless of cooldown.
    /// Default: 10 ticks.
    /// </summary>
    public const short LaneChangeCooldown = 10;
}
