using System.Globalization;
using Resonance.Models;

namespace Resonance.Extensions;

public static class AccountExtensions
{
    public static Profile ToProfile(this Account account, DateTimeOffset now)
    {
        return new Profile
        {
            Id = account.Id,
            Uid = account.Id.ToString(CultureInfo.InvariantCulture),
            Username = account.Name,
            CreatedAt = account.CreatedAt.ToUnixTimeMilliseconds(),
            AboutMe = account.AboutMe,
            Flag = account.CountryCode,
            Position = account.Rank,
            SkillPoints = account.RhythmPoints,
            PlayCount = account.PlayCount,
            SquaresHit = account.SquaresHit,
            TotalScore = account.TotalScore,
            IsOnline = account.IsOnline,
            Verified = false,
            Ban = account.Status.ToProtocolString(account.StatusExpiresAt, now),
            Badges = []
        };
    }
}
