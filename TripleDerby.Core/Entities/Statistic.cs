using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class Statistic
{
    [Key]
    public StatisticId Id { get; set; }

    public string Name { get; set; } = default!;

    public string Description { get; set; } = default!;

    public bool IsGenetic { get; set; }
}
