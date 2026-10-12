using Microsoft.Extensions.Options;
using Resonance.Core.Configuration;
using Resonance.Core.Services;

namespace Resonance.Hosting;

public sealed class SessionCleanupService(
    IAuthenticationService authentication,
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
                    await authentication.RemoveExpiredSessionsAsync(stoppingToken);
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
