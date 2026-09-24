using System.ComponentModel.DataAnnotations;

namespace Resonance.Data.Entities;

public sealed class Session
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(64)]
    public required string TokenHash { get; set; }

    [MaxLength(8)]
    public required string Client { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
