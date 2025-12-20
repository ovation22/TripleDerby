using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.SharedKernel;

public record RaceRunResult
{
    public Guid RaceRunId { get; init; }

    public byte RaceId { get; init; }

    public string RaceName { get; set; } = null!;

    public TrackId TrackId { get; set; }

    public string TrackName { get; set; } = null!;

    public ConditionId ConditionId { get; set; } = default!;

    public string ConditionName { get; set; } = null!;

    public SurfaceId SurfaceId { get; set; }

    public string SurfaceName { get; set; } = null!;

    public decimal Furlongs { get; set; }

    public List<RaceRunHorseResult> HorseResults { get; set; } = null!;

    public List<string> PlayByPlay { get; init; } = null!;
}

public record RaceRunHorseResult
{
    public Guid HorseId { get; set; } = Guid.Empty!;

    public string HorseName { get; set; } = null!;

    public byte Place { get; set; }

    public decimal Payout { get; set; }

    public double Time { get; set; }

    public string DisplayTime => TimeSpan.FromSeconds(Time * 0.50633).ToString(@"m\:ss\.ff");
}