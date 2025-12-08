using System.ComponentModel.DataAnnotations;

namespace TripleDerby.Core.Entities;

public class Feeding
{
    [Key]
    public byte Id { get; set; }

    public string Name { get; set; } = default!;

    public string Description { get; set; } = default!;
}
