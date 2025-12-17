using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class Horse
{
    [Key]
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public byte ColorId { get; set; }

    public virtual Color Color { get; set; } = null!;

    public LegTypeId LegTypeId { get; set; }
    
    public bool IsMale { get; set; }
    
    public Guid? SireId { get; set; }
    
    public Guid? DamId { get; set; }
    
    public short RaceStarts { get; set; }
    
    public short RaceWins { get; set; }
    
    public short RacePlace { get; set; }
    
    public short RaceShow { get; set; }
    
    public int Earnings { get; set; }
    
    public bool IsRetired { get; set; }
    
    public int Parented { get; set; }
    
    public Guid OwnerId { get; set; }

    public DateTimeOffset CreatedDate { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset? UpdatedDate { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual User Owner { get; set; } = null!;

    public virtual Horse? Sire { get; set; }

    public virtual Horse? Dam { get; set; }

    public virtual ICollection<Horse> Foals { get; set; } = new Collection<Horse>();

    public virtual ICollection<HorseStatistic> Statistics { get; set; } = new Collection<HorseStatistic>();
    
    public virtual ICollection<FeedingSession> FeedingSessions { get; set; } = new Collection<FeedingSession>();

    public byte Speed
    {
        get => Statistics?.FirstOrDefault()?.Speed ?? 0;
        set
        {
            var stat = Statistics?.FirstOrDefault();
            if (stat != null)
            {
                stat.Speed = value;
            }
        }
    }

    public byte Stamina
    {
        get => Statistics?.FirstOrDefault()?.Stamina ?? 0;
        set
        {
            var stat = Statistics?.FirstOrDefault();
            if (stat != null)
            {
                stat.Stamina = value;
            }
        }
    }

    public byte Agility
    {
        get => Statistics?.FirstOrDefault()?.Agility ?? 0;
        set
        {
            var stat = Statistics?.FirstOrDefault();
            if (stat != null)
            {
                stat.Agility = value;
            }
        }
    }

    public byte Durability
    {
        get => Statistics?.FirstOrDefault()?.Durability ?? 0;
        set
        {
            var stat = Statistics?.FirstOrDefault();
            if (stat != null)
            {
                stat.Durability = value;
            }
        }
    }

    public byte Happiness
    {
        get => Statistics?.FirstOrDefault()?.Happiness ?? 0;
        set
        {
            var stat = Statistics?.FirstOrDefault();
            if (stat != null)
            {
                stat.Happiness = value;
            }
        }
    }
}
