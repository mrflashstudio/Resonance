namespace Resonance.Core.Models;

public sealed record Account(int Id, string Name)
{
    public UserPrivileges Privileges { get; init; }
    public UserBadges Badges { get; init; }
    public bool IsBot { get; init; }
    public bool IsSuperuser { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastOnlineAt { get; init; }
    public bool IsOnline { get; init; }
    public string AboutMe { get; init; } = string.Empty;
    public string? CountryCode { get; init; }
    public int Rank { get; init; }
    public double RhythmPoints { get; init; }
    public int PlayCount { get; init; }
    public int SquaresHit { get; init; }
    public long TotalScore { get; init; }
    public UserBan Ban { get; init; }
    public DateTimeOffset? BannedAt { get; init; }
    public DateTimeOffset? SilencedAt { get; init; }
    public DateTimeOffset? SilenceExpiresAt { get; init; }
}
