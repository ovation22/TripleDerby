using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class LegType
{
    [Key]
    public LegTypeId Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public byte Weight { get; set; }
}
