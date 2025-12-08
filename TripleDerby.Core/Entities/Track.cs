using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

public class Track
{
    [Key]
    public TrackId Id { get; set; }

    public string Name { get; set; } = default!;
}
