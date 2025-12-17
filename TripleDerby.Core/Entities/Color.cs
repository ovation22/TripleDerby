using System.ComponentModel.DataAnnotations;

namespace TripleDerby.Core.Entities;

public class Color
{
    [Key]
    public byte Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int Weight { get; set; }

    public bool IsSpecial { get; set; }
}
