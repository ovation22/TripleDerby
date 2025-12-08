using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class HorseStatistic
{
    public Guid HorseId { get; set; }

    public virtual Horse Horse { get; set; } = default!;

    public byte Actual { get; set; }

    public byte DominantPotential { get; set; }

    public byte RecessivePotential { get; set; }

    public virtual StatisticId StatisticId { get; set; } = default!;
}
