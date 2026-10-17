using Resonance.Core.Models;

namespace Resonance.Models;

public sealed record SessionResponse(
    int Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastOnlineAt,
    bool IsOnline,
    string AboutMe,
    string? CountryCode,
    int Rank,
    double RhythmPoints,
    int PlayCount,
    int SquaresHit,
    long TotalScore,
    UserBan Ban,
    DateTimeOffset? BannedAt,
    DateTimeOffset? SilencedAt,
    DateTimeOffset? SilenceExpiresAt)
{
    public static SessionResponse FromAccount(Account account)
    {
        return new SessionResponse(account.Id, account.Name, account.CreatedAt, account.LastOnlineAt,
            account.IsOnline, account.AboutMe, account.CountryCode, account.Rank, account.RhythmPoints,
            account.PlayCount, account.SquaresHit, account.TotalScore, account.Ban, account.BannedAt,
            account.SilencedAt, account.SilenceExpiresAt);
    }
}
