using System;

namespace TripleDerby.SharedKernel;

public record UserResult
{
    public Guid Id { get; init; }

    public string Username { get; init; } = default!;

    public string Email { get; init; } = default!;

    public bool IsActive { get; init; }

    public bool IsAdmin { get; init; }
}
