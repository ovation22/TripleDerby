using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class Statistic
{
    [Key]
    public StatisticId Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsGenetic { get; set; }
}
