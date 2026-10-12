using System.Threading.RateLimiting;
using Resonance.Core.Models;
using Resonance.Core.Configuration;

namespace Resonance.Core.Services;

public sealed class AccountActionLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<(int UserId, AccountAction Action)> _limiter;

    public AccountActionLimiter(RateLimitSettings settings)
    {
        if (settings.HttpRequestsPerSecond <= 0 || settings.GamePacketsPerSecond <= 0)
            throw new ArgumentException("Action rates must be positive.", nameof(settings));

        _limiter = PartitionedRateLimiter.Create<(int UserId, AccountAction Action), (int UserId, AccountAction Action)>(key =>
        {
            var rate = key.Action switch
            {
                AccountAction.Http => settings.HttpRequestsPerSecond,
                AccountAction.GamePacket => settings.GamePacketsPerSecond,
                _ => throw new ArgumentOutOfRangeException(nameof(key.Action))
            };

            return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = rate,
                Window = TimeSpan.FromSeconds(1),
                SegmentsPerWindow = 10,
                QueueLimit = 0
            });
        });
    }

    public bool TryAcquire(int userId, AccountAction action, out TimeSpan retryAfter)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        using var lease = _limiter.AttemptAcquire((userId, action));
        retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var delay) ? delay : TimeSpan.FromSeconds(1);
        return lease.IsAcquired;
    }

    public void Dispose()
    {
        _limiter.Dispose();
    }
}
