using Resonance.Core.Models;

namespace Resonance.Core.Extensions;

public static class AccountStatusExtensions
{
    public static bool IsSilenced(this Account account, DateTimeOffset now)
    {
        return account.SilencedAt is not null &&
            (account.SilenceExpiresAt is null || account.SilenceExpiresAt > now);
    }

    public static bool CanChat(this Account account, DateTimeOffset now)
    {
        return account.Ban == UserBan.None && !account.IsSilenced(now);
    }
}
