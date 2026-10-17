using Microsoft.Extensions.Logging;
using Resonance.Core.Extensions;
using Resonance.Core.Models;
using Resonance.Core.Repositories;

namespace Resonance.Core.Services;

public sealed class ModerationService(IUserRepository users, TimeProvider clock, ILogger<ModerationService> logger)
{
    public async Task<Account?> ChangeSilenceAsync(int actorId, int targetId, bool silenced,
        DateTimeOffset? expiresAt, string? reason, CancellationToken cancellationToken)
    {
        if (targetId <= 0 || actorId == targetId)
            return null;

        var now = clock.GetUtcNow();

        if (silenced)
        {
            if (expiresAt <= now || string.IsNullOrWhiteSpace(reason) || reason.Length > 1000 || reason.Any(char.IsControl))
                return null;

            reason = reason.Trim();
        }
        else
        {
            expiresAt = null;
            reason = null;
        }

        var actor = await users.GetAsync(actorId, cancellationToken);

        if (actor is null || actor.IsBot || !actor.CanChat(now) ||
            !actor.HasAnyPrivilege(UserPrivileges.Moderator | UserPrivileges.TeamAssistant))
            return null;

        var target = await users.ChangeSilenceAsync(targetId, silenced ? now : null, expiresAt, reason, cancellationToken);

        if (target is not null)
            logger.LogInformation("Account {ActorId} set silence for account {TargetId}: {Silenced}, expires {ExpiresAt}, reason: {Reason}",
                actorId, targetId, silenced, expiresAt, reason);

        return target;
    }
}
