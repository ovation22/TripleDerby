using System.ComponentModel.DataAnnotations;
using TripleDerby.SharedKernel.Enums;

namespace TripleDerby.Core.Entities;

/// <summary>
/// Represents a type of feed that can be given to horses.
/// Each feed has a category, happiness effect range, and stat effect ranges.
/// </summary>
public class Feeding
{
    [Key]
    public byte Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    /// <summary>
    /// Category that determines preference probability weights.
    /// </summary>
    public FeedingCategoryId CategoryId { get; set; }

    /// <summary>
    /// Navigation property to the category lookup table.
    /// </summary>
    public virtual FeedingCategory Category { get; set; } = null!;

    /// <summary>
    /// Minimum happiness gain (rolled randomly each feeding).
    /// </summary>
    public double HappinessMin { get; set; }

    /// <summary>
    /// Maximum happiness gain (rolled randomly each feeding).
    /// </summary>
    public double HappinessMax { get; set; }

    /// <summary>
    /// Minimum stamina gain (rolled randomly each feeding).
    /// </summary>
    public double StaminaMin { get; set; }

    /// <summary>
    /// Maximum stamina gain (rolled randomly each feeding).
    /// </summary>
    public double StaminaMax { get; set; }

    /// <summary>
    /// Minimum durability gain (rolled randomly each feeding).
    /// </summary>
    public double DurabilityMin { get; set; }

    /// <summary>
    /// Maximum durability gain (rolled randomly each feeding).
    /// </summary>
    public double DurabilityMax { get; set; }

    /// <summary>
    /// Minimum speed gain (rolled randomly each feeding).
    /// </summary>
    public double SpeedMin { get; set; }

    /// <summary>
    /// Maximum speed gain (rolled randomly each feeding).
    /// </summary>
    public double SpeedMax { get; set; }

    /// <summary>
    /// Minimum agility gain (rolled randomly each feeding).
    /// </summary>
    public double AgilityMin { get; set; }

    /// <summary>
    /// Maximum agility gain (rolled randomly each feeding).
    /// </summary>
    public double AgilityMax { get; set; }
}
