using System.ComponentModel.DataAnnotations;
using Resonance.Models;

namespace Resonance.Data.Entities;

public sealed class User
{
    public int Id { get; set; }

    [MaxLength(32)]
    public required string Username { get; set; }

    [MaxLength(512)]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastOnlineAt { get; set; }

    [MaxLength(10000)]
    public string AboutMe { get; set; } = string.Empty;

    [MaxLength(2)]
    public string? CountryCode { get; set; }

    public int Rank { get; set; }
    public double RhythmPoints { get; set; }
    public int PlayCount { get; set; }
    public int SquaresHit { get; set; }
    public long TotalScore { get; set; }

    public UserStatus Status { get; set; }
    public DateTimeOffset? StatusExpiresAt { get; set; }
}
