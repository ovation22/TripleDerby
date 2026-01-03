using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class HorseStatistic
{
    public Guid HorseId { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public double Actual { get; set; }

    public double DominantPotential { get; set; }

    public double RecessivePotential { get; set; }

    public virtual StatisticId StatisticId { get; set; } = default!;

    public double Speed
    {
        get => StatisticId == StatisticId.Speed ? Actual : 0;
        set { if (StatisticId == StatisticId.Speed) Actual = value; }
    }

    public double Stamina
    {
        get => StatisticId == StatisticId.Stamina ? Actual : 0;
        set { if (StatisticId == StatisticId.Stamina) Actual = value; }
    }

    public double Agility
    {
        get => StatisticId == StatisticId.Agility ? Actual : 0;
        set { if (StatisticId == StatisticId.Agility) Actual = value; }
    }

    public double Durability
    {
        get => StatisticId == StatisticId.Durability ? Actual : 0;
        set { if (StatisticId == StatisticId.Durability) Actual = value; }
    }

    public double Happiness
    {
        get => StatisticId == StatisticId.Happiness ? Actual : 0;
        set { if (StatisticId == StatisticId.Happiness) Actual = value; }
    }
}
