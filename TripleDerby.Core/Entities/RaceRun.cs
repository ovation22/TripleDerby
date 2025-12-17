using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class RaceRun
{
    [Key]
    public Guid Id { get; set; }

    public byte RaceId { get; set; }

    public virtual Race Race { get; set; } = null!;

    public ConditionId ConditionId { get; set; }

    public Guid? WinHorseId { get; set; }

    public virtual Horse WinHorse { get; set; } = null!;

    public Guid? PlaceHorseId { get; set; }

    public virtual Horse PlaceHorse { get; set; } = null!;

    public Guid? ShowHorseId { get; set; }

    public virtual Horse ShowHorse { get; set; } = null!;

    public int Purse { get; set; }

    public virtual ICollection<RaceRunHorse> Horses { get; set; } = null!;

    public virtual ICollection<RaceRunTick> RaceRunTicks { get; set; } = null!;
}
