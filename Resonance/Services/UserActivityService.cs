using Microsoft.EntityFrameworkCore;
using Resonance.Data;

namespace Resonance.Services;

public sealed class UserActivityService(
    IDbContextFactory<ResonanceDbContext> database,
    TimeProvider clock,
    ILogger<UserActivityService> logger)
{
    public async Task RecordAsync(int userId, DateTimeOffset observedAt)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5), clock);

        try
        {
            await using var db = await database.CreateDbContextAsync(timeout.Token);

            await db.Users.Where(user => user.Id == userId &&
                    (user.LastOnlineAt == null || user.LastOnlineAt < observedAt))
                .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.LastOnlineAt, observedAt), timeout.Token);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not record last online time for account {AccountId}", userId);
        }
    }
}
