using Resonance.Core.Models;
using Resonance.Core.Repositories;

namespace Resonance.Core.Commands.ChatCommands.Administration;

[ChatCommand("givebadge", Usage = "/givebadge <user id or username> <value or name>", RequiredPrivileges = UserPrivileges.Developer,
    Description = "Give an account one or more awarded badges by name or numeric value", Example = "/givebadge \"Example User\" Tester")]
public sealed class GiveBadgeCommand(IUserRepository users) : AccountFlagCommand<UserBadges>(users, "Granted badges")
{
    protected override Task<bool> ChangeAsync(int id, UserBadges flags, CancellationToken cancellationToken) =>
        Users.ChangeBadgesAsync(id, flags, true, cancellationToken);
}
