using Microsoft.Extensions.Logging;
using Resonance.Core.Repositories;

namespace Resonance.Core.Services;

public sealed class UserActivityService(
    IUserRepository users,
    TimeProvider clock,
    ILogger<UserActivityService> logger)
{
    public async Task RecordAsync(int userId, DateTimeOffset observedAt)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5), clock);

        try
        {
            await users.RecordActivityAsync(userId, observedAt, timeout.Token);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not record last online time for account {AccountId}", userId);
        }
    }
}
