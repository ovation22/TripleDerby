using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class LegType
{
    [Key]
    public LegTypeId Id { get; set; }

    public string Name { get; set; } = default!;

    public string Description { get; set; } = default!;

    public byte Weight { get; set; }
}
