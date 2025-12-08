using System.ComponentModel.DataAnnotations;

namespace TripleDerby.Core.Entities;

public class Color
{
    [Key]
    public byte Id { get; set; }

    public string Name { get; set; } = default!;

    public string Description { get; set; } = default!;

    public int Weight { get; set; }

    public bool IsSpecial { get; set; }
}
