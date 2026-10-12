using Resonance.Core.Models;
using Resonance.Core.Repositories;

namespace Resonance.Core.Commands.ChatCommands.Administration;

[ChatCommand("giveprivilege", Usage = "/giveprivilege <user id or username> <value or name>", RequiredPrivileges = UserPrivileges.Developer,
    Description = "Give an account one or more privileges by name or numeric value", Example = "/giveprivilege 2 Moderator")]
public sealed class GivePrivilegeCommand(IUserRepository users) : AccountFlagCommand<UserPrivileges>(users, "Granted privileges")
{
    protected override Task<bool> ChangeAsync(int id, UserPrivileges flags, CancellationToken cancellationToken) =>
        Users.ChangePrivilegesAsync(id, flags, true, cancellationToken);
}
