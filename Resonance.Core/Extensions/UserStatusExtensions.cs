using Resonance.Core.Models;

namespace Resonance.Core.Extensions;

public static class UserStatusExtensions
{
    public static UserStatus GetEffectiveStatus(this UserStatus status, DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        return expiresAt <= now ? UserStatus.Active : status;
    }
}
