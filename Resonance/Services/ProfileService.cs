using Microsoft.EntityFrameworkCore;
using Resonance.Data;
using Resonance.Extensions;
using Resonance.Models;

namespace Resonance.Services;

public sealed class ProfileService(
    IDbContextFactory<ResonanceDbContext> database,
    IOnlinePlayers onlinePlayers,
    TimeProvider clock) : IProfileService
{
    public async Task<Profile?> GetAsync(int id, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        var account = await db.Users.AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new Account(user.Id, user.Username)
            {
                CreatedAt = user.CreatedAt,
                AboutMe = user.AboutMe,
                CountryCode = user.CountryCode,
                Rank = user.Rank,
                RhythmPoints = user.RhythmPoints,
                PlayCount = user.PlayCount,
                SquaresHit = user.SquaresHit,
                TotalScore = user.TotalScore,
                Status = user.Status,
                StatusExpiresAt = user.StatusExpiresAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        return account is null ? null : account.ToProfile(clock.GetUtcNow()) with
        {
            IsOnline = onlinePlayers.IsOnline(id)
        };
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        await using var db = await database.CreateDbContextAsync(cancellationToken);
        return await db.Users.AnyAsync(user => user.Id == id, cancellationToken);
    }
}
