using System.Linq.Expressions;
using Resonance.Core.Models;
using Resonance.Database.Entities;

namespace Resonance.Database.Mapping;

internal static class UserMapping
{
    internal static readonly Expression<Func<User, Account>> AccountProjection = user => new Account(user.Id, user.Username)
    {
        CreatedAt = user.CreatedAt,
        LastOnlineAt = user.LastOnlineAt,
        AboutMe = user.AboutMe,
        CountryCode = user.CountryCode,
        Rank = user.Rank,
        RhythmPoints = user.RhythmPoints,
        PlayCount = user.PlayCount,
        SquaresHit = user.SquaresHit,
        TotalScore = user.TotalScore,
        Status = user.Status,
        StatusExpiresAt = user.StatusExpiresAt
    };

    private static readonly Func<User, Account> MapAccount = AccountProjection.Compile();

    internal static Account ToAccount(User user)
    {
        return MapAccount(user);
    }
}
