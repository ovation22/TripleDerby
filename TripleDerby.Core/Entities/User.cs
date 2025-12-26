using System.ComponentModel.DataAnnotations;

namespace TripleDerby.Core.Entities;

public class User
{
    [Key]
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsAdmin { get; set; }

    public int Balance { get; set; }
}
