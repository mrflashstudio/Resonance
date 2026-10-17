using System.Globalization;
using Resonance.Core.Extensions;
using Resonance.Core.Models;
using Resonance.Core.Services;
using Resonance.Server.Models;

namespace Resonance.Server.Extensions;

public static class AccountExtensions
{
    public static Profile ToProfile(this Account account, DateTimeOffset now, IProfileAssets assets)
    {
        return new Profile
        {
            Id = account.Id,
            Uid = account.Id.ToString(CultureInfo.InvariantCulture),
            Username = account.Name,
            AvatarUrl = assets.GetUrl(account, AssetKind.Avatar),
            ProfileImage = assets.GetUrl(account, AssetKind.Banner),
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
            Ban = account.ToProtocolStatus(now),
            Badges = account.GetBadgeNames()
        };
    }
}
