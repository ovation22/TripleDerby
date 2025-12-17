using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class HorseStatistic
{
    public Guid HorseId { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public byte Actual { get; set; }

    public byte DominantPotential { get; set; }

    public byte RecessivePotential { get; set; }

    public virtual StatisticId StatisticId { get; set; } = default!;

    public byte Speed
    {
        get => (byte)(StatisticId == StatisticId.Speed ? Actual : 0);
        set { if (StatisticId == StatisticId.Speed) Actual = value; }
    }

    public byte Stamina
    {
        get => (byte)(StatisticId == StatisticId.Stamina ? Actual : 0);
        set { if (StatisticId == StatisticId.Stamina) Actual = value; }
    }

    public byte Agility
    {
        get => (byte)(StatisticId == StatisticId.Agility ? Actual : 0);
        set { if (StatisticId == StatisticId.Agility) Actual = value; }
    }

    public byte Durability
    {
        get => (byte)(StatisticId == StatisticId.Durability ? Actual : 0);
        set { if (StatisticId == StatisticId.Durability) Actual = value; }
    }

    public byte Happiness
    {
        get => (byte)(StatisticId == StatisticId.Happiness ? Actual : 0);
        set { if (StatisticId == StatisticId.Happiness) Actual = value; }
    }
}
