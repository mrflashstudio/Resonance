using System.Globalization;
using Resonance.Core.Extensions;
using Resonance.Core.Models;

namespace Resonance.Server.Extensions;

public static class UserStatusExtensions
{
    public static string ToProtocolString(this UserStatus status, DateTimeOffset? expiresAt, DateTimeOffset now)
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
