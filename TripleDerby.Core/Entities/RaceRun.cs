using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class RaceRun
{
    [Key]
    public Guid Id { get; set; }

    public byte RaceId { get; set; }

    public virtual Race Race { get; set; } = default!;

    public ConditionId ConditionId { get; set; }

    public Guid? WinHorseId { get; set; }

    public virtual Horse WinHorse { get; set; } = default!;

    public Guid? PlaceHorseId { get; set; }

    public virtual Horse PlaceHorse { get; set; } = default!;

    public Guid? ShowHorseId { get; set; }

    public virtual Horse ShowHorse { get; set; } = default!;

    public int Purse { get; set; }

    public virtual ICollection<RaceRunHorse> Horses { get; set; } = default!;

    public virtual ICollection<RaceRunTick> RaceRunTicks { get; set; } = default!;
}
