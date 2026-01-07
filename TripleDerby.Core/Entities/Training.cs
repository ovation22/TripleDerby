using System.ComponentModel.DataAnnotations;

namespace TripleDerby.Core.Entities;

public class Training
{
    [Key]
    public byte Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public double SpeedModifier { get; set; }

    public double StaminaModifier { get; set; }

    public double AgilityModifier { get; set; }

    public double DurabilityModifier { get; set; }

    public double HappinessCost { get; set; }

    public double OverworkRisk { get; set; }

    public bool IsRecovery { get; set; }
}
