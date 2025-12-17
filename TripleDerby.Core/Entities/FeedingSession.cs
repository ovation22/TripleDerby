using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class FeedingSession
{
    [Key]
    public Guid Id { get; set; }
    
    public byte FeedingId { get; set; }

    public virtual Feeding Feeding { get; set; } = null!;

    public Guid HorseId { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public FeedResponse Result { get; set; }
}
