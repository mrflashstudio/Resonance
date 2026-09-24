using System.Globalization;
using Resonance.Models;

namespace Resonance.Extensions;

public static class UserStatusExtensions
{
    extension(UserStatus status)
    {
        public UserStatus GetEffectiveStatus(DateTimeOffset? expiresAt, DateTimeOffset now)
        {
            return expiresAt <= now ? UserStatus.Active : status;
        }

        public string? ToProtocolString(DateTimeOffset? expiresAt, DateTimeOffset now)
        {
            return status.GetEffectiveStatus(expiresAt, now) switch
            {
                UserStatus.Active => "cool",
                UserStatus.Silenced when expiresAt is { } expiry =>
                    $"silenced until {expiry.ToString("O", CultureInfo.InvariantCulture)}",
                UserStatus.Silenced => "silenced",
                UserStatus.Restricted => "restricted",
                UserStatus.Excluded => "excluded",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown user status.")
            };
        }
    }
}
