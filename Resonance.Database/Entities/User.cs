using System.ComponentModel.DataAnnotations;
using Resonance.Core.Models;

namespace Resonance.Database.Entities;

public sealed class User
{
    public int Id { get; set; }
    public UserPrivileges Privileges { get; set; }
    public UserBadges Badges { get; set; }
    public bool IsBot { get; set; }
    public bool IsSuperuser { get; set; }

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

    public UserBan Ban { get; set; }
    public DateTimeOffset? BannedAt { get; set; }

    [MaxLength(1000)]
    public string? BanReason { get; set; }

    public DateTimeOffset? SilencedAt { get; set; }
    public DateTimeOffset? SilenceExpiresAt { get; set; }

    [MaxLength(1000)]
    public string? SilenceReason { get; set; }
}
