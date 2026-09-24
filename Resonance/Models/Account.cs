namespace Resonance.Models;

public sealed record Account(int Id, string Name)
{
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
    public UserStatus Status { get; init; }
    public DateTimeOffset? StatusExpiresAt { get; init; }
}
