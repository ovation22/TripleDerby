using System.ComponentModel.DataAnnotations;

namespace TripleDerby.Core.Entities;

public class TrainingSession
{
    [Key]
    public Guid Id { get; set; }
    
    public byte TrainingId { get; set; }

    public virtual Training Training { get; set; } = default!;

    public Guid HorseId { get; set; }

    public virtual Horse Horse { get; set; } = default!;

    public string Result { get; set; } = default!;
}
