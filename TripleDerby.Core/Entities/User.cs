using System.ComponentModel.DataAnnotations;

namespace TripleDerby.Core.Entities;

public class User
{
    [Key]
    public Guid Id { get; set; }

    public string Username { get; set; } = default!;

    public string Email { get; set; } = default!;

    public bool IsActive { get; set; }

    public bool IsAdmin { get; set; }
}
