using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

/// <summary>
/// Lookup table for feeding categories.
/// Categories determine preference probability weights and group feeds by effect profile.
/// </summary>
public class FeedingCategory
{
    [Key]
    public FeedingCategoryId Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;
}
