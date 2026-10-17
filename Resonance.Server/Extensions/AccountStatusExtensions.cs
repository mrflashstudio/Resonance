using System.Globalization;
using Resonance.Core.Extensions;
using Resonance.Core.Models;

namespace Resonance.Server.Extensions;

public static class AccountStatusExtensions
{
    public static string ToProtocolStatus(this Account account, DateTimeOffset now)
    {
        return account.Ban switch
        {
            UserBan.Excluded => "excluded",
            UserBan.Restricted => "restricted",
            UserBan.None when !account.IsSilenced(now) => "cool",
            UserBan.None when account.SilenceExpiresAt is { } expiry =>
                $"silenced until {expiry.ToString("O", CultureInfo.InvariantCulture)}",
            UserBan.None => "silenced",
            _ => throw new ArgumentOutOfRangeException(nameof(account), account.Ban, "Unknown user ban.")
        };
    }
}
