using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class RaceClass
{
    [Key]
    public RaceClassId Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;
}
