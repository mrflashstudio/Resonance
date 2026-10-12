using Resonance.Core.Models;
using Resonance.Core.Repositories;

namespace Resonance.Core.Commands.ChatCommands.Administration;

[ChatCommand("takebadge", Usage = "/takebadge <user id or username> <value or name>", RequiredPrivileges = UserPrivileges.Developer,
    Description = "Remove one or more awarded badges from an account by name or numeric value", Example = "/takebadge \"Example User\" Tester")]
public sealed class TakeBadgeCommand(IUserRepository users) : AccountFlagCommand<UserBadges>(users, "Removed badges")
{
    protected override Task<bool> ChangeAsync(int id, UserBadges flags, CancellationToken cancellationToken) =>
        Users.ChangeBadgesAsync(id, flags, false, cancellationToken);
}
