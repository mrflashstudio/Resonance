using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Resonance.Authentication;
using Resonance.Data;

namespace Resonance.Services;

public sealed class SessionCleanupService(
    IDbContextFactory<ResonanceDbContext> database,
    IOptions<SessionSettings> settings,
    TimeProvider clock,
    ILogger<SessionCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(settings.Value.CleanupInterval, clock);

        try
        {
            do
            {
                try
                {
                    await using var db = await database.CreateDbContextAsync(stoppingToken);
                    var now = clock.GetUtcNow();

                    await db.Sessions.Where(session => session.ExpiresAt <= now || session.RevokedAt != null)
                        .ExecuteDeleteAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Could not clean up expired and revoked sessions");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
